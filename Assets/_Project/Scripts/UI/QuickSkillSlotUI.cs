using EasternFantasy.Skill;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class QuickSkillSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text placeholderText;
        [SerializeField] private TMP_Text cooldownText;
        [SerializeField] private TMP_Text hotkeyText;

        private PlayerSkillSystem skillSystem;
        private PlayerActiveSkillCaster caster;
        private int slotIndex;

        public void Initialize(PlayerSkillSystem system, PlayerActiveSkillCaster activeCaster, int index)
        {
            skillSystem = system;
            caster = activeCaster;
            slotIndex = index;
            if (hotkeyText != null)
                hotkeyText.text = (index + 1).ToString();
            Refresh();
        }

        public void Refresh()
        {
            SkillDefinition skill = GetSkill();
            bool hasSkill = skill != null;
            bool hasIcon = hasSkill && skill.Icon != null;
            if (iconImage != null)
            {
                iconImage.sprite = hasIcon ? skill.Icon : null;
                iconImage.enabled = hasIcon;
            }
            if (placeholderText != null)
            {
                placeholderText.gameObject.SetActive(!hasIcon);
                placeholderText.text = hasSkill ? skill.DisplayName : string.Empty;
            }

            RefreshCooldown();
        }

        public void RefreshCooldown()
        {
            if (cooldownText == null)
                return;

            float remaining = caster != null ? caster.GetCooldownRemaining(GetSkill()) : 0f;
            cooldownText.text = remaining > 0.05f ? Mathf.CeilToInt(remaining).ToString() : string.Empty;
        }

        public void OnDrop(PointerEventData eventData)
        {
            SkillSlotUI source = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<SkillSlotUI>()
                : null;
            if (source != null && skillSystem != null)
                skillSystem.AssignQuickSlot(slotIndex, source.Definition);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right && skillSystem != null)
                skillSystem.ClearQuickSlot(slotIndex);
        }

        private SkillDefinition GetSkill()
        {
            return skillSystem != null && slotIndex >= 0 && slotIndex < skillSystem.QuickSlots.Count
                ? skillSystem.QuickSlots[slotIndex]
                : null;
        }
    }
}
