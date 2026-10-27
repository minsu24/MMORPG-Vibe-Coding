using EasternFantasy.Dialogue;
using EasternFantasy.Inventory;
using EasternFantasy.Player;
using EasternFantasy.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Quest
{
    public sealed partial class ToadHuntQuestController : MonoBehaviour
    {
        public static ToadHuntQuestController Instance { get; private set; }
        public ToadHuntDefinition Definition { get; private set; }
        public QuestRewardRelic Relic { get; private set; }
        public bool NecklaceClaimed => Definition != null && quests != null
            && quests.FindProgress(Definition.quest)?.Status == QuestStatus.Completed;
        private QuestManager quests;
        private DialogueManager dialogue;
        private MON_Frog boss;
        private bool defeated, offerPending, inspecting, claiming, explanationPending, equipmentPresentationPending;
        private bool hasDeathPosition;
        private PlayerInventory inspectingInventory;
        private Vector3 deathPosition;
        private float nextRefresh;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= EnsureController;
            SceneManager.sceneLoaded += EnsureController;
        }
        private static void EnsureController(Scene scene, LoadSceneMode mode)
        {
            if (Instance != null || FindFirstObjectByType<PlayerEntity>() == null) return;
            var definition = Resources.Load<ToadHuntDefinition>("ToadHunt");
            if (definition == null) return;
            var root = new GameObject("Toad Hunt Quest Flow");
            var controller = root.AddComponent<ToadHuntQuestController>();
            controller.Definition = definition;
            controller.RefreshBindings();
            controller.RefreshProgress();
        }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private void OnEnable() => SceneManager.sceneLoaded += SceneLoaded;
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            if (quests != null) quests.QuestsChanged -= RefreshProgress;
            if (dialogue != null) dialogue.DialogueCompleted -= DialogueCompleted;
            if (boss != null) boss.Defeated -= BossDefeated;
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            inspecting = false;
            inspectingInventory = null;
            Relic = null;
            RefreshBindings();
            RefreshProgress();
        }
        private void RefreshBindings()
        {
            if (quests != QuestManager.Instance)
            {
                if (quests != null) quests.QuestsChanged -= RefreshProgress;
                quests = QuestManager.Instance;
                if (quests != null) quests.QuestsChanged += RefreshProgress;
            }
            if (dialogue != DialogueManager.Instance)
            {
                if (dialogue != null) dialogue.DialogueCompleted -= DialogueCompleted;
                dialogue = DialogueManager.Instance;
                if (dialogue != null) dialogue.DialogueCompleted += DialogueCompleted;
            }
            if (Definition == null || SceneManager.GetActiveScene().name != Definition.bossScene) return;
            var currentBoss = FindFirstObjectByType<MON_Frog>(FindObjectsInactive.Include);
            if (boss != currentBoss)
            {
                if (boss != null) boss.Defeated -= BossDefeated;
                boss = currentBoss;
                if (boss != null) boss.Defeated += BossDefeated;
            }
            if (boss != null && boss.WasDefeatedThisSession)
            {
                defeated = true;
                if (!hasDeathPosition) { deathPosition = boss.transform.position; hasDeathPosition = true; }
            }
        }
        private void Update()
        {
            if (EasternFantasy.Saving.SaveGameService.IsRestoring) return;
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + .25f;
                RefreshBindings();
                RefreshProgress();
            }
            if (dialogue == null || dialogue.IsDialogueActive) return;
            if (inspecting) { inspecting = false; inspectingInventory = null; } // Interrupted monologues grant nothing.
            if (explanationPending)
            {
                if (dialogue.StartDialogue(Definition.accessoryExplanation)) explanationPending = false;
                return;
            }
            if (equipmentPresentationPending && InventoryWindowUI.Instance != null && EquipmentWindowUI.Instance != null
                && InventoryWindowUI.Instance.IsUnlocked && EquipmentWindowUI.Instance.IsUnlocked
                && !InventoryEquipmentTutorialUI.IsRunning && !StatsSkillTutorialUI.IsRunning && !MapTutorialUI.IsRunning)
            {
                EquipmentWindowUI.Instance.SetOpen(true);
                InventoryWindowUI.Instance.SelectCategory(ItemCategory.Accessory);
                EquipmentWindowUI.Instance.Refresh();
                equipmentPresentationPending = !EquipmentWindowUI.Instance.IsOpen;
            }
            if (!offerPending) return;
            foreach (var giver in FindObjectsByType<QuestGiver>(FindObjectsSortMode.None))
                if (giver.Quest == Definition.quest && giver.CanOfferQuest)
                {
                    offerPending = false;
                    giver.OfferQuest();
                    break;
                }
        }
        private void DialogueCompleted(DialogueSequence sequence)
        {
            if (Definition == null) return;
            if (sequence == Definition.prerequisiteCompletion) offerPending = true;
            if (sequence == Definition.accessoryExplanation) equipmentPresentationPending = true;
            if (sequence == Definition.discoveryMonologue && inspecting)
            {
                inspecting = false;
                TryClaimReward();
            }
        }
        public void BossDefeated(EnemyController enemy)
        {
            if (Definition == null || enemy == null || enemy.gameObject.scene.name != Definition.bossScene) return;
            defeated = true;
            hasDeathPosition = true;
            deathPosition = enemy.transform.position;
            RefreshProgress();
        }
        private void RefreshProgress()
        {
            if (quests == null || Definition == null || !defeated) return;
            var progress = quests.FindProgress(Definition.quest);
            if (progress == null) return;
            if (progress.Status == QuestStatus.Active)
            {
                quests.AddProgress(Definition.quest, Definition.quest.RequiredAmount);
                return;
            }
            if (progress.Status == QuestStatus.Completed)
            {
                if (Relic != null)
                {
                    if (Application.isPlaying) Destroy(Relic.gameObject);
                    else DestroyImmediate(Relic.gameObject);
                }
                Relic = null;
                return;
            }
            if (Relic != null || SceneManager.GetActiveScene().name != Definition.bossScene
                || Definition.relicPrefab == null) return;
            Vector3 origin = deathPosition;
            var floor = DropPlacement2D.FindSupport(SceneManager.GetActiveScene(), origin);
            if (DropPlacement2D.TryFindLanding(floor, origin, origin.x, .4f, out Vector3 landing)) origin = landing;
            Relic = Instantiate(Definition.relicPrefab, origin + Vector3.up * .38f, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(Relic.gameObject, SceneManager.GetActiveScene());
            Relic.Initialize(this);
        }
        public bool TryInspectRelic()
        {
            if (Definition == null || quests == null || dialogue == null || inspecting || claiming
                || dialogue.IsDialogueActive || Relic == null
                || quests.FindProgress(Definition.quest)?.Status != QuestStatus.ReadyToTurnIn) return false;
            var player = FindFirstObjectByType<PlayerEntity>();
            if (player == null || player.IsDead) return false;
            var input = player.GetComponent<PlayerInputReader>();
            if (input != null && !input.CanControlGameplay) return false;
            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null || !inventory.CanAdd(Definition.necklace))
            {
                SystemNoticeUI.Show("장신구 인벤토리에 빈 칸을 마련한 뒤 다시 조사하세요.", this);
                return false;
            }
            inspecting = dialogue.StartDialogue(Definition.discoveryMonologue);
            inspectingInventory = inspecting ? inventory : null;
            return inspecting;
        }
        private bool TryClaimReward()
        {
            if (claiming || Definition == null || quests == null
                || quests.FindProgress(Definition.quest)?.Status != QuestStatus.ReadyToTurnIn) return false;
            var inventory = inspectingInventory;
            if (inventory == null || !inventory.CanAdd(Definition.necklace)) return false;
            claiming = true;
            try
            {
                if (!inventory.AddItem(Definition.necklace)) return false;
                if (!quests.CompleteQuest(Definition.quest))
                {
                    inventory.RemoveItem(Definition.necklace);
                    return false;
                }
                explanationPending = true;
                InventoryWindowUI.Instance?.SelectCategory(ItemCategory.Accessory);
                inspectingInventory = null;
                SystemNoticeUI.Show("두꺼비 방울 목걸이 획득", this);
                RefreshProgress();
                return true;
            }
            finally { claiming = false; }
        }
    }
}
