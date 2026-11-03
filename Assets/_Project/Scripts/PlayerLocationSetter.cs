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
            if (instance != null && instance != this && instance.gameObject.activeInHierarchy)
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
            string targetName = MapTransferData.TargetSpawnPointName;
            if (string.IsNullOrWhiteSpace(targetName))
            {
                RetargetCinemachineCamera(transform.position);
                return;
            }

            // Consume only this scene transfer, even when its destination is missing.
            MapTransferData.Clear();
            GameObject spawnPoint = FindSpawnPoint(scene, targetName);
            if (spawnPoint == null)
            {
                Debug.LogError(
                    $"Scene '{scene.name}' does not contain a portal named '{targetName}'.",
                    this);
                return;
            }

            TeleportToAndRetarget(spawnPoint.transform);

            Debug.Log($"Moved player to portal '{targetName}' in scene '{scene.name}'.", this);
        }

        private static GameObject FindSpawnPoint(Scene scene, string targetName)
        {
            // A quest/boss may hide the destination portal. Its position is still a
            // valid arrival anchor; finding it must not activate or unlock it.
            string normalizedName = targetName.Trim();
            GameObject result = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!string.Equals(candidate.name.Trim(), normalizedName, System.StringComparison.Ordinal)) continue;
                    if (result != null)
                    {
                        Debug.LogError($"Scene '{scene.name}' has multiple spawn points named '{normalizedName}'.");
                        return null;
                    }
                    result = candidate.gameObject;
                }
            }
            return result;
        }

        public void TeleportToAndRetarget(Transform destination)
        {
            if (destination == null)
                return;

            // An explicit child anchor denotes the player's origin. Otherwise align the
            // player's feet to support near the portal, whose pivot may be at its base.
            var arrival = destination.Find("ArrivalPoint");
            Vector3 position = arrival != null ? arrival.position
                : playerController.ResolvePortalArrivalPosition(destination);
            TeleportToAndRetarget(position, arrival != null ? arrival.rotation : destination.rotation);
        }

        public void TeleportToAndRetarget(Vector3 position, Quaternion rotation)
        {
            playerController.TeleportTo(position, rotation);
            RetargetCinemachineCamera(position);
            MoveMainCameraTo(position);
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
