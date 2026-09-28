using EasternFantasy.Skill;
using EasternFantasy.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class QuickSkillSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text placeholderText;
        [SerializeField] private TMP_Text cooldownText;
        [SerializeField] private TMP_Text hotkeyText;

        private PlayerSkillSystem skillSystem;
        private IPlayerSkillCaster caster;
        private int slotIndex;
        private Image cooldownFill;
        private GameObject hintRoot;
        private TMP_Text hintText;
        private bool hovered;
        private GameObject emptyLock;
        private Vector2 placeholderBasePosition;

        private void Awake()
        {
            Image background = GetComponent<Image>();
            if (background != null)
            {
                background.color = new Color(0.035f, 0.075f, 0.09f, 0.96f);
                Outline border = gameObject.AddComponent<Outline>();
                border.effectColor = new Color(0.72f, 0.54f, 0.25f, 0.95f);
                border.effectDistance = new Vector2(2f, -2f);
            }

            if (hotkeyText != null)
            {
                hotkeyText.enableAutoSizing = false;
                hotkeyText.fontSize = 24f;
                hotkeyText.fontStyle = FontStyles.Bold;
                hotkeyText.color = new Color(1f, 0.89f, 0.55f);
                Outline keyOutline = hotkeyText.gameObject.AddComponent<Outline>();
                keyOutline.effectColor = new Color(0.02f, 0.025f, 0.03f);
                keyOutline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            if (placeholderText != null)
                placeholderBasePosition = placeholderText.rectTransform.anchoredPosition;

            emptyLock = new GameObject("Empty Lock", typeof(RectTransform));
            emptyLock.transform.SetParent(transform, false);
            RectTransform lockRect = emptyLock.GetComponent<RectTransform>();
            lockRect.anchorMin = lockRect.anchorMax = new Vector2(0.5f, 0.5f);
            lockRect.anchoredPosition = new Vector2(0f, 10f);
            lockRect.sizeDelta = new Vector2(20f, 23f);
            CreateLockPart("Shackle", lockRect, new Vector2(14f, 13f),
                new Vector2(0f, 5f), new Color(0.82f, 0.67f, 0.37f));
            CreateLockPart("Shackle Opening", lockRect, new Vector2(8f, 9f),
                new Vector2(0f, 4f), new Color(0.035f, 0.075f, 0.09f));
            CreateLockPart("Body", lockRect, new Vector2(20f, 13f),
                new Vector2(0f, -5f), new Color(0.82f, 0.67f, 0.37f));
            emptyLock.transform.SetSiblingIndex(placeholderText != null
                ? placeholderText.transform.GetSiblingIndex() : transform.childCount - 1);

            GameObject fill = new GameObject("Radial Cooldown", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            fill.transform.SetParent(transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);
            fill.transform.SetSiblingIndex(iconImage != null
                ? iconImage.transform.GetSiblingIndex() + 1 : 1);
            cooldownFill = fill.GetComponent<Image>();
            cooldownFill.sprite = Sprite.Create(Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            cooldownFill.type = Image.Type.Filled;
            cooldownFill.fillMethod = Image.FillMethod.Radial360;
            cooldownFill.fillOrigin = (int)Image.Origin360.Top;
            cooldownFill.fillClockwise = false;
            cooldownFill.color = new Color(0.01f, 0.02f, 0.035f, 0.78f);
            cooldownFill.raycastTarget = false;
            cooldownFill.enabled = false;

            GameObject hint = new GameObject("Unavailable Reason", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            hint.transform.SetParent(transform, false);
            RectTransform hintRect = hint.GetComponent<RectTransform>();
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 1f);
            hintRect.pivot = new Vector2(1f, 0f);
            hintRect.anchoredPosition = new Vector2(42f, 12f);
            hintRect.sizeDelta = new Vector2(230f, 55f);
            Image hintBackground = hint.GetComponent<Image>();
            hintBackground.color = new Color(0.025f, 0.04f, 0.045f, 0.96f);
            hintBackground.raycastTarget = false;
            GameObject label = new GameObject("Reason", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(hint.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 3f);
            labelRect.offsetMax = new Vector2(-8f, -3f);
            hintText = label.GetComponent<TextMeshProUGUI>();
            if (placeholderText != null)
                hintText.font = placeholderText.font;
            hintText.fontSize = 17f;
            hintText.color = new Color(1f, 0.86f, 0.62f);
            hintText.alignment = TextAlignmentOptions.Center;
            hintText.raycastTarget = false;
            hintRoot = hint;
            hintRoot.SetActive(false);
        }

        public void Initialize(PlayerSkillSystem system, IPlayerSkillCaster activeCaster, int index)
        {
            skillSystem = system;
            caster = activeCaster;
            slotIndex = index;
            if (hotkeyText != null)
                hotkeyText.text = QuickSlotBarUI.SkillHotkeys[index].ToString();
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
                placeholderText.gameObject.SetActive(hasSkill && !hasIcon);
                placeholderText.text = hasSkill ? skill.DisplayName : string.Empty;
                placeholderText.color = hasSkill ? Color.white
                    : new Color(0.76f, 0.69f, 0.53f);
                placeholderText.rectTransform.anchoredPosition = hasSkill
                    ? placeholderBasePosition : new Vector2(placeholderBasePosition.x, -22f);
            }
            if (emptyLock != null)
                emptyLock.SetActive(!hasSkill);

            RefreshCooldown();
        }

        public void RefreshCooldown()
        {
            SkillDefinition skill = GetSkill();
            float remaining = caster != null && skill != null
                ? caster.GetCooldownRemaining(skill) : 0f;
            if (cooldownText != null)
                cooldownText.text = remaining > 0.05f
                    ? Mathf.CeilToInt(remaining).ToString() : string.Empty;
            if (cooldownFill != null)
            {
                cooldownFill.enabled = remaining > 0.05f && skill != null;
                cooldownFill.fillAmount = skill != null && skill.CooldownSeconds > 0f
                    ? Mathf.Clamp01(remaining / skill.CooldownSeconds) : 0f;
            }
            if (hovered)
                RefreshHint();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            RefreshHint();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            if (hintRoot != null)
                hintRoot.SetActive(false);
        }

        private void RefreshHint()
        {
            if (hintRoot == null)
                return;
            string reason = GetUnavailableReason();
            hintText.text = reason;
            hintRoot.SetActive(hovered && !string.IsNullOrEmpty(reason));
        }

        public string GetUnavailableReason()
        {
            SkillDefinition skill = GetSkill();
            PlayerEntity player = skillSystem != null
                ? skillSystem.GetComponent<PlayerEntity>() : null;
            return skill == null ? "K 스킬창에서 스킬을 등록하세요"
                : player != null && player.IsDead ? "사망 중에는 사용할 수 없습니다"
                : Time.timeScale <= 0f ? "현재 사용할 수 없습니다"
                : skillSystem.GetSkillLevel(skill) <= 0 ? "스킬을 배우지 않았습니다"
                : caster == null ? "사용할 수 없는 캐릭터입니다"
                : caster.GetCooldownRemaining(skill) > 0.05f
                    ? $"재사용 대기 중 ({Mathf.CeilToInt(caster.GetCooldownRemaining(skill))}초)"
                : player != null && player.Energy != null
                    && skill.ResourceCost == SkillResourceCost.FullEnergy
                    && !player.Energy.IsFull
                    ? "기력이 완전히 충전되어야 합니다"
                : player != null && skill.ResourceCost == SkillResourceCost.Mana
                    && player.MP < skill.ManaCost
                    ? $"MP 부족 (필요 {skill.ManaCost:0})"
                : string.Empty;
        }

        private static void CreateLockPart(string objectName, Transform parent,
            Vector2 size, Vector2 position, Color color)
        {
            GameObject part = new GameObject(objectName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            part.transform.SetParent(parent, false);
            RectTransform rect = part.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = part.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
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
