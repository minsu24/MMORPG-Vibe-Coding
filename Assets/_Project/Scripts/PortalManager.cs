using EasternFantasy.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public sealed class PortalManager : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private string targetPointName;

    private PlayerMovement2D playerInPortal;
    private bool isLoadingScene;

    private void Update()
    {
        if (playerInPortal == null || isLoadingScene)
            return;

        if (Keyboard.current?.upArrowKey.wasPressedThisFrame == true)
            MoveToScene();
    }

    private void MoveToScene()
    {
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

        MapTransferData.SetTarget(targetPointName);
        isLoadingScene = true;
        SceneManager.LoadScene(sceneName);
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
