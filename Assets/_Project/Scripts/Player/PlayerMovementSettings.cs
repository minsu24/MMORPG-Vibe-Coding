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
        [SerializeField, Min(0.1f)] private float dropThroughSpeed = 3f;
        [SerializeField, Range(0f, 0.25f)] private float jumpBufferDuration = 0.12f;
        [SerializeField, Range(0f, 0.2f)] private float coyoteDuration = 0.08f;

        public float MoveSpeed => moveSpeed;
        public float JumpSpeed => jumpSpeed;
        public float GravityScale => gravityScale;
        public float GroundCheckDistance => groundCheckDistance;
        public float DropThroughSpeed => dropThroughSpeed;
        public float JumpBufferDuration => jumpBufferDuration;
        public float CoyoteDuration => coyoteDuration;
    }
}
