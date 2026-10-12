using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
namespace EasternFantasy.UI
{
    public sealed class ScrollCodexAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform viewport, leftRoller, rightRoller;
        [SerializeField] private CanvasGroup rollers, boundScroll;
        [SerializeField, Min(.01f)] private float duration = .65f;
        [SerializeField] private UnityEvent opened = new UnityEvent(), closed = new UnityEvent();
        private Sequence sequence;
        private float progress;
        public bool IsOpen { get; private set; }
        public bool IsAnimating => sequence != null && sequence.IsActive();
        public UnityEvent Opened => opened;
        public UnityEvent Closed => closed;
        public void Configure(RectTransform mask, RectTransform left, RectTransform right, CanvasGroup unrolled, CanvasGroup bound)
        {
            viewport = mask; leftRoller = left; rightRoller = right;
            rollers = unrolled; boundScroll = bound; SetOpenInstant(false);
        }
        public void Open() => Animate(true);
        public void Close() => Animate(false);
        public void Toggle() => Animate(!IsOpen);
        private void Animate(bool open)
        {
            if (!viewport || (IsOpen == open && IsAnimating)) return;
            sequence?.Kill(); IsOpen = open;
            sequence = DOTween.Sequence().SetUpdate(true);
            if (open) {
                sequence.Append(boundScroll.DOFade(0, .16f));
                sequence.Join(rollers.DOFade(1, .16f));
            }
            sequence.Append(DOTween.To(() => progress, ApplyProgress, open ? 1 : 0,
                Mathf.Max(.01f, duration * Mathf.Abs((open ? 1 : 0) - progress))).SetEase(open ? Ease.OutCubic : Ease.InCubic));
            if (!open) {
                sequence.Append(rollers.DOFade(0, .16f));
                sequence.Join(boundScroll.DOFade(1, .16f));
            }
            sequence.OnComplete(() => { sequence = null; if (open) opened.Invoke(); else closed.Invoke(); });
        }
        public void SetOpenInstant(bool open)
        {
            sequence?.Kill(); sequence = null; IsOpen = open;
            if (!viewport) return;
            ApplyProgress(open ? 1 : 0);
            rollers.alpha = open ? 1 : 0; boundScroll.alpha = open ? 0 : 1;
        }
        // Reveal the full-size parchment with a mask, never by scaling its width.
        private void ApplyProgress(float value)
        {
            progress = Mathf.Clamp01(value);
            viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1412 * progress);
            float x = Mathf.Lerp(65, 758, progress);
            leftRoller.anchoredPosition = new Vector2(-x, 0);
            rightRoller.anchoredPosition = new Vector2(x, 0);
        }
        private void OnDisable() => SetOpenInstant(false);
        private void OnDestroy() => sequence?.Kill();
    }
}
