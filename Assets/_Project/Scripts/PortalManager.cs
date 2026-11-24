using EasternFantasy.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using EasternFantasy.Quest;

[RequireComponent(typeof(Collider2D))]
public sealed class PortalManager : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private string targetPointName;

    [SerializeField] private bool startsLocked;
    [SerializeField] private bool requiresToadNecklace;
    [SerializeField] private QuestDefinition requiredAcceptedQuest;
    [SerializeField] private QuestDefinition requiredCompletedQuest;
    [SerializeField] private QuestDefinition gateDuringQuest;
    [SerializeField] private string questBlockedMessage;
    private bool isLocked;
    private bool NecklaceBlocked => requiresToadNecklace &&
        (EasternFantasy.Quest.ToadHuntQuestController.Instance == null ||
         !EasternFantasy.Quest.ToadHuntQuestController.Instance.NecklaceClaimed);
    private bool QuestBlocked
    {
        get
        {
            var manager = QuestManager.Instance;
            if (requiredAcceptedQuest != null && manager?.FindProgress(requiredAcceptedQuest) == null)
                return true;
            return requiredCompletedQuest != null
                && (gateDuringQuest == null
                    || manager?.FindProgress(gateDuringQuest) is { Status: not QuestStatus.Completed })
                && manager?.FindProgress(requiredCompletedQuest)?.Status != QuestStatus.Completed;
        }
    }
    public bool IsLocked => isLocked || NecklaceBlocked || QuestBlocked;
    public void SetLocked(bool locked) => isLocked = locked;
    private void Awake() => isLocked = startsLocked;

    private PlayerMovement2D playerInPortal;
    private bool isLoadingScene;

    private void Update()
    {
        if (EasternFantasy.UI.SoulstoneWindowUI.IsOpen || EasternFantasy.UI.SoulstoneTutorialUI.IsRunning) return;
        if (EasternFantasy.Dungeon.DungeonRunController.Instance != null
            && EasternFantasy.Dungeon.DungeonRunController.Instance.IsExitPromptOpen) return;
        if (Time.timeScale <= 0f || playerInPortal == null || isLoadingScene
            || Keyboard.current?.upArrowKey.wasPressedThisFrame != true
            || (EasternFantasy.UI.DungeonWindowUI.Instance != null && EasternFantasy.UI.DungeonWindowUI.Instance.IsOpen))
            return;

        PlayerEntity entity = playerInPortal.GetComponent<PlayerEntity>();
        if (entity != null && entity.IsDead)
            return;

        if (NecklaceBlocked)
            EasternFantasy.UI.SystemNoticeUI.ShowFailure("목걸이를 T키로 조사해 획득한 뒤 이동하세요.", this);
        else if (QuestBlocked)
            EasternFantasy.UI.SystemNoticeUI.ShowFailure(questBlockedMessage, this);
        else if (!isLocked)
            MoveToScene();
    }

    private void MoveToScene()
    {
        if (IsLocked || Time.timeScale <= 0f || playerInPortal == null || isLoadingScene) return;
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"Portal '{name}' has no destination scene name.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"Portal '{name}' cannot load scene '{sceneName}'. Check the spelling and Build Settings.",
                this);
            return;
        }

        if (string.IsNullOrWhiteSpace(targetPointName))
        {
            Debug.LogError($"Portal '{name}' has no target portal name.", this);
            return;
        }

        if (!playerInPortal.TryGetComponent<PlayerLocationSetter>(out _))
            playerInPortal.gameObject.AddComponent<PlayerLocationSetter>();

        isLoadingScene = PortalSceneTransition.TryLoad(sceneName, targetPointName,
            success => { if (this != null) isLoadingScene = false; }, playPortalAudio: true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement2D player = other.GetComponentInParent<PlayerMovement2D>();
        if (player != null)
            playerInPortal = player;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerMovement2D player = other.GetComponentInParent<PlayerMovement2D>();
        if (player != null && player == playerInPortal)
            playerInPortal = null;
    }
}
