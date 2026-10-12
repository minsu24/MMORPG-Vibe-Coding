using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [RequireComponent(typeof(RectTransform), typeof(Button))]
    public sealed class UpgradeConfirmFeedback : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField, Range(0.5f, 1f)] private float pressedScale = 0.92f;
        [SerializeField, Min(0.01f)] private float pressDuration = 0.06f;
        [SerializeField, Min(0.01f)] private float returnDuration = 0.16f;
        private EquipmentUpgradeWindowUI window;
        private RectTransform rect;
        private Vector3 normalScale;
        private Sequence feedbackSequence;

        private void Awake()
        {
            rect = (RectTransform)transform;
            normalScale = rect.localScale;
        }

        public void Initialize(EquipmentUpgradeWindowUI owner) => window = owner;

        public void PlayFeedback(bool available)
        {
            if (!isActiveAndEnabled) return;
            feedbackSequence?.Kill();
            rect.localScale = normalScale;
            float downTime = Mathf.Max(0.01f, pressDuration);
            float upTime = Mathf.Max(0.01f, returnDuration);
            feedbackSequence = DOTween.Sequence().SetUpdate(true);
            feedbackSequence.Append(rect.DOScale(normalScale * pressedScale, downTime).SetEase(Ease.OutQuad));
            feedbackSequence.Append(rect.DOScale(normalScale, upTime).SetEase(Ease.OutBack));
            if (!available)
            {
                feedbackSequence.Append(rect.DOScale(normalScale * pressedScale, downTime).SetEase(Ease.OutQuad));
                feedbackSequence.Append(rect.DOScale(normalScale, upTime).SetEase(Ease.OutQuad));
            }
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left && !GetComponent<Button>().interactable)
                window?.Confirm();
        }

        private void OnDisable()
        {
            feedbackSequence?.Kill();
            feedbackSequence = null;
            if (rect != null) rect.localScale = normalScale;
        }

        private void OnDestroy() => feedbackSequence?.Kill();
    }
}
