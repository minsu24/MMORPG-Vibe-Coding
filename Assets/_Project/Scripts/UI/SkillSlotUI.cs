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
            if (skillSystem == null || definition == null)
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
            tooltip?.Hide();
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
