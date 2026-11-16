using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(Button))]
    public sealed class UIButtonClickAudio : MonoBehaviour
    {
        [SerializeField] private bool confirmation;
        private Button button;

        public static void ConfigureConfirmation(Button target)
        {
            if (target == null) return;
            var audio = target.GetComponent<UIButtonClickAudio>();
            if (audio == null) audio = target.gameObject.AddComponent<UIButtonClickAudio>();
            audio.confirmation = true;
        }

        private void Awake()
        {
            button = GetComponent<Button>();
            Rebind();
        }

        public void Rebind()
        {
            if (button == null) button = GetComponent<Button>();
            button.onClick.RemoveListener(Play);
            button.onClick.AddListener(Play);
        }

        // Button handles interactability, left clicks, keyboard/gamepad submit and dragging.
        // The callback still completes if another listener closes the window during this click.
        private void Play() => UIFeedbackAudio.Play(confirmation ? UIFeedbackCue.Confirm : UIFeedbackCue.Click);

        private void OnDestroy() { if (button != null) button.onClick.RemoveListener(Play); }
    }
}
