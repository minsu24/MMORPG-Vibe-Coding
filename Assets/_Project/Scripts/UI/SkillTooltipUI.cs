using EasternFantasy.Skill;
using TMPro;
using UnityEngine;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class SkillTooltipUI : MonoBehaviour
    {
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;

        public void Hide()
        {
            if (tooltipRoot != null)
                tooltipRoot.SetActive(false);
        }

        public void Show(
            SkillDefinition skill,
            PlayerSkillSystem skillSystem,
            RectTransform hoveredSlot)
        {
            if (skill == null || skillSystem == null || tooltipRoot == null)
                return;

            int level = skillSystem.GetSkillLevel(skill);
            titleText.text = $"{skill.DisplayName}   Lv. {level} / {skill.MaximumLevel}";

            string requirement = skillSystem.GetRequirementText(skill);
            string typeText = skill.ActivationType == SkillActivationType.Passive
                ? "패시브"
                : $"액티브 · 재사용 대기시간 {skill.CooldownSeconds:0.#}초";
            descriptionText.text = typeText + "\n\n" + skill.ShortDescription;
            if (!string.IsNullOrWhiteSpace(skill.DetailedDescription))
                descriptionText.text += "\n\n" + skill.DetailedDescription;
            if (!string.IsNullOrWhiteSpace(requirement))
                descriptionText.text += "\n\n" + requirement;

            RectTransform tooltipRect = tooltipRoot.GetComponent<RectTransform>();
            float side = hoveredSlot.anchoredPosition.x < 0f ? -1f : 1f;
            tooltipRect.anchoredPosition = new Vector2(
                510f * side,
                Mathf.Clamp(hoveredSlot.anchoredPosition.y, -120f, 120f));
            tooltipRoot.SetActive(true);
        }
    }
}
