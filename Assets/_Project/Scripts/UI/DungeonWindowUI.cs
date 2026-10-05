using System.Collections.Generic;
using EasternFantasy.Dungeon;
using EasternFantasy.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class DungeonWindowUI : MonoBehaviour
    {
        public DungeonDefinition[] dungeons;
        public GameObject windowRoot;
        public Image preview, dungeonEmblem;
        public TMP_Text dungeonName, description, levelText, timeText, monsterText, rewardText, message;
        public Image[] rewardIcons;
        public TMP_Text[] rewardAmounts;
        public Button enterButton, closeButton;
        public ScrollRect list;
        public DungeonListEntryUI entryTemplate;
        private readonly List<DungeonListEntryUI> entries = new List<DungeonListEntryUI>();
        private readonly List<DungeonDefinition> boundDungeons = new List<DungeonDefinition>();
        private PlayerMovement2D player;
        private DungeonPortal portal;
        private bool loading;
        public static DungeonWindowUI Instance { get; private set; }
        public bool IsOpen => windowRoot != null && windowRoot.activeSelf;
        public DungeonDefinition Selected { get; private set; }
        private void Awake()
        {
            Initialize();
            windowRoot.SetActive(false);
            entryTemplate.gameObject.SetActive(false);
        }
        private void Initialize()
        {
            Instance = this;

        }
        private void Update()
        {
            if (!IsOpen) return;
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true || player == null || portal == null
                || (player.GetComponent<PlayerEntity>() is PlayerEntity entity && entity.IsDead)) Close();
        }
        public void Open(DungeonPortal source, PlayerMovement2D actor)
        {
            Initialize();
            if (loading || source == null || actor == null) return;
            if (actor.GetComponent<PlayerEntity>() is PlayerEntity entity && entity.IsDead) return;
            portal = source;
            player = actor;
            InventoryWindowUI.Instance?.SetOpen(false);
            EquipmentWindowUI.Instance?.SetOpen(false);
            SkillWindowUI.Instance?.SetOpen(false);
            PlayerStatsWindowUI.Instance?.SetOpen(false);
            ShopWindowUI.Instance?.Close();
            player.ClearInput();
            windowRoot.SetActive(true);
            GameTimeController.SetPaused(this, true);
            RebuildList();
            Select(boundDungeons.Contains(Selected) ? Selected : boundDungeons.Count > 0 ? boundDungeons[0] : null);
            list.verticalNormalizedPosition = 1f;
        }
        private void RebuildList()
        {
            foreach (var entry in entries) { entry.gameObject.SetActive(false); Destroy(entry.gameObject); }
            entries.Clear();
            boundDungeons.Clear();
            if (dungeons == null) return;
            foreach (var dungeon in dungeons)
            {
                if (dungeon == null) continue;
                var entry = Instantiate(entryTemplate, list.content);
                entry.name = "Dungeon - " + dungeon.displayName;
                entry.gameObject.SetActive(true);
                entry.Bind(this, dungeon);
                entries.Add(entry);
                boundDungeons.Add(dungeon);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(list.content);
        }
        public bool CanEnter(DungeonDefinition dungeon)
        {
            if (dungeon == null || !dungeon.available || string.IsNullOrWhiteSpace(dungeon.sceneName)
                || !Application.CanStreamedLevelBeLoaded(dungeon.sceneName)) return false;
            var progression = player != null ? player.GetComponent<PlayerProgression>() : null;
            return (progression != null ? progression.CurrentLevel : 1) >= dungeon.minimumLevel;
        }
        public void Select(DungeonDefinition dungeon)
        {
            if (dungeon != null && !boundDungeons.Contains(dungeon)) return;
            Selected = dungeon;
            preview.sprite = dungeon != null ? dungeon.preview : null;
            preview.enabled = preview.sprite != null;
            if (dungeonEmblem != null) { dungeonEmblem.sprite = preview.sprite; dungeonEmblem.enabled = preview.enabled; }
            var fitter = preview.GetComponent<AspectRatioFitter>();
            if (fitter != null && preview.sprite != null) fitter.aspectRatio = preview.sprite.rect.width / preview.sprite.rect.height;
            dungeonName.text = dungeon != null ? dungeon.displayName : "등록된 던전 없음";
            description.text = dungeon != null ? dungeon.description : "던전 데이터를 등록해 주세요.";
            levelText.text = dungeon != null ? $"입장 레벨  {dungeon.minimumLevel}" : "입장 레벨  —";
            timeText.text = dungeon != null && dungeon.timeLimitSeconds > 0
                ? $"제한시간  {dungeon.timeLimitSeconds / 60:00}:{dungeon.timeLimitSeconds % 60:00}" : "제한시간  미설정";
            monsterText.text = dungeon != null && dungeon.monsterCount > 0 ? $"몬스터  {dungeon.monsterCount}마리" : "몬스터  미설정";
            var names = new List<string>();
            int iconIndex = 0;
            if (dungeon != null && dungeon.clearRewards != null)
                foreach (var reward in dungeon.clearRewards)
                {
                    if (reward == null || reward.item == null) continue;
                    names.Add($"{reward.item.DisplayName} ×{reward.quantity}");
                    if (iconIndex >= rewardIcons.Length) continue;
                    rewardIcons[iconIndex].sprite = reward.item.Icon;
                    rewardIcons[iconIndex].enabled = reward.item.Icon != null;
                    rewardAmounts[iconIndex].text = $"×{reward.quantity}";
                    iconIndex++;
                }
            for (int i = iconIndex; i < rewardIcons.Length; i++)
            { rewardIcons[i].enabled = false; rewardAmounts[i].text = string.Empty; }
            rewardText.text = names.Count > 0 ? "클리어 보상  " + string.Join(" · ", names) : "클리어 보상  미설정";
            for (int i = 0; i < entries.Count; i++) entries[i].SetSelected(boundDungeons[i] == dungeon);
            enterButton.interactable = !loading && CanEnter(dungeon);
            message.text = dungeon != null && !CanEnter(dungeon) ? "입장 조건 또는 연결된 씬을 확인해 주세요." : string.Empty;
        }
        public void EnterSelected()
        {
            if (!IsOpen || loading || player == null || !CanEnter(Selected)) return;
            if (player.GetComponent<PlayerEntity>() is PlayerEntity entity && entity.IsDead) return;
            string destination = Selected.sceneName;
            if (!player.TryGetComponent<PlayerLocationSetter>(out _)) player.gameObject.AddComponent<PlayerLocationSetter>();
            if (!string.IsNullOrWhiteSpace(Selected.spawnPointName)) MapTransferData.SetTarget(Selected.spawnPointName);
            else MapTransferData.Clear();
            loading = true;
            enterButton.interactable = false;
            Close();
            SceneManager.LoadSceneAsync(destination);
        }
        public void Close()
        {
            if (windowRoot != null) windowRoot.SetActive(false);
            if (player != null) player.ClearInput();
            GameTimeController.SetPaused(this, false);
        }
        private void OnDisable() => Close();
        private void OnDestroy() { GameTimeController.SetPaused(this, false); if (Instance == this) Instance = null; }
    }
}
