using EasternFantasy.CharacterSelection;
using EasternFantasy.Dialogue;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class EquipmentWindowUI : MonoBehaviour
    {
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private Image characterImage;
        [SerializeField] private EquipmentSlotUI[] slots;

        private InventoryWindowUI inventoryWindow;
        private PlayerInventory inventory;
        private SpriteRenderer characterRenderer;

        public static EquipmentWindowUI Instance { get; private set; }
        public RectTransform TutorialTarget => windowRoot != null ? windowRoot.transform as RectTransform : null;
        public RectTransform TutorialWeaponTarget { get { foreach (var slot in slots) if (slot != null && slot.Slot == EquipmentSlot.Weapon) return slot.transform as RectTransform; return TutorialTarget; } }
        public bool IsUnlocked => InventoryEquipmentTutorialUI.HasFinishedForCurrentClass;
        public bool IsOpen => windowRoot != null && windowRoot.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (windowRoot != null)
                windowRoot.SetActive(false);
        }

        private void Start()
        {
            inventoryWindow = InventoryWindowUI.Instance;
            SetInventory(FindFirstObjectByType<PlayerInventory>());
        }

        public void SetInventory(PlayerInventory current)
        {
            if (inventory == current && current != null)
                return;
            if (inventory != null)
                inventory.InventoryChanged -= Refresh;
            inventory = current;
            if (inventory != null)
                inventory.InventoryChanged += Refresh;
            if (slots != null)
                foreach (EquipmentSlotUI slot in slots)
                    slot.Initialize(inventory, inventoryWindow != null ? inventoryWindow.Tooltip : null);
            characterRenderer = null;
            Refresh();
        }

        private void Update()
        {
            if (StatsSkillTutorialUI.IsRunning || InventoryEquipmentTutorialUI.IsRunning || DungeonWindowUI.IsTutorialRunning || EquipmentUpgradeWindowUI.IsTutorialRunning || ShopWindowUI.IsTutorialRunning) { if (IsOpen) RefreshCharacter(); return; }
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.jKey.wasPressedThisFrame)
            {
                bool blocked = !IsOpen && DialogueManager.Instance != null
                    && DialogueManager.Instance.IsDialogueActive;
                if (!blocked)
                    SetOpen(!IsOpen);
            }

            if (IsOpen)
                RefreshCharacter();
        }

        public void SetOpen(bool open) { SetOpenInternal(open, false); }
        internal void SetOpenForTutorial(bool open) { SetOpenInternal(open, true); }
        private void SetOpenInternal(bool open, bool forTutorial)
        {
            if (open && !forTutorial && !IsUnlocked) return;
            if (open && EquipmentUpgradeWindowUI.Instance != null && EquipmentUpgradeWindowUI.Instance.IsOpen) return;
            if (windowRoot == null)
                return;
            if (inventoryWindow == null)
                inventoryWindow = InventoryWindowUI.Instance;
            if (inventoryWindow == null)
                return;

            if (open)
            {
                if (forTutorial) inventoryWindow.SetOpenForTutorial(true);
                else inventoryWindow.SetOpen(true);
                SetInventory(inventoryWindow.Inventory);
                inventoryWindow.SetEquipmentLayout(true);
                Refresh();
            }
            else
            {
                inventoryWindow.SetOpen(false);
            }
            windowRoot.SetActive(open);
        }

        public void CloseFromInventory()
        {
            if (windowRoot != null)
                windowRoot.SetActive(false);
            inventoryWindow?.SetEquipmentLayout(false);
        }

        public void Refresh()
        {
            if (slots == null)
                return;
            foreach (EquipmentSlotUI slot in slots)
                if (slot != null)
                    slot.Refresh();
        }

        private void RefreshCharacter()
        {
            if (characterImage == null)
                return;
            if (characterRenderer == null)
            {
                foreach (PlayerClassRuntime player in FindObjectsByType<PlayerClassRuntime>(FindObjectsSortMode.None))
                {
                    if (player.RepresentedClass != CharacterSelectionState.SelectedClass)
                        continue;
                    characterRenderer = player.GetComponent<SpriteRenderer>();
                    if (characterRenderer == null)
                        characterRenderer = player.GetComponentInChildren<SpriteRenderer>();
                    if (characterRenderer != null)
                        break;
                }
            }

            Sprite sprite = characterRenderer != null ? characterRenderer.sprite : null;
            if (sprite == null && PlayerClassRuntime.ActiveDefinition != null)
                sprite = PlayerClassRuntime.ActiveDefinition.Portrait;
            characterImage.sprite = sprite;
            characterImage.enabled = sprite != null;
        }

        private void OnDestroy()
        {
            if (inventory != null)
                inventory.InventoryChanged -= Refresh;
            if (Instance == this)
                Instance = null;
        }
    }
}
