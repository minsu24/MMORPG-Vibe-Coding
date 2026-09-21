using EasternFantasy.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class SettingsMenuController : MonoBehaviour
    {
        private const string MasterVolumeKey = "Settings.MasterVolume";
        private const string FullscreenKey = "Settings.Fullscreen";

        [Header("Panel shown while settings are open")]
        [SerializeField] private GameObject settingsRoot;

        [Header("Optional setting controls")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Optional - found automatically in gameplay scenes")]
        [SerializeField] private PlayerInputReader playerInput;

        private float previousTimeScale = 1f;
        private bool playerInputWasEnabled;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            float savedVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            bool savedFullscreen = PlayerPrefs.GetInt(
                FullscreenKey,
                Screen.fullScreen ? 1 : 0) == 1;

            AudioListener.volume = savedVolume;
            if (masterVolumeSlider != null)
                masterVolumeSlider.SetValueWithoutNotify(savedVolume);
            if (fullscreenToggle != null)
                fullscreenToggle.SetIsOnWithoutNotify(savedFullscreen);

            if (settingsRoot != null)
                settingsRoot.SetActive(false);
        }

        private void Update()
        {
            if (IsOpen && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Close();
        }

        // Connect this to both the main-menu Settings button and an in-game gear button.
        public void Open()
        {
            if (IsOpen)
                return;

            if (settingsRoot == null)
            {
                Debug.LogError("SettingsMenuController needs a Settings Root.", this);
                return;
            }

            previousTimeScale = Time.timeScale;
            DisablePlayerInput();
            Time.timeScale = 0f;

            IsOpen = true;
            settingsRoot.SetActive(true);
        }

        // Connect this to the settings window's Close or Back button.
        public void Close()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            settingsRoot.SetActive(false);
            Time.timeScale = previousTimeScale;
            RestorePlayerInput();
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        public void SetMasterVolume(float value)
        {
            float volume = Mathf.Clamp01(value);
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat(MasterVolumeKey, volume);
            PlayerPrefs.Save();
        }

        public void SetFullscreen(bool fullscreen)
        {
            Screen.fullScreen = fullscreen;
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void DisablePlayerInput()
        {
            if (playerInput == null)
                playerInput = FindFirstObjectByType<PlayerInputReader>();

            if (playerInput == null)
                return;

            playerInputWasEnabled = playerInput.enabled;
            playerInput.enabled = false;
        }

        private void RestorePlayerInput()
        {
            if (playerInput != null)
                playerInput.enabled = playerInputWasEnabled;
        }

        private void OnDisable()
        {
            if (IsOpen)
                Close();
        }
    }
}
