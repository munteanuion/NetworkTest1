using UnityEngine;

namespace Assets._Scripts._Core.Configs
{
    [CreateAssetMenu(menuName = "Coop/App Config", fileName = "AppConfig")]
    public sealed class AppConfig : ScriptableObject
    {
        [Header("Build")]
        public string BuildVersion = "0.1.0";


        [Header("Transport")]
        public TransportKind DefaultTransport = TransportKind.Steam;
        public ushort DirectPort = 7777;


        [Header("Session")]
        public int MaxPlayers = 4;
        public string MenuScene = "MainMenu";
        public string GameScene = "Gameplay";
        public float LoadingTimeoutSeconds = 20f;
        public float ConnectTimeoutSeconds = 10f;


        [Header("Steam")]
        public uint SteamAppId = 480;
        public LobbyVisibility DefaultVisibility = LobbyVisibility.FriendsOnly;
    }
}
