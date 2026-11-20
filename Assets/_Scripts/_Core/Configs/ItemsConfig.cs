using UnityEngine;

namespace Assets._Scripts._Core.Configs
{
    [CreateAssetMenu(menuName = "Coop/Items Config", fileName = "ItemsConfig")]
    public class ItemsConfig : ScriptableObject
    {
        [Header("Items")]
        public float GrabRange = 3.0f;
        public float GrabTolerance = 0.75f;
        public float ThrowSpeed = 9.0f;
        public int MaxItems = 40;
        public float ItemSpawnCooldown = 0.5f;
    }
}
