using UnityEngine;
using UnityEngine.InputSystem;

namespace EasternFantasy.Player
{
    [RequireComponent(typeof(PlayerMovement2D))]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        private InputActionAsset runtimeActions;
        private InputAction move;
        private InputAction jump;
        private PlayerMovement2D movement;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement2D>();
            if (inputActions == null)
            {
                Debug.LogError("PlayerInputReader requires an InputActionAsset.", this);
                enabled = false;
                return;
            }
            // Own the enabled state without changing the project's shared action asset.
            runtimeActions = Instantiate(inputActions);
            move = runtimeActions.FindAction("Player/Move", true);
            jump = runtimeActions.FindAction("Player/Jump", true);
        }

        private void OnEnable()
        {
            if (runtimeActions == null) return;
            move.Enable();
            jump.Enable();
        }

        private void Update()
        {
            movement.SetMoveInput(move.ReadValue<Vector2>().x);
            if (jump.WasPressedThisFrame()) movement.RequestJump();
        }

        private void OnDisable()
        {
            if (runtimeActions != null) runtimeActions.Disable();
            if (movement != null) movement.ClearInput();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) movement.ClearInput();
        }

        private void OnDestroy()
        {
            if (runtimeActions != null) Destroy(runtimeActions);
        }
    }
}
