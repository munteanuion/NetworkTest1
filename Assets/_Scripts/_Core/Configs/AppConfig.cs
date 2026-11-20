using UnityEngine;

namespace Assets._Scripts._Core.Configs
{
    [CreateAssetMenu(menuName = "Coop/App Config", fileName = "AppConfig")]
    public sealed class AppConfig : ScriptableObject
    {
        [Header("Session")]
        public int MaxPlayers = 4;
        public string MenuScene = "MainMenu";
        public string GameScene = "Gameplay";


        [Header("Steam")]
        public uint SteamAppId = 480;
    }
}
