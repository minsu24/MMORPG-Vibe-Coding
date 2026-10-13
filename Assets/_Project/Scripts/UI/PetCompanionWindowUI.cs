using System.Collections.Generic;
using EasternFantasy.CharacterSelection;
using EasternFantasy.Dialogue;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class PetCompanionWindowUI : MonoBehaviour
    {
        private static readonly HashSet<CharacterClassId> tutorialCompletedThisSession = new HashSet<CharacterClassId>();
        [SerializeField] private DialogueSequence tutorialDialogue;
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Image goblinPreview;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private float idleFrameRate = 5f;
        [SerializeField] private Button closeButton, confirmButton, replayButton;
        [SerializeField] private Button[] poseButtons;
        [SerializeField] private Image[] poseFrames;
        [SerializeField] private Sprite selectedPoseFrame, normalPoseFrame;
        [SerializeField] private TMP_Text soulCountText, hintText;
        [SerializeField] private ItemDefinition goblinSoul;

        private DialogueManager boundDialogue;
        private PlayerInventory inventory;
        private bool dialogueStartedInBrokenSeal;
        private bool animatePreview = true;
        private float previewTime;
        private readonly List<Canvas> hiddenCanvases = new List<Canvas>();

        private static PetCompanionWindowUI instance;
        public static PetCompanionWindowUI Instance
        {
            get
            {
                // Editor recompilation resets statics while persistent UI can survive.
                if (instance == null) instance = FindFirstObjectByType<PetCompanionWindowUI>();
                return instance;
            }
        }
        public bool IsUnlocked => tutorialCompletedThisSession.Contains(CharacterSelectionState.SelectedClass);
        public bool IsOpen => windowRoot != null && windowRoot.activeSelf;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            // Also runs when Enter Play Mode has domain reload disabled.
            tutorialCompletedThisSession.Clear();
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var prefab = Resources.Load<GameObject>("PetCompanionWindow");
            if (prefab != null) Instantiate(prefab);
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            windowRoot.SetActive(false);
            closeButton.onClick.AddListener(Close);
            confirmButton.onClick.AddListener(Close);
            replayButton.onClick.AddListener(ReplayIdle);
            for (int i = 0; i < poseButtons.Length; i++)
            {
                int pose = i;
                poseButtons[i].onClick.AddListener(() => SelectPose(pose));
            }
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start() => BindDialogue();

        private void BindDialogue()
        {
            if (boundDialogue == DialogueManager.Instance) return;
            UnbindDialogue();
            boundDialogue = DialogueManager.Instance;
            if (boundDialogue == null) return;
            boundDialogue.DialogueStarted += OnDialogueStarted;
            boundDialogue.DialogueCompleted += OnDialogueCompleted;
        }

        private void OnDialogueStarted()
        {
            dialogueStartedInBrokenSeal = SceneManager.GetActiveScene().name == "BrokenSeal";
            Close();
        }

        private void OnDialogueCompleted(DialogueSequence completed)
        {
            if (!dialogueStartedInBrokenSeal || completed != tutorialDialogue
                || completed == null || SceneManager.GetActiveScene().name != "BrokenSeal") return;
            dialogueStartedInBrokenSeal = false;
            tutorialCompletedThisSession.Add(CharacterSelectionState.SelectedClass);
            // DialogueCompleted is raised after the dialogue has released input and pause.
            Open();
        }

        private void Update()
        {
            BindDialogue();
            // Every entry point, including an accidentally enabled root, respects the gate.
            if (IsOpen && !IsUnlocked) Close();
            if (IsOpen)
            {
                Fit();
                if (animatePreview && idleFrames != null && idleFrames.Length > 0)
                {
                    previewTime += Time.unscaledDeltaTime;
                    goblinPreview.sprite = idleFrames[(int)(previewTime * idleFrameRate) % idleFrames.Length];
                }
            }
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (IsOpen && keyboard.escapeKey.wasPressedThisFrame) { Close(); return; }
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null && (selected.GetComponent<TMP_InputField>() != null
                || selected.GetComponent<InputField>() != null)) return;
            if (keyboard.pKey.wasPressedThisFrame) Toggle();
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public bool Open()
        {
            if (!IsUnlocked || windowRoot == null) return false;
            if (IsOpen) return true;
            string scene = SceneManager.GetActiveScene().name;
            if (scene == "MainScreen" || scene == "ChooseCharactor") return false;
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return false;
            if (EquipmentUpgradeWindowUI.Instance != null && EquipmentUpgradeWindowUI.Instance.IsOpen) return false;
            var player = FindFirstObjectByType<PlayerEntity>();
            if (player == null || player.IsDead) return false;
            InventoryWindowUI.Instance?.SetOpen(false);
            EquipmentWindowUI.Instance?.CloseFromInventory();
            PlayerStatsWindowUI.Instance?.SetOpen(false);
            SkillWindowUI.Instance?.SetOpen(false);
            ShopWindowUI.Instance?.Close();
            ScrollCodexWindowUI.Instance?.Close();
            HideGameplayCanvases();
            BindInventory(player.GetComponent<PlayerInventory>());
            windowRoot.SetActive(true);
            Canvas.ForceUpdateCanvases();
            Fit();
            ReplayIdle();
            GameTimeController.SetPaused(this, true);
            RefreshSoulCount();
            return true;
        }

        public void Close()
        {
            if (windowRoot != null) windowRoot.SetActive(false);
            foreach (Canvas canvas in hiddenCanvases)
                if (canvas != null) canvas.enabled = true;
            hiddenCanvases.Clear();
            GameTimeController.SetPaused(this, false);
            BindInventory(null);
        }

        private void HideGameplayCanvases()
        {
            // Keep HUD and other canvases from covering this modal window.
            // Restore exactly the canvases that were enabled when it opened.
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.enabled || canvas.transform.IsChildOf(transform)) continue;
                hiddenCanvases.Add(canvas);
                canvas.enabled = false;
            }
        }

        public void ShowLockedCompanion()
        {
            if (hintText != null) hintText.text = "아직 계약하지 않은 혼령입니다.";
        }

        public void ReplayIdle()
        {
            previewTime = 0f;
            animatePreview = true;
            if (hintText != null) hintText.text = "P 키로 열기 · ESC 키로 닫기";
            HighlightPose(0);
            if (idleFrames.Length > 0) goblinPreview.sprite = idleFrames[0];
        }

        private void SelectPose(int index)
        {
            if (idleFrames == null || idleFrames.Length == 0) return;
            animatePreview = false;
            goblinPreview.sprite = idleFrames[Mathf.Min(index * idleFrames.Length / poseButtons.Length, idleFrames.Length - 1)];
            HighlightPose(index);
        }

        private void HighlightPose(int index)
        {
            for (int i = 0; i < poseFrames.Length; i++)
                poseFrames[i].sprite = i == index ? selectedPoseFrame : normalPoseFrame;
        }

        private void BindInventory(PlayerInventory current)
        {
            if (inventory == current) return;
            if (inventory != null) inventory.InventoryChanged -= RefreshSoulCount;
            inventory = current;
            if (inventory != null) inventory.InventoryChanged += RefreshSoulCount;
        }

        private void RefreshSoulCount()
        {
            if (soulCountText != null)
                soulCountText.text = (inventory != null && goblinSoul != null ? inventory.GetQuantity(goblinSoul) : 0) + " 개";
        }

        private void Fit()
        {
            Rect available = ((RectTransform)windowRoot.transform).rect;
            float scale = Mathf.Min((available.width - 48f) / 1448f, (available.height - 36f) / 1000f);
            panel.localScale = Vector3.one * Mathf.Max(.05f, scale);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Close();
            dialogueStartedInBrokenSeal = false;
            BindDialogue();
        }

        private void UnbindDialogue()
        {
            if (boundDialogue != null)
            {
                boundDialogue.DialogueStarted -= OnDialogueStarted;
                boundDialogue.DialogueCompleted -= OnDialogueCompleted;
            }
            boundDialogue = null;
        }

        private void OnDisable() { Close(); UnbindDialogue(); }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnbindDialogue();
            BindInventory(null);
            GameTimeController.SetPaused(this, false);
            if (instance == this) instance = null;
        }
    }
}
