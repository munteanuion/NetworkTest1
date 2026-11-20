using System.Text;
using Assets._Scripts._Core;
using Assets._Scripts._Core.Configs;
using Assets._Scripts._Core.Services;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets._Scripts.UI
{
    public sealed class GameplayHud : MonoBehaviour
    {
        [SerializeField] private Text _playersText;

        private SteamSessionService _session;
        private AppConfig _config;

        private float _nextRefresh;
        private bool _leaving;


        private void Start()
        {
            _session = NetworkDiResolver.Resolve<SteamSessionService>();
            _config = NetworkDiResolver.Resolve<AppConfig>();
        }

        private void Update()
        {
            if (_session == null) return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                GoToMainMenu();
                return;
            }

            if (Time.unscaledTime < _nextRefresh) return;

            _nextRefresh = Time.unscaledTime + 0.5f;
            Refresh();
        }

        private void Refresh()
        {
            if (_playersText == null) return;

            var players = _session.GetLobbyPlayers();

            if (players.Count == 0)
            {
                _playersText.text = "Players: -";
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine($"Players ({players.Count}/{_session.MaxPlayers})");

            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                var host = player.IsOwner ? " (host)" : string.Empty;

                builder.AppendLine($"{i + 1}. {player.Name}{host} - {player.Id}");
            }

            _playersText.text = builder.ToString();
        }

        private void GoToMainMenu()
        {
            if (_leaving) return;

            _leaving = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _session?.Leave();

            if (_config != null)
                SceneManager.LoadScene(_config.MenuScene);
        }
    }
}
