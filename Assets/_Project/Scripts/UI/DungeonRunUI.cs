using EasternFantasy.Dungeon;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    public sealed class DungeonRunUI : MonoBehaviour
    {
        public GameObject promptRoot;
        public TMP_Text timer, timerCaption, exitTimer;
        public Button noButton, yesButton;

        private void Awake()
        {
            noButton.onClick.AddListener(HidePrompt);
            yesButton.onClick.AddListener(() => DungeonRunController.Instance?.ExitToVillage());
        }

        public static string FormatTime(float seconds)
        {
            int wholeSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return $"{wholeSeconds / 60:00}:{wholeSeconds % 60:00}";
        }

        public void SetTimer(float seconds, bool cleared, bool visible)
        {
            timer.transform.parent.gameObject.SetActive(visible);
            timer.text = FormatTime(seconds);
            timerCaption.text = cleared ? "자동 퇴장까지" : "던전 제한시간";
            exitTimer.text = FormatTime(seconds);
        }

        public void ShowPrompt()
        {
            promptRoot.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(noButton.gameObject);
        }

        public void HidePrompt()
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null
                && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(promptRoot.transform))
                EventSystem.current.SetSelectedGameObject(null);
            promptRoot.SetActive(false);
        }

        private void Update()
        {
            if (promptRoot.activeSelf && Keyboard.current?.escapeKey.wasPressedThisFrame == true) HidePrompt();
        }
    }
}
