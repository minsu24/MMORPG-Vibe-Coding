using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace EasternFantasy.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerEntity), typeof(PlayerMovement2D), typeof(PlayerLocationSetter))]
    [RequireComponent(typeof(PlayerProgression))]
    public sealed class PlayerAdminController : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float ghostSpeed = 8f;
        [SerializeField, Min(1f)] private float fastMultiplier = 3f;

        private PlayerEntity entity;
        private PlayerProgression progression;
        private PlayerMovement2D movement;
        private PlayerInputReader input;
        private PlayerLocationSetter locationSetter;
        private PlayerDeathController deathController;
        private Rigidbody2D body;
        private bool panelOpen;
        private bool ghostMode;
        private bool inputWasEnabled;
        private bool movementWasEnabled;
        private bool bodyWasSimulated;
        private bool invincibleBeforeGhost;
        private Vector3 savedPosition;
        private Quaternion savedRotation;
        private string savedSceneName;
        private string pendingSceneName;
        private Vector2 sceneScroll;
        private Rect windowRect = new Rect(20f, 20f, 480f, 640f);

        private void Awake()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
            {
                enabled = false;
                return;
            }

            entity = GetComponent<PlayerEntity>();
            progression = GetComponent<PlayerProgression>();
            movement = GetComponent<PlayerMovement2D>();
            input = GetComponent<PlayerInputReader>();
            locationSetter = GetComponent<PlayerLocationSetter>();
            deathController = GetComponent<PlayerDeathController>();
            body = GetComponent<Rigidbody2D>();
            SavePosition();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (ghostMode)
                SetGhostMode(false);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.f10Key.wasPressedThisFrame)
                panelOpen = !panelOpen;
            if (keyboard.f9Key.wasPressedThisFrame)
                SetGhostMode(!ghostMode);

            if (!ghostMode)
                return;

            entity.isInvincible = true;
            Vector2 direction = Vector2.zero;
            if (keyboard.leftArrowKey.isPressed) direction.x -= 1f;
            if (keyboard.rightArrowKey.isPressed) direction.x += 1f;
            if (keyboard.downArrowKey.isPressed) direction.y -= 1f;
            if (keyboard.upArrowKey.isPressed) direction.y += 1f;
            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            bool fast = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            float speed = ghostSpeed * (fast ? fastMultiplier : 1f);
            transform.position += (Vector3)(direction * speed * Time.unscaledDeltaTime);
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(Mathf.Max(0f, Screen.width - 240f), 12f, 228f, 28f),
                ghostMode ? "F9 Ghost: ON  |  F10 Admin" : "F9 Ghost: OFF  |  F10 Admin");

            if (!panelOpen)
                return;

            windowRect.width = Mathf.Min(480f, Mathf.Max(200f, Screen.width - 24f));
            windowRect.height = Mathf.Min(640f, Mathf.Max(200f, Screen.height - 24f));
            windowRect = GUILayout.Window(GetInstanceID(), windowRect, DrawWindow, "Admin Mode  |  F10");
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.Label($"Scene: {SceneManager.GetActiveScene().name}");
            GUILayout.Label($"Position: {transform.position.x:F1}, {transform.position.y:F1}");
            GUILayout.Label($"HP {entity.HP:F0}/{entity.maxHP:F0}   MP {entity.MP:F0}/{entity.maxMP:F0}");
            GUILayout.Label($"Level {progression.CurrentLevel}/{PlayerLevelTable.MaximumLevel}   EXP {progression.CurrentExperience}/{progression.RequiredExperience}");

            GUI.enabled = !entity.IsDead;
            if (GUILayout.Button(ghostMode ? "Ghost Mode: ON  (F9 to exit)" : "Ghost Mode: OFF  (F9 to enter)"))
                SetGhostMode(!ghostMode);
            GUILayout.Label("Ghost: Arrows to fly, Shift to move faster; F9 to exit");

            if (GUILayout.Button("Save Position"))
                SavePosition();
            GUI.enabled = !entity.IsDead && savedSceneName == SceneManager.GetActiveScene().name;
            if (GUILayout.Button("Return to Saved Position"))
                TeleportToSavedPosition();

            GUI.enabled = true;
            if (GUILayout.Button(entity.IsDead ? "Respawn" : "Restore HP / MP"))
            {
                if (entity.IsDead)
                    deathController?.Respawn();
                else
                {
                    entity.RestoreHealth(entity.maxHP);
                    entity.RestoreMana(entity.maxMP);
                }
            }

            GUI.enabled = !entity.IsDead && !progression.IsMaximumLevel;
            if (GUILayout.Button("Level Up (+1)"))
                progression.AddExperience(progression.RequiredExperience - progression.CurrentExperience);

            GUI.enabled = !entity.IsDead;
            if (GUILayout.Button("Test Death"))
            {
                if (ghostMode)
                    SetGhostMode(false);
                entity.KillForTesting();
            }

            GUILayout.Space(8f);
            GUILayout.Label("Move to Scene (entry portal)");
            sceneScroll = GUILayout.BeginScrollView(sceneScroll, GUILayout.Height(220f));
            for (int index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(index);
                if (!path.StartsWith("Assets/_Project/Scenes/"))
                    continue;

                string sceneName = Path.GetFileNameWithoutExtension(path);
                if (sceneName == "MainScreen" || sceneName == "ChooseCharactor")
                    continue;

                if (GUILayout.Button(sceneName))
                    ChangeScene(sceneName);
            }
            GUILayout.EndScrollView();
            GUI.enabled = true;
            GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 24f));
        }

        private void SetGhostMode(bool enabledGhost)
        {
            if (ghostMode == enabledGhost || (enabledGhost && entity.IsDead))
                return;

            ghostMode = enabledGhost;
            if (ghostMode)
            {
                inputWasEnabled = input != null && input.enabled;
                movementWasEnabled = movement.enabled;
                bodyWasSimulated = body.simulated;
                invincibleBeforeGhost = entity.isInvincible;
                if (input != null) input.enabled = false;
                movement.ClearInput();
                movement.enabled = false;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.simulated = false;
                entity.isInvincible = true;
            }
            else
            {
                body.simulated = bodyWasSimulated;
                body.linearVelocity = Vector2.zero;
                movement.enabled = movementWasEnabled;
                if (input != null) input.enabled = inputWasEnabled;
                entity.isInvincible = invincibleBeforeGhost;
                Physics2D.SyncTransforms();
            }
        }

        private void SavePosition()
        {
            savedPosition = transform.position;
            savedRotation = transform.rotation;
            savedSceneName = SceneManager.GetActiveScene().name;
        }

        private void TeleportToSavedPosition()
        {
            if (savedSceneName != SceneManager.GetActiveScene().name)
                return;
            locationSetter.TeleportToAndRetarget(savedPosition, savedRotation);
        }

        private void ChangeScene(string sceneName)
        {
            if (entity.IsDead || string.IsNullOrEmpty(sceneName)
                || !Application.CanStreamedLevelBeLoaded(sceneName))
                return;

            pendingSceneName = sceneName;
            MapTransferData.Clear();
            SceneManager.LoadScene(sceneName);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != pendingSceneName)
                return;

            pendingSceneName = null;
            string entryName = GetEntryPortalName(scene.name);
            GameObject entry = string.IsNullOrEmpty(entryName)
                ? null
                : GameObject.Find(entryName);
            if (entry == null)
            {
                PortalManager[] portals = FindObjectsByType<PortalManager>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (PortalManager portal in portals)
                {
                    if (entry == null)
                        entry = portal.gameObject;
                    if (portal.name.StartsWith("In_"))
                    {
                        entry = portal.gameObject;
                        break;
                    }
                }
            }

            if (entry != null)
            {
                locationSetter.TeleportToAndRetarget(entry.transform);
                SavePosition();
            }
            else
                Debug.LogWarning($"No entry portal found in '{scene.name}'.", this);
        }

        private static string GetEntryPortalName(string sceneName)
        {
            switch (sceneName)
            {
                case "MovementPrototype": return "Out_Tuto_Portal";
                case "PrologueVilage": return "In_Vilage_Portal";
                case "Cemetery": return "In_Cemetery_Portal";
                case "SpiderCave": return "In_SpiderCave_Portal";
                case "Marshland": return "In_Marshland_Portal";
                case "BrokenSeal": return "In_BrokenSeal_Portal";
                default: return string.Empty;
            }
        }
    }
}
