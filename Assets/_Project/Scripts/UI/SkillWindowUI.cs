using EasternFantasy.Dialogue;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Player;
using EasternFantasy.Skill;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class SkillWindowUI : MonoBehaviour
    {
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private TMP_Text skillPointText;
        [SerializeField] private SkillSlotUI[] skillSlots;
        [SerializeField] private SkillTooltipUI tooltip;
        [SerializeField] private PlayerSkillSystem skillSystem;

        private bool skillSlotsInitialized;

        public static SkillWindowUI Instance { get; private set; }
        public bool IsOpen => windowRoot != null && windowRoot.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (windowRoot != null)
                windowRoot.SetActive(false);
        }

        private void Start()
        {
            BindCurrentSkillSystem();
            Refresh();
        }

        private void BindCurrentSkillSystem()
        {
            PlayerSkillSystem current = FindCurrentSkillSystem();
            if (current == skillSystem && current != null && skillSlotsInitialized)
                return;
            if (skillSystem != null)
                skillSystem.SkillsChanged -= Refresh;
            skillSystem = current;
            skillSlotsInitialized = false;
            if (skillSystem == null)
                return;

            for (int i = 0; i < skillSlots.Length; i++)
                if (skillSlots[i] != null)
                    skillSlots[i].Initialize(skillSystem,
                        i < skillSystem.AvailableSkills.Count
                            ? skillSystem.AvailableSkills[i] : null, tooltip);
            skillSlotsInitialized = true;
            skillSystem.SkillsChanged += Refresh;
        }

        private static PlayerSkillSystem FindCurrentSkillSystem()
        {
            foreach (PlayerSkillSystem candidate in
                FindObjectsByType<PlayerSkillSystem>(FindObjectsSortMode.None))
            {
                PlayerClassRuntime playerClass = candidate.GetComponent<PlayerClassRuntime>();
                if (playerClass != null
                    && playerClass.RepresentedClass == CharacterSelectionState.SelectedClass)
                    return candidate;
            }

            return FindFirstObjectByType<PlayerSkillSystem>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (IsOpen && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetOpen(false);
                return;
            }

            if (!keyboard.kKey.wasPressedThisFrame)
                return;

            bool dialogueBlocksOpening = !IsOpen
                && DialogueManager.Instance != null
                && DialogueManager.Instance.IsDialogueActive;
            if (!dialogueBlocksOpening)
                SetOpen(!IsOpen);
        }

        public void SetOpen(bool open)
        {
            if (windowRoot == null)
                return;

            if (open && PlayerStatsWindowUI.Instance != null)
                PlayerStatsWindowUI.Instance.SetOpen(false);
            if (open && InventoryWindowUI.Instance != null)
                InventoryWindowUI.Instance.SetOpen(false);

            if (open)
                Refresh();
            else if (tooltip != null)
                tooltip.Hide();
            windowRoot.SetActive(open);
        }

        public void Refresh()
        {
            BindCurrentSkillSystem();
            if (skillSystem == null)
                return;

            if (skillPointText != null)
                skillPointText.text = $"보유 스킬 포인트  {skillSystem.SkillPoints}";

            foreach (SkillSlotUI slot in skillSlots)
                if (slot != null)
                    slot.Refresh();
        }

        private void OnDestroy()
        {
            if (skillSystem != null)
                skillSystem.SkillsChanged -= Refresh;

            if (Instance == this)
                Instance = null;
        }
    }
}
