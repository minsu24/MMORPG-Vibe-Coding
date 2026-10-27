using System.Collections.Generic;
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
        [SerializeField, Range(0f, 0.3f)] private float attackBufferDuration = 0.15f;
        [SerializeField] private bool repeatAttackWhileHeld = true;
        
        private InputActionAsset runtimeActions;
        private InputAction move;
        private InputAction jump;
        private InputAction attack;
        private InputAction interact;
        private PlayerMovement2D movement;
        private PlayerEntity entity;
        private IPlayerBasicAttack basicAttack;
        private PlayerQuestInteraction questInteraction;
        private readonly PlayerAttackInputBuffer attackBuffer = new PlayerAttackInputBuffer();
        private PlayerIdleAnimation animationController;
        private bool hasFocus = true;
        private readonly HashSet<object> inputBlockers = new HashSet<object>();
        public bool IsInputBlocked => inputBlockers.Count > 0;

        public bool CanControlGameplay => isActiveAndEnabled && hasFocus && !IsInputBlocked
            && !EasternFantasy.UI.GameplayHudVisibility.IsMenuScene
            && !PortalSceneTransition.IsTransitioning && (entity == null || !entity.IsDead)
            && !IsGameplayWindowOpen;

        public bool CanPerformCombatAction => CanControlGameplay
            && (entity == null || !entity.isKnockback)
            && movement != null && !movement.IsMovementLocked && !movement.IsDashing;

        private static bool IsGameplayWindowOpen =>
            (EasternFantasy.Dialogue.DialogueManager.Instance != null && EasternFantasy.Dialogue.DialogueManager.Instance.IsDialogueActive)
            || (EasternFantasy.UI.InventoryWindowUI.Instance != null && EasternFantasy.UI.InventoryWindowUI.Instance.IsOpen)
            || (EasternFantasy.UI.SkillWindowUI.Instance != null && EasternFantasy.UI.SkillWindowUI.Instance.IsOpen)
            || (EasternFantasy.UI.PlayerStatsWindowUI.Instance != null && EasternFantasy.UI.PlayerStatsWindowUI.Instance.IsOpen)
            || (EasternFantasy.UI.EquipmentWindowUI.Instance != null && EasternFantasy.UI.EquipmentWindowUI.Instance.IsOpen)
            || (EasternFantasy.UI.ShopWindowUI.Instance != null && EasternFantasy.UI.ShopWindowUI.Instance.IsOpen)
            || (EasternFantasy.UI.PetCompanionWindowUI.Instance != null && EasternFantasy.UI.PetCompanionWindowUI.Instance.IsOpen)
            || (EasternFantasy.UI.MapWindowUI.Instance != null && EasternFantasy.UI.MapWindowUI.Instance.IsOpen)
            || (EasternFantasy.UI.ScrollCodexWindowUI.Instance != null && EasternFantasy.UI.ScrollCodexWindowUI.Instance.IsOpen)
            || (EasternFantasy.Dungeon.DungeonRunController.Instance != null && EasternFantasy.Dungeon.DungeonRunController.Instance.IsExitPromptOpen)
            || (EasternFantasy.UI.DungeonWindowUI.Instance != null && EasternFantasy.UI.DungeonWindowUI.Instance.IsOpen)
            || (EasternFantasy.UI.EquipmentUpgradeWindowUI.Instance != null && EasternFantasy.UI.EquipmentUpgradeWindowUI.Instance.IsOpen);

        // Independent dialogue/cinematic owners release only their own lock. Do not
        // snapshot enabled: another owner may already be blocking the same player.
        public void SetInputBlocked(object owner, bool blocked)
        {
            if (owner == null) return;
            if (blocked) inputBlockers.Add(owner);
            else inputBlockers.Remove(owner);
            ClearGameplayInput();
            if (runtimeActions == null) return;
            if (IsInputBlocked || !isActiveAndEnabled) runtimeActions.Disable();
            else EnableGameplayActions();
        }

        private void Awake()
        {
            movement = GetComponent<PlayerMovement2D>();
            entity = GetComponent<PlayerEntity>();
            animationController = GetComponent<PlayerIdleAnimation>();
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
            if (runtimeActions == null || IsInputBlocked) return;
            EnableGameplayActions();
        }

        private void EnableGameplayActions()
        {
            move.Enable();
            jump.Enable();
            attack.Enable();
            interact.Enable();
        }

        private void Update()
        {
            if (!CanControlGameplay)
            {
                ClearGameplayInput();
                return;
            }
            Vector2 moveValue = move.ReadValue<Vector2>();
            movement.SetMoveInput(moveValue.x);
            if (entity != null && entity.isKnockback)
            {
                attackBuffer.Suppress();
                movement.ClearInput();
                animationController?.CancelBasicAttack();
                return;
            }
            if (jump.WasPressedThisFrame()) movement.RequestJump(moveValue.y < -0.5f);
            if (!CanPerformCombatAction) attackBuffer.Suppress();
            else if (attackBuffer.Read(attack.WasPressedThisFrame(), attack.IsPressed(),
                repeatAttackWhileHeld, Time.time, attackBufferDuration)
                && Time.timeScale > 0f && basicAttack != null && basicAttack.TryAttack())
                attackBuffer.Consume();
            if (interact.WasPressedThisFrame() && questInteraction != null)
                questInteraction.TryInteract();
        }

        private void OnDisable()
        {
            if (runtimeActions != null) runtimeActions.Disable();
            ClearGameplayInput();
        }

        private void ClearGameplayInput()
        {
            attackBuffer.Suppress();
            if (movement != null) movement.ClearInput();
            animationController?.CancelBasicAttack();
        }

        private void OnApplicationFocus(bool focused)
        {
            hasFocus = focused;
            if (!focused) ClearGameplayInput();
        }

        private void OnDestroy()
        {
            if (runtimeActions != null) Destroy(runtimeActions);
        }
    }
}
