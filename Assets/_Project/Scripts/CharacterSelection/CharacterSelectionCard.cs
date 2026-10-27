using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using EasternFantasy.Saving;

namespace EasternFantasy.CharacterSelection
{
    [DisallowMultipleComponent]
    public sealed class CharacterSelectionCard : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        [SerializeField] private CharacterClassDefinition definition;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Image cardBackground;
        [SerializeField] private TMP_Text classNameText;
        [SerializeField] private TMP_Text roleText;
        [SerializeField] private TMP_Text availabilityText;

        private static readonly Color NormalColor = new Color(1f, 1f, 1f, 1f);
        private static readonly Color HoverColor = new Color(1f, 1f, 0.45f, 1f);
        private const float HoverScale = 1.035f;

        private CharacterSelectionController controller;
        private Vector3 normalScale;
        private TMP_Text savedDataText;

        public CharacterClassDefinition Definition => definition;

        public void Initialize(CharacterSelectionController owner)
        {
            controller = owner;
            normalScale = transform.localScale;

            if (definition == null)
                return;

            portraitImage.sprite = definition.Portrait;
            portraitImage.enabled = definition.Portrait != null;
            classNameText.text = definition.DisplayName;
            roleText.text = definition.CombatRole;
            availabilityText.text = definition.Playable ? "선택 가능" : "준비 중";
            availabilityText.color = definition.Playable
                ? new Color(0f, 0f, 0f, 1f)
                : new Color(0f, 0f, 0f, 1f);
            cardBackground.color = NormalColor;
            RefreshSavedData();
        }

        public void RefreshSavedData()
        {
            if (definition == null) return;
            if (savedDataText == null)
            {
                var label = new GameObject("SavedData", typeof(RectTransform), typeof(TextMeshProUGUI));
                // A sibling caption follows the frame's center, independent of card hover scaling.
                label.transform.SetParent(transform.parent, false);
                savedDataText = label.GetComponent<TextMeshProUGUI>();
                savedDataText.font = availabilityText.font;
                savedDataText.fontSharedMaterial = availabilityText.fontSharedMaterial;
                savedDataText.fontSize = 21f;
                savedDataText.enableAutoSizing = true;
                savedDataText.fontSizeMin = 18f;
                savedDataText.fontSizeMax = 21f;
                savedDataText.alignment = TextAlignmentOptions.Center;
                savedDataText.textWrappingMode = TextWrappingModes.NoWrap;
                savedDataText.color = new Color(1f, .89f, .64f, 1f);
                savedDataText.outlineColor = new Color(0.04f, 0.03f, 0.02f, 1f);
                savedDataText.outlineWidth = .18f;
                savedDataText.raycastTarget = false;
                var rect = savedDataText.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                var corners = new Vector3[4];
                cardBackground.rectTransform.GetWorldCorners(corners);
                var parent = savedDataText.transform.parent;
                Vector3 left = parent.InverseTransformPoint(corners[0]);
                Vector3 right = parent.InverseTransformPoint(corners[3]);
                rect.localPosition = new Vector3((left.x + right.x) * .5f, Mathf.Min(left.y, right.y) - 32f, 0f);
                rect.sizeDelta = new Vector2(Mathf.Abs(right.x - left.x) + 24f, 36f);
                savedDataText.margin = Vector4.zero;
            }
            bool exists = SaveGameService.TryGetSavedLevel(definition.ClassId, out int level);
            savedDataText.text = exists ? $"저장된 데이터 존재 - 레벨 : {level:00}" : string.Empty;
            savedDataText.gameObject.SetActive(exists);
        }

        private void OnEnable()
        {
            if (savedDataText != null) RefreshSavedData();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            controller?.ShowDetails(definition);
            cardBackground.color = HoverColor;
            transform.localScale = normalScale * HoverScale;
            transform.SetAsLastSibling();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            cardBackground.color = NormalColor;
            transform.localScale = normalScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                controller?.SelectClass(definition);
        }

        private void OnDisable()
        {
            transform.localScale = normalScale == Vector3.zero ? Vector3.one : normalScale;
            if (savedDataText != null) savedDataText.gameObject.SetActive(false);
        }
        private void OnDestroy()
        {
            if (savedDataText != null) Destroy(savedDataText.gameObject);
        }
    }
}
