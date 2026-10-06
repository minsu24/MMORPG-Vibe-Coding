using UnityEngine;
using UnityEngine.InputSystem;
using EasternFantasy.Quest;

namespace EasternFantasy.Player
{
    public interface IPlayerBasicAttack
    {
        bool TryAttack();
    }

    [RequireComponent(typeof(PlayerMovement2D))]
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
        private PlayerEntity entity;
        private IPlayerBasicAttack basicAttack;
        private PlayerQuestInteraction questInteraction;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement2D>();
            entity = GetComponent<PlayerEntity>();
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
            foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
            {
                if (component is IPlayerBasicAttack attackHandler
                    && component.enabled)
                {
                    basicAttack = attackHandler;
                    break;
                }
            }

            if (basicAttack == null)
                Debug.LogWarning("Player prefab has no enabled basic attack handler.", this);
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
            if (EasternFantasy.Dungeon.DungeonRunController.Instance != null
                && EasternFantasy.Dungeon.DungeonRunController.Instance.IsExitPromptOpen)
            {
                movement.ClearInput();
                return;
            }
            if (EasternFantasy.UI.DungeonWindowUI.Instance != null && EasternFantasy.UI.DungeonWindowUI.Instance.IsOpen)
            {
                movement.ClearInput();
                return;
            }
            if (entity != null && entity.IsDead)
            {
                movement.ClearInput();
                return;
            }
            movement.SetMoveInput(move.ReadValue<Vector2>().x);
            if (jump.WasPressedThisFrame()) movement.RequestJump();
            if (attack.WasPressedThisFrame() && basicAttack != null)
                basicAttack.TryAttack();
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
