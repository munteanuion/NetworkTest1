using UnityEngine;

namespace Assets._Scripts._Core.Configs
{
    [CreateAssetMenu(menuName = "Coop/Network Prefabs", fileName = "NetworkPrefabs")]
    public sealed class NetworkPrefabsConfig : ScriptableObject
    {
        public GameObject Player;
        public GameObject PhysicsItem;
    }
}
