using EasternFantasy.Dialogue;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace EasternFantasy.UI
{
    public sealed class ScrollCodexWindowUI : MonoBehaviour
    {
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private RectTransform scrollRoot;
        [SerializeField] private ScrollCodexAnimator animator;
        public static ScrollCodexWindowUI Instance { get; private set; }
        public bool IsOpen => windowRoot != null && windowRoot.activeSelf;
        public void Configure(GameObject window, RectTransform scroll, ScrollCodexAnimator animation)
        { windowRoot = window; scrollRoot = scroll; animator = animation; }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            animator.Closed.AddListener(Hide); windowRoot.SetActive(false);
            SceneManager.activeSceneChanged += SceneChanged;
        }
        private void Update()
        {
            if (IsOpen) Fit();
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (IsOpen && keyboard.escapeKey.wasPressedThisFrame) { Close(); return; }
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (selected && (selected.GetComponent<TMPro.TMP_InputField>() || selected.GetComponent<UnityEngine.UI.InputField>())) return;
            if (keyboard.uKey.wasPressedThisFrame) Toggle();
        }
        public void Toggle() { if (IsOpen && animator.IsOpen) Close(); else Open(); }
        public void Open()
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
            if (EquipmentUpgradeWindowUI.Instance != null && EquipmentUpgradeWindowUI.Instance.IsOpen) return;
            string scene = SceneManager.GetActiveScene().name;
            if (scene == "MainScreen" || scene == "ChooseCharactor") return;
            windowRoot.SetActive(true); Canvas.ForceUpdateCanvases(); Fit(); animator.Open();
        }
        public void Close() { if (IsOpen) animator.Close(); }
        private void Hide() => windowRoot.SetActive(false);
        private void Fit()
        {
            var rect = ((RectTransform)windowRoot.transform).rect;
            float scale = Mathf.Min(1.15f, Mathf.Min((rect.width - 32) / 1672, (rect.height - 60) / 941));
            scrollRoot.localScale = Vector3.one * Mathf.Max(.05f, scale);
        }
        private void SceneChanged(Scene previous, Scene next) { animator.SetOpenInstant(false); Hide(); }
        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= SceneChanged;
            if (Instance == this) Instance = null;
        }
    }
}
