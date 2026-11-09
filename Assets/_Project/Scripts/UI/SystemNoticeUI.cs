using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class SystemNoticeUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private float holdSeconds = .95f;
        [SerializeField, Range(0f, 1f)] private float visibleOpacity = .7f;
        private static SystemNoticeUI instance;
        private Sequence sequence;
        private Object messageOwner;
        private Vector2 restingPosition;
        private Vector3 restingScale;
        private bool prepared;
        private AudioSource restrictionAudio;
        private AudioClip restrictionSound;
        public bool IsVisible => panel != null && panel.gameObject.activeSelf;
        public string CurrentMessage => messageText != null ? messageText.text : string.Empty;

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            Prepare();
            HideInstant();
            SceneManager.activeSceneChanged += SceneChanged;
        }
        public static void Show(string message, Object owner = null)
        {
            if (instance == null) instance = FindFirstObjectByType<SystemNoticeUI>();
            if (instance != null) instance.Present(message, owner);
        }
        public static void ShowFailure(string message, Object owner = null)
        {
            if (instance == null) instance = FindFirstObjectByType<SystemNoticeUI>();
            if (instance != null) instance.Present(message, owner, true);
        }
        public static void Hide(Object owner)
        {
            if (instance != null && instance.messageOwner == owner) instance.HideInstant();
        }
        private void Prepare()
        {
            if (prepared || panel == null) return;
            prepared = true;
            var rect = (RectTransform)panel.transform;
            restingPosition = rect.anchoredPosition;
            restingScale = rect.localScale;
        }
        public void Present(string message, Object owner = null, bool restriction = false)
        {
            if (panel == null || messageText == null || string.IsNullOrWhiteSpace(message)) return;
            Prepare();
            sequence?.Kill();
            messageOwner = owner;
            messageText.text = message;
            var rect = (RectTransform)panel.transform;
            rect.anchoredPosition = restingPosition + Vector2.down * 8;
            rect.localScale = restingScale * .97f;
            panel.alpha = 0;
            panel.interactable = false;
            panel.blocksRaycasts = false;
            panel.gameObject.SetActive(true);
            if (restriction) PlayRestrictionSound();
            sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(panel.DOFade(visibleOpacity, .14f).SetEase(Ease.OutQuad));
            sequence.Join(rect.DOAnchorPos(restingPosition, .18f).SetEase(Ease.OutCubic));
            sequence.Join(rect.DOScale(restingScale, .18f).SetEase(Ease.OutCubic));
            sequence.AppendInterval(Mathf.Max(.1f, holdSeconds));
            sequence.Append(panel.DOFade(0, .28f).SetEase(Ease.InQuad));
            sequence.Join(rect.DOAnchorPos(restingPosition + Vector2.up * 6, .28f));
            sequence.OnComplete(() => { sequence = null; HideInstant(); });
        }
        public void HideInstant()
        {
            if (restrictionAudio != null) restrictionAudio.Stop();
            sequence?.Kill();
            sequence = null;
            messageOwner = null;
            if (panel == null) return;
            panel.alpha = 0;
            panel.gameObject.SetActive(false);
            if (!prepared) return;
            var rect = (RectTransform)panel.transform;
            rect.anchoredPosition = restingPosition;
            rect.localScale = restingScale;
        }
        private void PlayRestrictionSound()
        {
            if (restrictionSound == null) restrictionSound = Resources.Load<AudioClip>("Audio/UpgradeInsufficient");
            if (restrictionSound == null) return;
            if (restrictionAudio == null)
            {
                restrictionAudio = gameObject.AddComponent<AudioSource>();
                restrictionAudio.playOnAwake = false;
                restrictionAudio.loop = false;
                restrictionAudio.spatialBlend = 0f;
                restrictionAudio.ignoreListenerPause = true;
                restrictionAudio.volume = .4f;
                restrictionAudio.clip = restrictionSound;
            }
            // Repeated attempts share one voice instead of stacking loud one-shots.
            if (!restrictionAudio.isPlaying) restrictionAudio.Play();
        }
        private void SceneChanged(Scene before, Scene next) => HideInstant();
        private void OnDisable() => HideInstant();
        private void OnDestroy()
        {
            HideInstant();
            SceneManager.activeSceneChanged -= SceneChanged;
            if (instance == this) instance = null;
        }
    }
}
