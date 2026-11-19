using Assets._Scripts._Core;
using Assets._Scripts._Core.Services;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Assets._Scripts.UI
{
    public class UISessionController : MonoBehaviour
    {
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private Button _inviteButton;
        [SerializeField] private Button _leaveButton;
        [SerializeField] private InputField _lobbyIdInput;
        [SerializeField] private Text _statusText;

        private SteamSessionService _session;
        private NetworkManager _networkManager;


        private void Start()
        {
            // use global resolver temporary
            _session = NetworkDiResolver.Resolve<SteamSessionService>();
            _networkManager = NetworkDiResolver.Resolve<NetworkManager>();

            _hostButton.onClick.AddListener(() => _session.Host());
            _joinButton.onClick.AddListener(JoinClicked);
            _inviteButton.onClick.AddListener(() => _session.OpenInvite());
            _leaveButton.onClick.AddListener(() => _session.Leave());
        }


        private void JoinClicked()
        {
            if (!ulong.TryParse(_lobbyIdInput.text.Trim(), out var lobbyId))
            {
                _statusText.text = "Invalid lobby ID.";
                return;
            }

            _session.Join(lobbyId);
        }


        private void Update()
        {
            if (_session == null || _networkManager == null) return;

            var text = _session.Status;

            if (_session.InLobby)
                text += $"\nLobby: {_session.LobbyId}";

            if (_networkManager.IsHost)
                text += $"\nClients: {_networkManager.ConnectedClientsIds.Count}";
            else if (_networkManager.IsConnectedClient)
                text += "\nConnected";

            _statusText.text = text;

            var canStart = _session.IsSteamReady && !_session.Busy && !_networkManager.IsListening;
            _hostButton.interactable = canStart;
            _joinButton.interactable = canStart;
            _inviteButton.interactable = _session.InLobby;
            _leaveButton.interactable = _networkManager.IsListening || _session.InLobby;
        }
    }
}