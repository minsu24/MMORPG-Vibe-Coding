using EasternFantasy.Skill;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class SkillTooltipUI : MonoBehaviour
    {
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;

        private bool pendingPointerHide;
        private float hideAfter;
        public void HideAfterPointerExit()
        {
            pendingPointerHide = true;
            hideAfter = Time.unscaledTime + .15f;
        }
        private void Update()
        {
            if (!pendingPointerHide || Time.unscaledTime < hideAfter) return;
            Canvas canvas = tooltipRoot != null ? tooltipRoot.GetComponentInParent<Canvas>() : null;
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (tooltipRoot != null && Mouse.current != null &&
                RectTransformUtility.RectangleContainsScreenPoint(tooltipRoot.transform as RectTransform,
                    Mouse.current.position.ReadValue(), camera)) return;
            Hide();
        }
        public void Hide()
        {
            pendingPointerHide = false;
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
            float cooldown = skill.GetCooldownSeconds(level);
            string typeText = skill.ActivationType == SkillActivationType.Passive
                ? "패시브"
                : cooldown > 0f
                    ? $"액티브 · 재사용 대기시간 {cooldown:0.#}초"
                    : "액티브 · 재사용 대기시간 없음";
            if (skill.ActivationType == SkillActivationType.Active)
            {
                switch (skill.ResourceCost)
                {
                    case SkillResourceCost.Energy:
                        typeText += $" · 기력 {skill.EnergyCost:0.#} 소모";
                        break;
                    case SkillResourceCost.FullEnergy:
                        typeText += " · 기력 완충 시 사용 · 기력 전량 소모";
                        break;
                    case SkillResourceCost.Mana:
                        typeText += $" · MP {skill.ManaCost:0.#} 소모";
                        break;
                    case SkillResourceCost.None:
                        typeText += " · 자원 소모 없음";
                        break;
                }
            }
            descriptionText.text = typeText + "\n\n" + skill.ShortDescription;
            if (!string.IsNullOrWhiteSpace(skill.DetailedDescription))
                descriptionText.text += "\n\n" + skill.DetailedDescription;
            string levelEffect = skill.GetLevelEffectDescription(Mathf.Max(1, level));
            if (!string.IsNullOrEmpty(levelEffect))
            {
                descriptionText.text += $"\n\n{(level > 0 ? "현재 효과" : "습득 시 효과")}: {levelEffect}";
                if (level > 0 && level < skill.MaximumLevel)
                    descriptionText.text += "\n다음 레벨: " + skill.GetLevelEffectDescription(level + 1);
            }
            if (!string.IsNullOrWhiteSpace(requirement))
                descriptionText.text += "\n\n" + requirement;

            RectTransform tooltipRect = tooltipRoot.GetComponent<RectTransform>();
            pendingPointerHide = false;
            tooltipRoot.SetActive(true);
            UIReadability.LayoutTooltip(tooltipRoot, titleText, descriptionText);
            // Place the scrollable tooltip beside the icon so it cannot intercept learning clicks.
            Canvas canvas = tooltipRoot.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas.rootCanvas.transform as RectTransform;
            Vector3[] corners = new Vector3[4];
            hoveredSlot.GetWorldCorners(corners);
            Vector3 left = canvasRect.InverseTransformPoint(corners[0]);
            Vector3 right = canvasRect.InverseTransformPoint(corners[2]);
            float width = tooltipRect.rect.width * tooltipRect.lossyScale.x / canvasRect.lossyScale.x;
            float centerX = right.x + 16f + width * 0.5f;
            if (centerX + width * 0.5f > canvasRect.rect.xMax - 18f)
                centerX = left.x - 16f - width * 0.5f;
            tooltipRect.position = canvasRect.TransformPoint(new Vector3(
                centerX, (left.y + right.y) * 0.5f, 0f));
            UIReadability.ClampToCanvas(tooltipRect);
        }
    }
}
