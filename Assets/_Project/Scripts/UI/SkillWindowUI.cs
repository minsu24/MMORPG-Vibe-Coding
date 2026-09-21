using EasternFantasy.Dialogue;
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
            if (skillSystem == null)
                skillSystem = FindFirstObjectByType<PlayerSkillSystem>();

            if (skillSystem == null)
            {
                Debug.LogError("SkillWindowUI needs a PlayerSkillSystem.", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < skillSlots.Length && i < skillSystem.AvailableSkills.Count; i++)
                skillSlots[i].Initialize(skillSystem, skillSystem.AvailableSkills[i], tooltip);

            skillSystem.SkillsChanged += Refresh;
            Refresh();
        }

        private void Update()
        {
            if (Keyboard.current?.kKey.wasPressedThisFrame != true)
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
