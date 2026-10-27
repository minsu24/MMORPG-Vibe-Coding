using EasternFantasy.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using EasternFantasy.Saving;

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

        private bool playerInputWasEnabled;
        private Button saveButton;
        private TMP_Text saveStatus;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            BuildSaveControls();
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

            DisablePlayerInput();
            GameTimeController.SetPaused(this, true);

            IsOpen = true;
            if (saveStatus != null)
                saveStatus.text = SaveGameService.IsDemoSession ? "시연 모드 · 기존 저장 기록 유지" : "10초마다 자동 저장됩니다.";
            settingsRoot.SetActive(true);
        }

        private void BuildSaveControls()
        {
            if (masterVolumeSlider == null) return;
            var parent = masterVolumeSlider.transform.parent;
            var volumeRect = masterVolumeSlider.GetComponent<RectTransform>();
            volumeRect.anchoredPosition = new Vector2(volumeRect.anchoredPosition.x, -78f);
            var label = parent.Find("VolumeLabel") as RectTransform;
            if (label != null) label.anchoredPosition = new Vector2(label.anchoredPosition.x, -78f);
            var hint = parent.Find("SettingsHint");
            saveStatus = hint != null ? hint.GetComponent<TMP_Text>() : null;
            if (saveStatus != null)
            {
                saveStatus.rectTransform.anchoredPosition = new Vector2(0f, -133f);
                saveStatus.fontSize = 20f;
            }
            var close = parent.Find("CloseButton")?.GetComponent<Button>();
            var root = new GameObject("SaveButton", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(290f, 56f);
            rect.anchoredPosition = new Vector2(0f, 92f);
            var image = root.GetComponent<Image>();
            var source = close != null ? close.targetGraphic as Image : null;
            image.sprite = source != null ? source.sprite : null;
            image.type = source != null ? source.type : Image.Type.Simple;
            image.color = source != null ? source.color : new Color(.2f, .23f, .19f, 1f);
            saveButton = root.GetComponent<Button>(); saveButton.targetGraphic = image;
            if (close != null) saveButton.colors = close.colors;
            saveButton.onClick.AddListener(Save);
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(root.transform, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            var sourceText = close != null ? close.GetComponentInChildren<TMP_Text>(true) : null;
            if (sourceText != null) { text.font = sourceText.font; text.color = sourceText.color; }
            text.text = "저장"; text.fontSize = 28f; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        }

        public void Save()
        {
            SaveGameService.SaveNow(out string message);
            if (saveStatus != null) saveStatus.text = message;
        }

        // Connect this to the settings window's Close or Back button.
        public void Close()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            settingsRoot.SetActive(false);
            GameTimeController.SetPaused(this, false);
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
