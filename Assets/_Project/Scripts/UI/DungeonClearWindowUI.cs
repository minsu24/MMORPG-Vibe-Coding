using System.Collections.Generic;
using System.Linq;
using EasternFantasy.Dungeon;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using EasternFantasy.Saving;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    public sealed class DungeonClearWindowUI : MonoBehaviour
    {
        public GameObject windowRoot;
        public TMP_Text title, message;
        public Transform rewardContent;
        public GameObject rewardRowTemplate;
        public Button confirmButton, closeButton;
        public static DungeonClearWindowUI Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.windowRoot.activeSelf;
        private PendingDungeonReward current;
        private PlayerInventory inventory;
        private PlayerInputReader input;
        private string dismissedRun;
        private readonly List<GameObject> rows = new List<GameObject>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Instance = null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var prefab = Resources.Load<GameObject>("DungeonClearWindow");
            if (Instance == null && prefab != null) Instantiate(prefab);
        }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            windowRoot.SetActive(false); rewardRowTemplate.SetActive(false);
            confirmButton.onClick.AddListener(Confirm);
            closeButton.onClick.AddListener(Dismiss);
            SceneManager.activeSceneChanged += SceneChanged;
        }
        private void SceneChanged(Scene previous, Scene next)
        {
            Dismiss(); dismissedRun = null;
        }
        private void Update()
        {
            if (IsOpen)
            {
                if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) Dismiss();
                return;
            }
            if (SaveGameService.IsRestoring || GameplayHudVisibility.IsMenuScene || PortalSceneTransition.IsTransitioning || SceneLoadingScreen.IsVisible) return;
            var pending = DungeonRewardProgress.Pending.FirstOrDefault(value => value.returnScene == SceneManager.GetActiveScene().name && value.runId != dismissedRun);
            if (pending == null) return;
            var actor = FindFirstObjectByType<PlayerInventory>();
            if (actor == null || actor.GetComponent<PlayerEntity>().IsDead) return;
            Show(pending, actor);
        }
        public void Show(PendingDungeonReward reward, PlayerInventory actor)
        {
            if (reward == null || actor == null) return;
            if (inventory != null) inventory.InventoryChanged -= InventoryChanged;
            inventory = actor; inventory.InventoryChanged += InventoryChanged;
            current = reward;
            foreach (var row in rows) { row.SetActive(false); Destroy(row); }
            rows.Clear();
            title.text = reward.title;
            foreach (var entry in reward.items)
            {
                var item = SaveAssetCatalog.Resolve<ItemDefinition>(entry.item);
                var row = Instantiate(rewardRowTemplate, rewardContent); row.SetActive(true);
                var image = row.GetComponentInChildren<Image>(); image.sprite = item != null ? item.Icon : null; image.enabled = image.sprite != null;
                row.GetComponentInChildren<TMP_Text>().text = (item != null ? item.DisplayName : "아이템") + " ×" + entry.quantity;
                rows.Add(row);
            }
            input = actor.GetComponent<PlayerInputReader>();
            input?.SetInputBlocked(this, true);
            actor.GetComponent<PlayerMovement2D>()?.ClearInput();
            windowRoot.SetActive(true);
            GameTimeController.SetPaused(this, true);
            TryReceive();
            // A selected button uses its selected sprite even before the pointer enters it.
            EventSystem.current?.SetSelectedGameObject(null);
        }
        private void InventoryChanged() { dismissedRun = null; }
        private bool TryReceive()
        {
            if (current == null) return true;
            if (!DungeonRewardProgress.TryClaim(current, inventory))
            {
                message.text = "인벤토리가 가득 찼습니다. 창을 닫고 공간을 비운 뒤 보상을 받아 주세요.";
                confirmButton.GetComponentInChildren<TMP_Text>().text = "보상 받기";
                return false;
            }
            current = null;
            message.text = "클리어 보상이 인벤토리에 지급되었습니다.";
            confirmButton.GetComponentInChildren<TMP_Text>().text = "확인";
            SaveGameService.SaveNow(out _);
            return true;
        }
        public void Confirm() { if (TryReceive()) Dismiss(); }
        public void Dismiss()
        {
            if (current != null) dismissedRun = current.runId;
            current = null;
            windowRoot.SetActive(false);
            input?.SetInputBlocked(this, false); input = null;
            GameTimeController.SetPaused(this, false);
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null
                && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(windowRoot.transform))
                EventSystem.current.SetSelectedGameObject(null);
        }
        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= SceneChanged;
            if (inventory != null) inventory.InventoryChanged -= InventoryChanged;
            input?.SetInputBlocked(this, false);
            GameTimeController.SetPaused(this, false);
            if (Instance == this) Instance = null;
        }
    }
}
