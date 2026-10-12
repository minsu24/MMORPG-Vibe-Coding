using DG.Tweening;
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
        private Sequence hoverTween;

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

        private void SetHighlightState(bool pointer, bool selection)
        {
            bool wasHighlighted = IsHighlighted;
            bool wasPointerInside = pointerInside;
            pointerInside = pointer;
            selected = selection;
            if (!isActiveAndEnabled || (wasHighlighted == IsHighlighted && wasPointerInside == pointerInside))
                return;

            hoverTween?.Kill();
            hoverTween = null;
            // Mouse hover stays distinct even when EventSystem selected this button first.
            float emphasis = pointerInside ? 1f : (selected ? 0.5f : 0f);
            Vector3 targetScale = normalScale * Mathf.Lerp(1f, hoverScale, emphasis);
            Color targetColor = Color.Lerp(normalColor, hoverColor, emphasis);
            if (transitionSpeed <= 0f)
            {
                rectTransform.localScale = targetScale;
                if (targetGraphic != null)
                    targetGraphic.color = targetColor;
                return;
            }

            // Preserve the existing Inspector speed: higher values produce shorter tweens.
            float duration = 3f / transitionSpeed;
            hoverTween = DOTween.Sequence().SetUpdate(true);
            hoverTween.Append(rectTransform.DOScale(targetScale, duration)
                .SetEase(IsHighlighted ? Ease.OutBack : Ease.OutQuad));
            if (targetGraphic != null)
                hoverTween.Join(targetGraphic.DOColor(targetColor, duration).SetEase(Ease.OutQuad));
        }

        public void OnPointerEnter(PointerEventData eventData) => SetHighlightState(true, selected);
        public void OnPointerExit(PointerEventData eventData) => SetHighlightState(false, selected);
        public void OnSelect(BaseEventData eventData) => SetHighlightState(pointerInside, true);
        public void OnDeselect(BaseEventData eventData) => SetHighlightState(pointerInside, false);

        private void OnDisable()
        {
            hoverTween?.Kill();
            hoverTween = null;
            pointerInside = false;
            selected = false;

            if (rectTransform != null)
                rectTransform.localScale = normalScale;
            if (targetGraphic != null)
                targetGraphic.color = normalColor;
        }

        private void OnDestroy() => hoverTween?.Kill();
    }
}
