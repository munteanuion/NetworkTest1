using System;
using System.Collections.Generic;
using Assets._Scripts._Core.Configs;
using Assets._Scripts._Core.Network;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets._Scripts._Core.Services
{
    public readonly struct LobbyPlayer
    {
        public readonly ulong Id;
        public readonly string Name;
        public readonly bool IsOwner;

        public LobbyPlayer(ulong id, string name, bool isOwner)
        {
            Id = id;
            Name = name;
            IsOwner = isOwner;
        }
    }

    public class SteamSessionService : IDisposable
    {
        private readonly NetworkManager _networkManager;
        private readonly TransportProvider _transport;
        private readonly AppConfig _config;

        private readonly List<LobbyPlayer> _members = new();

        private bool _steamReady;
        private bool _busy;
        private CSteamID _lobby;

        private CallResult<LobbyCreated_t> _createCall;
        private CallResult<LobbyEnter_t> _enterCall;
        private Callback<LobbyChatUpdate_t> _lobbyChatUpdate;

        public bool IsSteamReady => _steamReady;
        public bool Busy => _busy;
        public bool InLobby => _lobby.m_SteamID != 0;
        public ulong LobbyId => _lobby.m_SteamID;
        public int MaxPlayers => _config.MaxPlayers;
        public int MemberVersion { get; private set; }
        public string Status { get; private set; } = "Offline";
        private bool _leaving;
        private bool _returningToMenu;
        private string _disconnectReason;


        public SteamSessionService(
            NetworkManager networkManager,
            TransportProvider transport,
            AppConfig config)
        {
            _networkManager = networkManager;
            _transport = transport;
            _config = config;
        }


        public bool Initialize()
        {
            if (_steamReady) return true;

            try
            {
#if !UNITY_EDITOR
                if (SteamAPI.RestartAppIfNecessary(new AppId_t(_config.SteamAppId)))
                {
                    Status = "Relaunching through Steam...";
                    return false;
                }
#endif
                if (!SteamAPI.Init())
                {
                    Status = "Steam is not running.";
                    return false;
                }
                _steamReady = true;
                SteamNetworkingUtils.InitRelayNetworkAccess();
                _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
                _networkManager.OnClientDisconnectCallback += OnClientDisconnected;
                _networkManager.OnClientStopped += OnClientStopped;

                Status = $"Steam: {SteamFriends.GetPersonaName()}";
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Status = "Steam init failed.";
                return false;
            }
        }


        public void Tick()
        {
            if (_steamReady)
                SteamAPI.RunCallbacks();
        }


        public void Host()
        {
            if (!_steamReady || _busy || _networkManager.IsListening)
                return;

            _busy = true;
            _returningToMenu = false;
            _disconnectReason = null;
            Status = "Creating lobby...";

            _createCall = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            _createCall.Set(SteamMatchmaking.CreateLobby(
                ELobbyType.k_ELobbyTypeFriendsOnly,
                _config.MaxPlayers));
        }


        private void OnLobbyCreated(LobbyCreated_t result, bool ioFailure)
        {
            _createCall?.Dispose();
            _createCall = null;

            if (ioFailure || result.m_eResult != EResult.k_EResultOK)
            {
                Status = ioFailure
                    ? "Create lobby failed."
                    : $"Create lobby failed: {result.m_eResult}";
                _busy = false;
                return;
            }

            _lobby = new CSteamID(result.m_ulSteamIDLobby);
            MemberVersion++;

            _transport.Select(TransportKind.Steam);
            _networkManager.NetworkConfig.ConnectionApproval = true;
            _networkManager.ConnectionApprovalCallback = Approve;
            _networkManager.gameObject.SetActive(true);

            if (!_networkManager.StartHost())
            {
                Status = "StartHost failed.";
                LeaveLobby();
                _busy = false;
                return;
            }

            Status = $"Host | lobby: {_lobby.m_SteamID}";
            UpdatePresence("In lobby");
            _busy = false;
        }


        public void Join(ulong lobbyId)
        {
            if (!_steamReady || _busy || _networkManager.IsListening || lobbyId == 0)
                return;

            _busy = true;
            _returningToMenu = false;
            _disconnectReason = null;
            Status = "Joining lobby...";

            _enterCall = CallResult<LobbyEnter_t>.Create(OnLobbyEntered);
            _enterCall.Set(SteamMatchmaking.JoinLobby(new CSteamID(lobbyId)));
        }


        private void OnLobbyEntered(LobbyEnter_t result, bool ioFailure)
        {
            _enterCall?.Dispose();
            _enterCall = null;

            if (ioFailure || result.m_EChatRoomEnterResponse != 1)
            {
                Status = ioFailure
                    ? "Join failed."
                    : result.m_EChatRoomEnterResponse == 4
                        ? "Session is full."
                        : $"Join failed: {result.m_EChatRoomEnterResponse}";
                _busy = false;
                return;
            }

            _lobby = new CSteamID(result.m_ulSteamIDLobby);
            MemberVersion++;

            var hostSteamId = SteamMatchmaking.GetLobbyOwner(_lobby).m_SteamID;
            if (hostSteamId == 0 || hostSteamId == SteamUser.GetSteamID().m_SteamID)
            {
                Status = "Invalid lobby host.";
                LeaveLobby();
                _busy = false;
                return;
            }

            _transport.Select(TransportKind.Steam);
            _transport.SetSteamPeer(hostSteamId);

            _networkManager.NetworkConfig.ConnectionApproval = true;
            _networkManager.gameObject.SetActive(true);

            if (!_networkManager.StartClient())
            {
                Status = "StartClient failed.";
                LeaveLobby();
                _busy = false;
                return;
            }

            Status = $"Joining host {hostSteamId}...";
            UpdatePresence("In lobby");
            _busy = false;
        }


        public void StartGame()
        {
            if (!_networkManager.IsServer || !_networkManager.IsListening)
                return;

            Status = "Starting game...";
            UpdatePresence("In game");
            _networkManager.SceneManager.LoadScene(_config.GameScene, LoadSceneMode.Single);
        }


        public IReadOnlyList<LobbyPlayer> GetLobbyPlayers()
        {
            _members.Clear();

            if (!InLobby)
                return _members;

            var owner = SteamMatchmaking.GetLobbyOwner(_lobby);
            var count = SteamMatchmaking.GetNumLobbyMembers(_lobby);

            for (var i = 0; i < count; i++)
            {
                var id = SteamMatchmaking.GetLobbyMemberByIndex(_lobby, i);
                _members.Add(new LobbyPlayer(id.m_SteamID, GetPlayerName(id.m_SteamID), id == owner));
            }

            return _members;
        }


        public string GetPlayerName(ulong steamId)
        {
            if (!_steamReady)
                return steamId.ToString();

            var id = new CSteamID(steamId);
            var name = id == SteamUser.GetSteamID()
                ? SteamFriends.GetPersonaName()
                : SteamFriends.GetFriendPersonaName(id);

            return string.IsNullOrEmpty(name) ? steamId.ToString() : name;
        }


        public void Leave()
        {
            _leaving = true;
            _disconnectReason = null;

            if (_networkManager.IsListening)
                _networkManager.Shutdown();

            LeaveLobby();
            _leaving = false;

            if (_steamReady)
                Status = "Offline";
        }


        public void OpenInvite()
        {
            if (!InLobby) return;

            SteamFriends.ActivateGameOverlayInviteDialog(_lobby);
            Debug.Log($"[Steam] Invite overlay requested for lobby {_lobby.m_SteamID}");
        }


        /// <summary>
        /// Publishes the lobby to the friends list so "Join Game" is available.
        /// </summary>
        private void UpdatePresence(string status)
        {
            if (!_steamReady) return;

            if (!InLobby)
            {
                SteamFriends.ClearRichPresence();
                return;
            }

            SteamFriends.SetRichPresence("connect", $"+connect_lobby {_lobby.m_SteamID}");
            SteamFriends.SetRichPresence("status", status);
        }


        private void OnLobbyChatUpdate(LobbyChatUpdate_t update)
        {
            MemberVersion++;
        }


        private void LeaveLobby()
        {
            if (InLobby)
                SteamMatchmaking.LeaveLobby(_lobby);

            _lobby = default;
            MemberVersion++;

            if (_steamReady)
                SteamFriends.ClearRichPresence();
        }


        private void Approve(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            foreach (var clientId in _networkManager.ConnectedClientsIds)
            {
                if (clientId == request.ClientNetworkId)
                {
                    response.Approved = false;
                    response.Reason = "You are already in this session.";
                    return;
                }
            }

            if (_networkManager.ConnectedClientsIds.Count >= _config.MaxPlayers)
            {
                response.Approved = false;
                response.Reason = "Session is full.";
                return;
            }

            response.Approved = true;
            response.CreatePlayerObject = false;
            response.Pending = false;
        }


        private void OnClientDisconnected(ulong clientId)
        {
            if (_networkManager.IsServer || _leaving) return;

            _disconnectReason = _networkManager.DisconnectReason;
            LeaveLobby();

            if (!string.IsNullOrEmpty(_disconnectReason))
                Status = _disconnectReason;
        }


        /// <summary>
        /// A client that loses the host returns to the main menu with a message.
        /// </summary>
        private void OnClientStopped(bool wasClean)
        {
            if (_networkManager.IsServer || _leaving || _returningToMenu) return;
            if (SceneManager.GetActiveScene().name == _config.MenuScene) return;

            _returningToMenu = true;

            Status = string.IsNullOrEmpty(_disconnectReason)
                ? "Disconnected from the host."
                : _disconnectReason;

            _disconnectReason = null;

            SceneManager.LoadScene(_config.MenuScene);
        }


        public void Dispose()
        {
            Leave();

            _networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            _networkManager.OnClientStopped -= OnClientStopped;

            _lobbyChatUpdate?.Dispose();
            _lobbyChatUpdate = null;

            if (_steamReady)
            {
                SteamAPI.Shutdown();
                _steamReady = false;
            }
        }
    }
}