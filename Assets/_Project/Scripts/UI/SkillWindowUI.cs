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
        public bool IsUnlocked => StatsSkillTutorialUI.HasFinishedForCurrentClass;
        public bool IsOpen => windowRoot != null && windowRoot.activeSelf;
        public RectTransform TutorialTarget => windowRoot != null ? windowRoot.transform as RectTransform : null;
        public RectTransform TutorialPointsTarget => skillPointText != null ? skillPointText.rectTransform : null;
        public System.Collections.Generic.IReadOnlyList<SkillSlotUI> TutorialSlots => skillSlots;

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
            PositionTierHeaders();
            skillSlotsInitialized = true;
            skillSystem.SkillsChanged += Refresh;
        }

        private void PositionTierHeaders()
        {
            PositionTierHeader("BaseHeader", SkillTier.Base);
            PositionTierHeader("FirstHeader", SkillTier.FirstAdvancement);
        }

        private void PositionTierHeader(string headerName, SkillTier tier)
        {
            if (windowRoot == null) return;
            Transform header = windowRoot.transform.Find(headerName);
            if (header == null) return;
            RectTransform topSlot = null;
            foreach (SkillSlotUI slot in skillSlots)
            {
                if (slot == null || slot.Definition == null || slot.Definition.Tier != tier) continue;
                RectTransform rect = slot.transform as RectTransform;
                if (topSlot == null || rect.anchoredPosition.y > topSlot.anchoredPosition.y)
                    topSlot = rect;
            }
            if (topSlot == null) return;
            RectTransform headerRect = header as RectTransform;
            headerRect.anchoredPosition = new Vector2(topSlot.anchoredPosition.x,
                topSlot.anchoredPosition.y + topSlot.rect.height * 0.5f + headerRect.rect.height * 0.5f + 12f);
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
            if (MapTutorialUI.IsRunning || StatsSkillTutorialUI.IsRunning || InventoryEquipmentTutorialUI.IsRunning || DungeonWindowUI.IsTutorialRunning || EquipmentUpgradeWindowUI.IsTutorialRunning || ShopWindowUI.IsTutorialRunning) return;
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

        public void SetOpen(bool open) { SetOpenInternal(open, false); }
        internal void SetOpenForTutorial(bool open) { SetOpenInternal(open, true); }
        private void SetOpenInternal(bool open, bool forTutorial)
        {
            if (open && !forTutorial && !IsUnlocked) return;
            if (open && EquipmentUpgradeWindowUI.Instance != null && EquipmentUpgradeWindowUI.Instance.IsOpen) return;
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
