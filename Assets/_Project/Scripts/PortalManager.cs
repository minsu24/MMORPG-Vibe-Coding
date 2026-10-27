using EasternFantasy.Player;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public sealed class PortalManager : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private string targetPointName;

    [SerializeField] private bool startsLocked;
    [SerializeField] private bool requiresToadNecklace;
    private bool isLocked;
    private bool NecklaceBlocked => requiresToadNecklace &&
        (EasternFantasy.Quest.ToadHuntQuestController.Instance == null ||
         !EasternFantasy.Quest.ToadHuntQuestController.Instance.NecklaceClaimed);
    public bool IsLocked => isLocked || NecklaceBlocked;
    public void SetLocked(bool locked) => isLocked = locked;
    private void Awake() => isLocked = startsLocked;

    private PlayerMovement2D playerInPortal;
    private bool isLoadingScene;

    private void Update()
    {
        if (EasternFantasy.Dungeon.DungeonRunController.Instance != null
            && EasternFantasy.Dungeon.DungeonRunController.Instance.IsExitPromptOpen) return;
        if (Time.timeScale > 0f && playerInPortal != null && NecklaceBlocked
            && Keyboard.current?.upArrowKey.wasPressedThisFrame == true)
            EasternFantasy.UI.SystemNoticeUI.Show("목걸이를 T키로 조사해 획득한 뒤 이동하세요.", this);
        if (IsLocked || Time.timeScale <= 0f || playerInPortal == null || isLoadingScene || (EasternFantasy.UI.DungeonWindowUI.Instance != null && EasternFantasy.UI.DungeonWindowUI.Instance.IsOpen))
            return;

        PlayerEntity entity = playerInPortal.GetComponent<PlayerEntity>();
        if (entity != null && entity.IsDead)
            return;

        if (Keyboard.current?.upArrowKey.wasPressedThisFrame == true)
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
            success => { if (this != null) isLoadingScene = false; });
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
