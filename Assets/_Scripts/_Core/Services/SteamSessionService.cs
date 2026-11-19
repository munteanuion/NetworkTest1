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

    public sealed class SteamSessionService : IDisposable
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
            _busy = false;
        }


        public void Join(ulong lobbyId)
        {
            if (!_steamReady || _busy || _networkManager.IsListening || lobbyId == 0)
                return;

            _busy = true;
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
            _busy = false;
        }


        public void StartGame()
        {
            if (!_networkManager.IsServer || !_networkManager.IsListening)
                return;

            Status = "Starting game...";
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
            if (_networkManager.IsListening)
                _networkManager.Shutdown();

            LeaveLobby();

            if (_steamReady)
                Status = "Offline";
        }


        public void OpenInvite()
        {
            if (InLobby)
                SteamFriends.ActivateGameOverlayInviteDialog(_lobby);
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
        }


        private static void Approve(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = true;
            response.CreatePlayerObject = false;
            response.Pending = false;
        }


        public void Dispose()
        {
            Leave();

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