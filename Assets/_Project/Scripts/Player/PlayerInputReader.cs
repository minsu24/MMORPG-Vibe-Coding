using UnityEngine;
using UnityEngine.InputSystem;
using EasternFantasy.Quest;

namespace EasternFantasy.Player
{
    [RequireComponent(typeof(PlayerMovement2D))]
    [RequireComponent(typeof(PlayerProjectileShooter))]
    [RequireComponent(typeof(PlayerQuestInteraction))]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        
        private InputActionAsset runtimeActions;
        private InputAction move;
        private InputAction jump;
        private InputAction attack;
        private InputAction interact;
        private PlayerMovement2D movement;
        private PlayerProjectileShooter projectileShooter;
        private PlayerQuestInteraction questInteraction;

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
            attack = runtimeActions.FindAction("Player/Attack", true);
            interact = runtimeActions.FindAction("Player/Interact", true);
            projectileShooter = GetComponent<PlayerProjectileShooter>();
            questInteraction = GetComponent<PlayerQuestInteraction>();
        }

        private void OnEnable()
        {
            if (runtimeActions == null) return;
            move.Enable();
            jump.Enable();
            attack.Enable();
            interact.Enable();
        }

        private void Update()
        {
            movement.SetMoveInput(move.ReadValue<Vector2>().x);
            if (jump.WasPressedThisFrame()) movement.RequestJump();
            if (attack.WasPressedThisFrame() && projectileShooter != null)
                projectileShooter.TryFire();
            if (interact.WasPressedThisFrame() && questInteraction != null)
                questInteraction.TryInteract();
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
