using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string characterSelectionSceneName = "ChooseCharactor";
        [SerializeField] private SettingsMenuController settingsMenu;

        // Connect this method to the Game Start button's On Click event.
        public void StartGame()
        {
            if (!Application.CanStreamedLevelBeLoaded(characterSelectionSceneName))
            {
                Debug.LogError(
                    $"Scene '{characterSelectionSceneName}' is not included in Build Settings.",
                    this);
                return;
            }

            Time.timeScale = 1f;
            UIFeedbackAudio.Play(UIFeedbackCue.Confirm);
            SceneManager.LoadScene(characterSelectionSceneName);
        }

        // Connect this method to the Settings button's On Click event.
        public void OpenSettings()
        {
            if (settingsMenu == null)
            {
                Debug.LogError("MainMenuController needs a SettingsMenuController reference.", this);
                return;
            }

            settingsMenu.Open();
        }

        public void ReplayOpening()
        {
            UIFeedbackAudio.Play(UIFeedbackCue.Confirm);
            Opening.OpeningCutsceneManager.ReplayFromTitle();
        }

        // Connect this method to the Quit button's On Click event.
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
