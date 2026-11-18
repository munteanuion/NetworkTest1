using UnityEngine;

namespace Assets._Scripts._Core.Configs
{
    [CreateAssetMenu(menuName = "Coop/Character Config", fileName = "CharacterConfig")]
    public class CharacterConfig : ScriptableObject
    {
        [Header("Player")]
        public float MoveSpeed = 5f;
        public float SprintSpeed = 7.5f;
        public float JumpHeight = 1.15f;
        public float MouseSensitivity = 2.0f;
    }
}