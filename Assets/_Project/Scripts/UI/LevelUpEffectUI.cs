using System.Collections;
using DG.Tweening;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        [SerializeField, Min(0.01f)] private float settleDuration = 0.22f;

        private Sequence effectSequence;
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
            SceneManager.sceneLoaded += HandleSceneLoaded;

            if (effectImage != null)
                effectRect = effectImage.rectTransform;

            SetHidden();
        }

        private void Start()
        {
            BindCurrentProgression();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BindCurrentProgression();
        }

        private void BindCurrentProgression()
        {
            PlayerProgression current = FindCurrentProgression();
            if (playerProgression != null)
                playerProgression.LeveledUp -= QueueLevelUpEffect;
            playerProgression = current;
            if (playerProgression != null)
                playerProgression.LeveledUp += QueueLevelUpEffect;
        }

        private static PlayerProgression FindCurrentProgression()
        {
            foreach (PlayerProgression candidate in
                FindObjectsByType<PlayerProgression>(FindObjectsSortMode.None))
            {
                PlayerClassRuntime playerClass = candidate.GetComponent<PlayerClassRuntime>();
                if (playerClass != null
                    && playerClass.RepresentedClass == CharacterSelectionState.SelectedClass)
                    return candidate;
            }

            return FindFirstObjectByType<PlayerProgression>();
        }

        private void QueueLevelUpEffect(int newLevel)
        {
            pendingEffects++;
            if (isActiveAndEnabled && effectQueueRoutine == null)
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

            effectSequence?.Kill();
            SetHidden();
            effectRect.localScale = Vector3.one * startScale;

            effectSequence = DOTween.Sequence().SetUpdate(true);
            effectSequence.Append(effectGroup.DOFade(1f, Mathf.Max(0.01f, flashInDuration))
                .SetEase(Ease.OutQuad));
            effectSequence.Join(effectRect.DOScale(Vector3.one * settledScale,
                Mathf.Max(0.01f, settleDuration)).SetEase(Ease.OutBack));
            effectSequence.AppendInterval(Mathf.Max(0f, holdDuration));
            effectSequence.Append(effectGroup.DOFade(0f, Mathf.Max(0.01f, fadeOutDuration))
                .SetEase(Ease.InQuad));

            // Keep the queue waiting until this complete animation has finished.
            yield return effectSequence.WaitForCompletion(true);
            effectSequence = null;
            SetHidden();
        }

        private void OnEnable()
        {
            if (pendingEffects > 0 && effectQueueRoutine == null)
                effectQueueRoutine = StartCoroutine(ProcessEffectQueue());
        }

        private void OnDisable()
        {
            if (effectQueueRoutine != null)
                StopCoroutine(effectQueueRoutine);
            effectQueueRoutine = null;
            effectSequence?.Kill();
            effectSequence = null;
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
            effectSequence?.Kill();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (playerProgression != null)
                playerProgression.LeveledUp -= QueueLevelUpEffect;

            if (Instance == this)
                Instance = null;
        }
    }
}
