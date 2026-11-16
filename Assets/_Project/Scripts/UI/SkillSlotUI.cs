using EasternFantasy.Skill;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class SkillSlotUI : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [SerializeField] private Button learnButton;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text placeholderText;
        [SerializeField] private SkillTooltipUI tooltip;
        [SerializeField] private GameObject lockOverlay;

        private PlayerSkillSystem skillSystem;
        private SkillDefinition definition;
        private CanvasGroup canvasGroup;
        private GameObject dragVisual;
        private RectTransform learnBadge;

        public SkillDefinition Definition => definition;

        public void Initialize(
            PlayerSkillSystem system,
            SkillDefinition skill,
            SkillTooltipUI tooltipUI)
        {
            skillSystem = system;
            definition = skill;
            tooltip = tooltipUI;
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            EnsureLockOverlay();
            learnButton.onClick.RemoveListener(Learn);
            learnButton.onClick.AddListener(Learn);
            Refresh();
        }

        public void Refresh()
        {
            if (definition == null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
                placeholderText.text = "준비 중";
                placeholderText.gameObject.SetActive(true);
                learnButton.interactable = false;
                if (lockOverlay != null) lockOverlay.SetActive(false);
                if (learnBadge != null) learnBadge.gameObject.SetActive(false);
                return;
            }
            if (skillSystem == null)
                return;

            bool hasIcon = definition.Icon != null;
            iconImage.sprite = definition.Icon;
            iconImage.enabled = hasIcon;
            placeholderText.gameObject.SetActive(!hasIcon);
            learnButton.interactable = skillSystem.CanLearn(definition);
            if (lockOverlay != null)
            {
                bool isAcquired = skillSystem.GetSkillLevel(definition) > 0;
                lockOverlay.SetActive(!isAcquired);
                lockOverlay.transform.SetAsLastSibling();
            }
            EnsureLearnBadge();
            learnBadge.gameObject.SetActive(learnButton.interactable);
            learnBadge.SetAsLastSibling();
        }

        private void EnsureLearnBadge()
        {
            if (learnBadge != null) return;
            var badge = new GameObject("LearnPointBadge", typeof(RectTransform), typeof(Image), typeof(Outline));
            badge.layer = gameObject.layer;
            badge.transform.SetParent(transform, false);
            learnBadge = (RectTransform)badge.transform;
            learnBadge.anchorMin = learnBadge.anchorMax = learnBadge.pivot = new Vector2(1f, 0f);
            learnBadge.anchoredPosition = new Vector2(-2f, 2f);
            learnBadge.sizeDelta = new Vector2(24f, 24f);
            var image = badge.GetComponent<Image>();
            image.color = new Color(.19f, .12f, .055f);
            image.raycastTarget = false;
            var outline = badge.GetComponent<Outline>();
            outline.effectColor = new Color(.95f, .72f, .28f);
            outline.effectDistance = new Vector2(1f, -1f);
            var labelObject = new GameObject("Plus", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.layer = gameObject.layer;
            labelObject.transform.SetParent(badge.transform, false);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.font = placeholderText != null ? placeholderText.font : TMP_Settings.defaultFontAsset;
            label.text = "+";
            label.fontSize = 23f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(1f, .88f, .56f);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }

        private void EnsureLockOverlay()
        {
            if (lockOverlay != null)
                return;

            Transform existing = transform.Find("LockOverlay");
            if (existing != null)
            {
                lockOverlay = existing.gameObject;
                return;
            }

            lockOverlay = new GameObject("LockOverlay", typeof(RectTransform), typeof(Image));
            lockOverlay.layer = gameObject.layer;
            lockOverlay.transform.SetParent(transform, false);

            RectTransform overlayRect = lockOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image shade = lockOverlay.GetComponent<Image>();
            shade.color = new Color(0.02f, 0.025f, 0.035f, 0.72f);
            shade.raycastTarget = false;

            Color lockColor = new Color(0.92f, 0.74f, 0.34f, 1f);
            CreateLockPiece("ShackleTop", new Vector2(0f, 10f), new Vector2(30f, 6f), lockColor);
            CreateLockPiece("ShackleLeft", new Vector2(-12f, 1f), new Vector2(6f, 20f), lockColor);
            CreateLockPiece("ShackleRight", new Vector2(12f, 1f), new Vector2(6f, 20f), lockColor);
            CreateLockPiece("LockBody", new Vector2(0f, -14f), new Vector2(40f, 30f), lockColor);
            lockOverlay.transform.SetAsLastSibling();
        }

        private void CreateLockPiece(string objectName, Vector2 position, Vector2 size, Color color)
        {
            GameObject piece = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            piece.layer = gameObject.layer;
            piece.transform.SetParent(lockOverlay.transform, false);

            RectTransform rect = piece.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = piece.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private void Learn()
        {
            if (skillSystem != null && skillSystem.LearnSkill(definition))
                tooltip?.Show(definition, skillSystem, transform as RectTransform);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            tooltip?.Show(definition, skillSystem, transform as RectTransform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltip?.HideAfterPointerExit();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (definition == null || skillSystem == null
                || !skillSystem.CanPlaceInQuickSlot(definition))
                return;

            canvasGroup.blocksRaycasts = false;
            tooltip?.Hide();
            Canvas canvas = GetComponentInParent<Canvas>();
            dragVisual = new GameObject("SkillDragIcon", typeof(RectTransform), typeof(Image));
            dragVisual.transform.SetParent(canvas.transform, false);
            RectTransform rect = dragVisual.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(64f, 64f);
            Image image = dragVisual.GetComponent<Image>();
            image.sprite = definition.Icon;
            image.color = definition.Icon != null ? Color.white : new Color(0.8f, 0.7f, 0.35f, 0.9f);
            image.raycastTarget = false;
            Canvas dragCanvas = dragVisual.AddComponent<Canvas>();
            dragCanvas.overrideSorting = true;
            dragCanvas.sortingOrder = 500;
            dragVisual.transform.position = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragVisual != null)
                dragVisual.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;
            if (dragVisual != null)
                Destroy(dragVisual);
        }
    }
}
