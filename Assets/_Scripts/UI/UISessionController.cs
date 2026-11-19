using System.Collections.Generic;
using Assets._Scripts._Core;
using Assets._Scripts._Core.Services;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Assets._Scripts.UI
{
    public class UISessionController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _sessionPanel;
        [SerializeField] private GameObject _lobbyPanel;

        [Header("Session Panel")]
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private Button _leaveButton;
        [SerializeField] private InputField _lobbyIdInput;
        [SerializeField] private Text _statusText;

        [Header("Lobby Panel")]
        [SerializeField] private Text _lobbyIdText;
        [SerializeField] private Button _copyIdButton;
        [SerializeField] private Button _lobbyInviteButton;
        [SerializeField] private Text _playersHeader;
        [SerializeField] private Transform _playersContent;
        [SerializeField] private Text _lobbyStatusText;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _lobbyLeaveButton;
        [SerializeField] private Text _playerRowTemplate;

        private SteamSessionService _session;
        private NetworkManager _networkManager;

        private int _memberVersion = -1;
        private float _copiedUntil;


        private void Start()
        {
            _session = NetworkDiResolver.Resolve<SteamSessionService>();
            _networkManager = NetworkDiResolver.Resolve<NetworkManager>();

            if (_session == null || _networkManager == null)
            {
                Debug.LogError("[UI] DI resolver is not initialized.");
                enabled = false;
                return;
            }

            if (_hostButton != null) _hostButton.onClick.AddListener(() => _session.Host());
            if (_joinButton != null) _joinButton.onClick.AddListener(JoinClicked);
            if (_leaveButton != null) _leaveButton.onClick.AddListener(LeaveClicked);
            if (_copyIdButton != null) _copyIdButton.onClick.AddListener(CopyLobbyId);
            if (_lobbyInviteButton != null) _lobbyInviteButton.onClick.AddListener(() => _session.OpenInvite());
            if (_lobbyLeaveButton != null) _lobbyLeaveButton.onClick.AddListener(LeaveClicked);
            if (_startButton != null) _startButton.onClick.AddListener(() => _session.StartGame());

            ApplyPanels(false);
        }


        private void Update()
        {
            if (_session == null || _networkManager == null) return;

            var inSession = _networkManager.IsListening;
            ApplyPanels(inSession);

            if (inSession)
                UpdateLobbyPanel();
            else
                UpdateSessionPanel();
        }


        private void ApplyPanels(bool inSession)
        {
            if (_sessionPanel != null && _sessionPanel.activeSelf == inSession)
                _sessionPanel.SetActive(!inSession);

            if (_lobbyPanel != null && _lobbyPanel.activeSelf != inSession)
                _lobbyPanel.SetActive(inSession);
        }


        private void UpdateSessionPanel()
        {
            if (_statusText != null)
            {
                var text = _session.Status;

                if (_session.InLobby)
                    text += $"\nLobby: {_session.LobbyId}";

                _statusText.text = text;
            }

            var canStart = _session.IsSteamReady && !_session.Busy;

            if (_hostButton != null) _hostButton.interactable = canStart;
            if (_joinButton != null) _joinButton.interactable = canStart;
            if (_leaveButton != null) _leaveButton.interactable = _session.InLobby;
        }


        private void UpdateLobbyPanel()
        {
            if (_lobbyIdText != null)
                _lobbyIdText.text = _session.LobbyId.ToString();

            if (_startButton != null)
            {
                _startButton.gameObject.SetActive(_networkManager.IsHost);
                _startButton.interactable = _networkManager.IsHost;
            }

            if (_copyIdButton != null)
                _copyIdButton.gameObject.SetActive(_session.InLobby);

            if (_lobbyInviteButton != null)
                _lobbyInviteButton.gameObject.SetActive(_session.InLobby);

            if (_lobbyStatusText != null)
                _lobbyStatusText.text = BuildLobbyStatus();

            if (_session.MemberVersion != _memberVersion)
            {
                _memberVersion = _session.MemberVersion;
                RebuildPlayers();
            }
        }


        private string BuildLobbyStatus()
        {
            if (Time.unscaledTime < _copiedUntil)
                return "Lobby ID copied to clipboard.";

            if (!_networkManager.IsHost && !_networkManager.IsConnectedClient)
                return _session.Status;

            return _networkManager.IsHost
                ? "You are the host. Press Start game."
                : "Waiting for the host to start...";
        }


        private void RebuildPlayers()
        {
            if (_playersContent == null) return;

            for (var i = _playersContent.childCount - 1; i >= 0; i--)
            {
                var child = _playersContent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            var players = _session.GetLobbyPlayers();

            if (_playersHeader != null)
                _playersHeader.text = $"Players ({players.Count}/{_session.MaxPlayers})";

            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];

                var text = Instantiate(_playerRowTemplate, _playersContent, false);

                text.gameObject.SetActive(true);
                text.text = player.IsOwner
                    ? $"{i + 1}. {player.Name} (host) — {player.Id}"
                    : $"{i + 1}. {player.Name} — {player.Id}";
            }
        }


        private void JoinClicked()
        {
            if (!ulong.TryParse(_lobbyIdInput.text.Trim(), out var lobbyId))
            {
                if (_statusText != null)
                    _statusText.text = "Invalid lobby ID.";

                return;
            }

            _session.Join(lobbyId);
        }


        private void CopyLobbyId()
        {
            if (!_session.InLobby) return;

            GUIUtility.systemCopyBuffer = _session.LobbyId.ToString();
            _copiedUntil = Time.unscaledTime + 1.5f;
        }


        private void LeaveClicked()
        {
            _memberVersion = -1;
            _session.Leave();
        }
    }
}