using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

namespace EasternFantasy.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement2D))]
    public sealed class PlayerLocationSetter : MonoBehaviour
    {
        private static PlayerLocationSetter instance;
        private PlayerMovement2D playerController;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            playerController = GetComponent<PlayerMovement2D>();
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RetargetCinemachineCamera(transform.position);

            string targetName = MapTransferData.TargetSpawnPointName;
            if (string.IsNullOrWhiteSpace(targetName))
                return;

            GameObject spawnPoint = GameObject.Find(targetName);
            if (spawnPoint == null)
            {
                Debug.LogError(
                    $"Scene '{scene.name}' does not contain a portal named '{targetName}'.",
                    this);
                return;
            }

            playerController.TeleportTo(spawnPoint.transform.position, spawnPoint.transform.rotation);
            RetargetCinemachineCamera(spawnPoint.transform.position);
            MoveMainCameraTo(spawnPoint.transform.position);
            MapTransferData.Clear();

            Debug.Log($"Moved player to portal '{targetName}' in scene '{scene.name}'.", this);
        }

        private void RetargetCinemachineCamera(Vector3 targetPosition)
        {
            CinemachineCamera virtualCamera = FindFirstObjectByType<CinemachineCamera>(
                FindObjectsInactive.Include);
            if (virtualCamera == null)
            {
                Debug.LogWarning(
                    $"Scene '{SceneManager.GetActiveScene().name}' does not contain a CinemachineCamera.",
                    this);
                return;
            }

            CameraTarget target = virtualCamera.Target;
            target.TrackingTarget = transform;
            virtualCamera.Target = target;

            Vector3 cameraPosition = virtualCamera.transform.position;
            cameraPosition.x = targetPosition.x;
            cameraPosition.y = targetPosition.y;
            virtualCamera.ForceCameraPosition(cameraPosition, virtualCamera.transform.rotation);
        }

        private static void MoveMainCameraTo(Vector3 targetPosition)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            Vector3 cameraPosition = mainCamera.transform.position;
            cameraPosition.x = targetPosition.x;
            cameraPosition.y = targetPosition.y;
            mainCamera.transform.position = cameraPosition;
        }
    }
}
