using EasternFantasy.Saving;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    public enum UIFeedbackCue { Click, Confirm, SoulstoneActivation, SkillLearn, Advancement, QuestComplete, QuestAccept }

    // Persistent channels keep menu/notification sounds alive when their UI closes or scenes change.
    [DefaultExecutionOrder(-10000)]
    public sealed class UIFeedbackAudio : MonoBehaviour
    {
        private static UIFeedbackAudio instance;
        private static readonly string[] ResourcesNames = {
            "UIClick", "UIConfirm", "SoulstoneActivation", "SkillLearn", "Advancement", "QuestComplete", "QuestAccept"
        };
        private readonly AudioSource[] channels = new AudioSource[7];
        private Selectable[] selectables = new Selectable[64];
        private int lastClickFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (instance != null) return;
            var root = new GameObject("UI Feedback Audio");
            instance = root.AddComponent<UIFeedbackAudio>();
            DontDestroyOnLoad(root);
        }

        private void Awake() => instance = this;

        private void Update()
        {
            // Includes buttons created at runtime, before EventSystem processes this frame's input.
            int count = Selectable.allSelectableCount;
            if (selectables.Length < count) selectables = new Selectable[count * 2];
            count = Selectable.AllSelectablesNoAlloc(selectables);
            for (int i = 0; i < count; i++)
                if (selectables[i] is Button button && button.GetComponent<UIButtonClickAudio>() == null)
                    button.gameObject.AddComponent<UIButtonClickAudio>();
            System.Array.Clear(selectables, 0, count);
        }

        public static void Play(UIFeedbackCue cue)
        {
            if (!Application.isPlaying || SaveGameService.IsRestoring) return;
            Install();
            bool click = cue == UIFeedbackCue.Click || cue == UIFeedbackCue.Confirm;
            // A UI action can also be invoked by a custom slot and a Button on the same object.
            if (click && instance.lastClickFrame == Time.frameCount) return;
            int index = (int)cue;
            var source = instance.channels[index];
            if (source == null)
            {
                var clip = Resources.Load<AudioClip>("Audio/" + ResourcesNames[index]);
                if (clip == null) return;
                var child = new GameObject(ResourcesNames[index] + " Audio");
                child.transform.SetParent(instance.transform, false);
                source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.ignoreListenerPause = click;
                source.volume = cue == UIFeedbackCue.Click ? .25f : .3f;
                source.clip = clip;
                instance.channels[index] = source;
            }
            if (click) instance.lastClickFrame = Time.frameCount;
            source.Play();
        }

        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
