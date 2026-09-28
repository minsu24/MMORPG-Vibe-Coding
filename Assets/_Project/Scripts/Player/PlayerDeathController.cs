using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerEntity), typeof(PlayerMovement2D))]
    public sealed class PlayerDeathController : MonoBehaviour
    {
        private const string TutorialScene = "MovementPrototype";
        private const string VillageScene = "PrologueVilage";
        private const string VillageSpawn = "In_Vilage_Portal";

        [SerializeField, Min(0f)] private float deathAnimationSeconds = 1f;
        [SerializeField, Range(0f, 0.2f)] private float deathHitStopSeconds = 0.08f;
        [Tooltip("Optional replacement for the temporary death UI. It must contain a Canvas and a Button.")]
        [SerializeField] private GameObject deathUiPrefab;

        private PlayerEntity entity;
        private PlayerMovement2D movement;
        private PlayerInputReader input;
        private PlayerIdleAnimation animationController;
        private Rigidbody2D body;
        private GameObject deathUi;
        private Vector3 tutorialSpawnPosition;
        private Quaternion tutorialSpawnRotation;
        private bool wasInputEnabled;
        private bool wasMovementEnabled;
        private bool previousEnemyCollisionIgnored;
        private bool isRespawning;

        private void Awake()
        {
            entity = GetComponent<PlayerEntity>();
            movement = GetComponent<PlayerMovement2D>();
            input = GetComponent<PlayerInputReader>();
            animationController = GetComponent<PlayerIdleAnimation>();
            body = GetComponent<Rigidbody2D>();
            tutorialSpawnPosition = transform.position;
            tutorialSpawnRotation = transform.rotation;
        }

        private void OnEnable()
        {
            entity.Died += HandleDeath;
        }

        private void OnDisable()
        {
            entity.Died -= HandleDeath;
        }

        private void HandleDeath()
        {
            if (isRespawning || deathUi != null)
                return;

            wasInputEnabled = input != null && input.enabled;
            wasMovementEnabled = movement.enabled;
            if (input != null)
                input.enabled = false;
            movement.ClearInput();
            movement.enabled = false;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            int playerLayer = gameObject.layer;
            if (enemyLayer >= 0)
            {
                previousEnemyCollisionIgnored = Physics2D.GetIgnoreLayerCollision(enemyLayer, playerLayer);
                Physics2D.IgnoreLayerCollision(enemyLayer, playerLayer, true);
            }

            if (animationController != null)
                animationController.PlayDeath();
            float hitStopDelay = GameTimeController.RequestHitStop(deathHitStopSeconds);
            StartCoroutine(ShowDeathUiAfterAnimation(hitStopDelay));
        }

        private IEnumerator ShowDeathUiAfterAnimation(float hitStopDelay)
        {
            yield return new WaitForSecondsRealtime(deathAnimationSeconds + hitStopDelay);
            deathUi = deathUiPrefab != null
                ? Instantiate(deathUiPrefab, transform)
                : CreateTemporaryDeathUi();
            Button respawnButton = deathUi.GetComponentInChildren<Button>(true);
            if (respawnButton != null)
                respawnButton.onClick.AddListener(Respawn);
            else
                Debug.LogError("Death UI needs a Button to respawn the player.", deathUi);
        }

        public void Respawn()
        {
            if (!entity.IsDead || isRespawning)
                return;
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            isRespawning = true;
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene == TutorialScene)
            {
                movement.TeleportTo(tutorialSpawnPosition, tutorialSpawnRotation);
            }
            else
            {
                if (currentScene != VillageScene)
                {
                    if (!Application.CanStreamedLevelBeLoaded(VillageScene))
                    {
                        Debug.LogError($"Respawn scene '{VillageScene}' is not in Build Settings.", this);
                        isRespawning = false;
                        yield break;
                    }
                    MapTransferData.SetTarget(VillageSpawn);
                    SceneManager.LoadScene(VillageScene);
                    yield return null;
                }

                GameObject spawn = GameObject.Find(VillageSpawn);
                if (spawn == null)
                {
                    Debug.LogError($"Respawn point '{VillageSpawn}' is missing from '{VillageScene}'.", this);
                    isRespawning = false;
                    yield break;
                }
                movement.TeleportTo(spawn.transform.position, spawn.transform.rotation);
            }

            entity.Revive();
            if (animationController != null)
                animationController.PlayIdle();

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                Physics2D.IgnoreLayerCollision(enemyLayer, gameObject.layer,
                    previousEnemyCollisionIgnored);

            movement.enabled = wasMovementEnabled;
            if (input != null)
                input.enabled = wasInputEnabled;
            if (deathUi != null)
                Destroy(deathUi);
            deathUi = null;
            isRespawning = false;
        }

        private GameObject CreateTemporaryDeathUi()
        {
            GameObject canvasObject = new GameObject("Temporary Death UI", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject shade = new GameObject("Dark Background", typeof(RectTransform), typeof(Image));
            shade.transform.SetParent(canvasObject.transform, false);
            RectTransform shadeRect = shade.GetComponent<RectTransform>();
            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = Vector2.zero;
            shadeRect.offsetMax = Vector2.zero;
            shade.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            CreateText(shade.transform, "You Died", 64f, new Vector2(0f, 65f));
            GameObject buttonObject = new GameObject("Respawn Button", typeof(RectTransform),
                typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(shade.transform, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(300f, 90f);
            buttonRect.anchoredPosition = new Vector2(0f, -65f);
            buttonObject.GetComponent<Image>().color = new Color(0.35f, 0.35f, 0.35f, 1f);
            CreateText(buttonObject.transform, "Respawn", 38f, Vector2.zero);
            return canvasObject;
        }

        private static void CreateText(Transform parent, string content, float fontSize, Vector2 position)
        {
            GameObject textObject = new GameObject(content, typeof(RectTransform),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 100f);
            rect.anchoredPosition = position;
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = content;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
        }
    }
}
