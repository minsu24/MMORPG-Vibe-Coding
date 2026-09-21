using System.Collections;
using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class LevelUpEffectUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerProgression playerProgression;
        [SerializeField] private CanvasGroup effectGroup;
        [SerializeField] private Image effectImage;

        [Header("Timing (uses unscaled time)")]
        [SerializeField, Min(0.01f)] private float flashInDuration = 0.08f;
        [SerializeField, Min(0f)] private float holdDuration = 0.35f;
        [SerializeField, Min(0.01f)] private float fadeOutDuration = 1.25f;

        [Header("Scale punch")]
        [SerializeField, Min(0.01f)] private float startScale = 1.12f;
        [SerializeField, Min(0.01f)] private float settledScale = 1f;

        private RectTransform effectRect;
        private Coroutine effectQueueRoutine;
        private int pendingEffects;

        public static LevelUpEffectUI Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (effectImage != null)
                effectRect = effectImage.rectTransform;

            SetHidden();
        }

        private void Start()
        {
            if (playerProgression == null)
                playerProgression = FindFirstObjectByType<PlayerProgression>();

            if (playerProgression == null)
            {
                Debug.LogError("LevelUpEffectUI needs a PlayerProgression in the scene.", this);
                enabled = false;
                return;
            }

            playerProgression.LevelChanged += QueueLevelUpEffect;
        }

        private void QueueLevelUpEffect(int newLevel)
        {
            pendingEffects++;
            if (effectQueueRoutine == null)
                effectQueueRoutine = StartCoroutine(ProcessEffectQueue());
        }

        private IEnumerator ProcessEffectQueue()
        {
            while (pendingEffects > 0)
            {
                while (DialogueManager.Instance != null
                    && DialogueManager.Instance.IsDialogueActive)
                {
                    yield return null;
                }

                // A follow-up dialogue can start from DialogueEnded callbacks in the same frame.
                yield return null;
                if (DialogueManager.Instance != null
                    && DialogueManager.Instance.IsDialogueActive)
                {
                    continue;
                }

                pendingEffects--;
                yield return PlayEffect();
            }

            effectQueueRoutine = null;
        }

        private IEnumerator PlayEffect()
        {
            if (effectGroup == null || effectRect == null)
                yield break;

            effectGroup.alpha = 0f;
            effectRect.localScale = Vector3.one * startScale;

            float elapsed = 0f;
            while (elapsed < flashInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / flashInDuration);
                effectGroup.alpha = t;
                effectRect.localScale = Vector3.one * Mathf.Lerp(startScale, settledScale, t);
                yield return null;
            }

            effectGroup.alpha = 1f;
            effectRect.localScale = Vector3.one * settledScale;

            elapsed = 0f;
            while (elapsed < holdDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                effectGroup.alpha = 1f - t;
                yield return null;
            }

            SetHidden();
        }

        private void SetHidden()
        {
            if (effectGroup != null)
            {
                effectGroup.alpha = 0f;
                effectGroup.interactable = false;
                effectGroup.blocksRaycasts = false;
            }

            if (effectRect != null)
                effectRect.localScale = Vector3.one * settledScale;
        }

        private void OnDestroy()
        {
            if (playerProgression != null)
                playerProgression.LevelChanged -= QueueLevelUpEffect;

            if (Instance == this)
                Instance = null;
        }
    }
}
