using UnityEngine;

namespace EasternFantasy.Player
{
    [CreateAssetMenu(menuName = "Eastern Fantasy/Player Movement Settings")]
    public sealed class PlayerMovementSettings : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 6f;
        [SerializeField, Min(0.1f)] private float jumpSpeed = 11f;
        [SerializeField, Min(0.1f)] private float gravityScale = 3f;
        [SerializeField, Range(0.01f, 0.15f)] private float groundCheckDistance = 0.05f;

        public float MoveSpeed => moveSpeed;
        public float JumpSpeed => jumpSpeed;
        public float GravityScale => gravityScale;
        public float GroundCheckDistance => groundCheckDistance;
    }
}
