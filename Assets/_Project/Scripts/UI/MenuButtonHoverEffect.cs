using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class MenuButtonHoverEffect : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [Header("Optional - defaults to the Graphic on this object")]
        [SerializeField] private Graphic targetGraphic;

        [Header("Hover appearance")]
        [SerializeField, Min(1f)] private float hoverScale = 1.06f;
        [SerializeField] private Color hoverColor = new Color(1f, 0.85f, 0.45f, 1f);
        [SerializeField, Min(0f)] private float transitionSpeed = 14f;

        private RectTransform rectTransform;
        private Vector3 normalScale;
        private Color normalColor;
        private bool pointerInside;
        private bool selected;

        private bool IsHighlighted => pointerInside || selected;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            if (targetGraphic == null)
                targetGraphic = GetComponent<Graphic>();

            normalScale = rectTransform.localScale;
            if (targetGraphic != null)
                normalColor = targetGraphic.color;
        }

        private void Update()
        {
            float blend = transitionSpeed <= 0f
                ? 1f
                : 1f - Mathf.Exp(-transitionSpeed * Time.unscaledDeltaTime);

            Vector3 targetScale = IsHighlighted
                ? normalScale * hoverScale
                : normalScale;
            rectTransform.localScale = Vector3.Lerp(
                rectTransform.localScale,
                targetScale,
                blend);

            if (targetGraphic != null)
            {
                Color targetColor = IsHighlighted ? hoverColor : normalColor;
                targetGraphic.color = Color.Lerp(targetGraphic.color, targetColor, blend);
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => pointerInside = true;
        public void OnPointerExit(PointerEventData eventData) => pointerInside = false;
        public void OnSelect(BaseEventData eventData) => selected = true;
        public void OnDeselect(BaseEventData eventData) => selected = false;

        private void OnDisable()
        {
            pointerInside = false;
            selected = false;

            if (rectTransform != null)
                rectTransform.localScale = normalScale;
            if (targetGraphic != null)
                targetGraphic.color = normalColor;
        }
    }
}
