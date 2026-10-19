using System.Collections;
using EasternFantasy.CharacterSelection;
using EasternFantasy.UI;
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
        private const string FirstDeathGuideKeyPrefix = "Tutorial.FirstDeath.Completed.";

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
        private bool isFirstDeath;
        private string firstDeathGuideKey;

        private void Awake()
        {
            entity = GetComponent<PlayerEntity>();
            movement = GetComponent<PlayerMovement2D>();
            input = GetComponent<PlayerInputReader>();
            animationController = GetComponent<PlayerIdleAnimation>();
            body = GetComponent<Rigidbody2D>();
            tutorialSpawnPosition = transform.position;
            tutorialSpawnRotation = transform.rotation;
            PlayerClassRuntime playerClass = GetComponent<PlayerClassRuntime>();
            firstDeathGuideKey = FirstDeathGuideKeyPrefix + (playerClass != null
                ? playerClass.RepresentedClass : CharacterSelectionState.SelectedClass);
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
            isFirstDeath = PlayerPrefs.GetInt(firstDeathGuideKey, 0) == 0;
            deathUi = deathUiPrefab != null
                ? Instantiate(deathUiPrefab, transform)
                : CreateTemporaryDeathUi();
            DeathScreenUI screen = deathUi.GetComponent<DeathScreenUI>();
            if (screen != null)
                screen.Configure(isFirstDeath, SceneManager.GetActiveScene().name == TutorialScene);
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
            // Complete the guide only after a successful revival, so failed transfers can be retried.
            if (isFirstDeath)
            {
                PlayerPrefs.SetInt(firstDeathGuideKey, 1);
                PlayerPrefs.Save();
            }
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

            CreateText(shade.transform, "가사상태", 64f, new Vector2(0f, 200f));
            if (isFirstDeath)
                CreateText(shade.transform,
                    "생명력이 모두 소진되어 가사상태에 빠졌습니다.\n가사상태에서는 이동하거나 전투할 수 없습니다.",
                    26f, new Vector2(0f, 65f));
            GameObject buttonObject = new GameObject("Respawn Button", typeof(RectTransform),
                typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(shade.transform, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(300f, 90f);
            buttonRect.anchoredPosition = new Vector2(0f, -65f);
            buttonObject.GetComponent<Image>().color = new Color(0.35f, 0.35f, 0.35f, 1f);
            CreateText(buttonObject.transform, "부활", 38f, Vector2.zero);
            CreateText(shade.transform, SceneManager.GetActiveScene().name == TutorialScene
                ? "이 수련장에서는 시작 지점에서 부활합니다."
                : "부활 버튼을 누르면 가까운 마을에서 부활합니다.", 26f, new Vector2(0f, -180f));
            return canvasObject;
        }

        private static void CreateText(Transform parent, string content, float fontSize, Vector2 position)
        {
            GameObject textObject = new GameObject(content, typeof(RectTransform),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(950f, 100f);
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
