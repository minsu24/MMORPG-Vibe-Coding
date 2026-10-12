using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using EasternFantasy.CharacterSelection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class PlayerStatsWindowUI : MonoBehaviour
    {
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private GameObject Profile;
        [SerializeField] private TMP_Text classAndLevelText;
        [SerializeField] private TMP_Text HPstatsText;
        [SerializeField] private TMP_Text MPstatsText;
        [SerializeField] private TMP_Text AttackstatsText;
        [SerializeField] private TMP_Text DefensestatsText;
        [SerializeField] private TMP_Text LifeStealstatsText;
        [SerializeField] private TMP_Text CriticalChancestatsText;
        [SerializeField] private TMP_Text CriticalDamagestatsText;
        [SerializeField] private TMP_Text MoveSpeedstatsText;

        [SerializeField] private PlayerEntity playerEntity;
        [SerializeField] private PlayerProgression playerProgression;

        private Image portraitBackground;
        private Texture2D portraitBackgroundTexture;
        private Sprite portraitBackgroundSprite;
        private bool portraitLayoutInitialized;
        private Vector3 normalPortraitPosition;
        private Vector2 normalPortraitSize;

        public static PlayerStatsWindowUI Instance { get; private set; }
        public bool IsOpen => windowRoot != null && windowRoot.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (windowRoot != null)
                windowRoot.SetActive(false);
            if (Profile != null)
                Profile.SetActive(false);
        }

        private void Start()
        {
            BindCurrentPlayer();
            Refresh();
        }

        private void BindCurrentPlayer()
        {
            PlayerEntity current = FindFirstObjectByType<PlayerEntity>();
            if (current == playerEntity && playerProgression != null)
                return;

            if (playerEntity != null)
                playerEntity.StatsChanged -= Refresh;
            if (playerProgression != null)
                playerProgression.LevelChanged -= OnLevelChanged;

            playerEntity = current;
            playerProgression = current != null
                ? current.GetComponent<PlayerProgression>() : null;
            if (playerEntity != null)
                playerEntity.StatsChanged += Refresh;
            if (playerProgression != null)
                playerProgression.LevelChanged += OnLevelChanged;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (IsOpen && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetOpen(false);
                return;
            }

            if (keyboard.cKey.wasPressedThisFrame)
            {
                bool dialogueBlocksOpening = !IsOpen
                    && DialogueManager.Instance != null
                    && DialogueManager.Instance.IsDialogueActive;

                if (!dialogueBlocksOpening)
                    SetOpen(!IsOpen);
            }

            if (IsOpen)
                Refresh();
        }

        public void SetOpen(bool open)
        {
            if (open && EquipmentUpgradeWindowUI.Instance != null && EquipmentUpgradeWindowUI.Instance.IsOpen) return;
            if (windowRoot == null)
                return;

            if (open && SkillWindowUI.Instance != null)
                SkillWindowUI.Instance.SetOpen(false);
            if (open && InventoryWindowUI.Instance != null)
                InventoryWindowUI.Instance.SetOpen(false);

            if (open)
                Refresh();
            windowRoot.SetActive(open);
            if (Profile != null)
                Profile.SetActive(open);
            if (portraitBackground != null)
                portraitBackground.gameObject.SetActive(open);
        }

        private void OnLevelChanged(int newLevel)
        {
            Refresh();
        }

        public void Refresh()
        {
            BindCurrentPlayer();
            if (playerEntity == null || playerProgression == null)
                return;

            if (classAndLevelText != null)
            {
                CharacterClassDefinition definition = PlayerClassRuntime.ActiveDefinition;
                string className = definition != null ? definition.DisplayName : playerEntity.ClassName;
                classAndLevelText.text = $"{className}    LEVEL  {playerProgression.CurrentLevel}";
                if (Profile != null && definition != null)
                {
                    Image portrait = Profile.GetComponent<Image>();
                    Sprite portraitSprite = definition.StatsPortrait;
                    if (portrait != null && portraitSprite != null)
                    {
                        portrait.sprite = portraitSprite;
                        portrait.preserveAspect = true;
                        AlignPortrait(portrait, definition);
                    }
                }
            }

            if (HPstatsText == null || MPstatsText == null || AttackstatsText == null || DefensestatsText == null || 
            LifeStealstatsText == null || CriticalChancestatsText == null || CriticalDamagestatsText == null ||
            MoveSpeedstatsText == null)
                return;

            HPstatsText.text = $"체력 {playerEntity.HP:F0} / {playerEntity.maxHP:F0}";
            MonkEnergy monkEnergy = playerEntity.Energy;
            MPstatsText.text = monkEnergy != null
                ? $"기력 {monkEnergy.Current:F0} / {monkEnergy.Maximum:F0}"
                : $"{playerEntity.ResourceName} {playerEntity.MP:F0} / {playerEntity.maxMP:F0}";
            AttackstatsText.text = $"공격력 {playerEntity.Attack_Power:F0}";
            DefensestatsText.text = $"방어력 {playerEntity.Defense:F0}";
            LifeStealstatsText.text = $"흡혈 {playerEntity.LifeStealRate * 100f:F1}%";
            CriticalChancestatsText.text = $"치명타 확률 {playerEntity.CriticalChance * 100f:F1}%";
            CriticalDamagestatsText.text = $"치명타 피해 {playerEntity.CriticalDamageMultiplier * 100f:F0}%";
            MoveSpeedstatsText.text = $"이동속도 {playerEntity.Speed:F1}";
        }

        private void AlignPortrait(Image portrait, CharacterClassDefinition definition)
        {
            RectTransform portraitRect = portrait.rectTransform;
            if (!portraitLayoutInitialized)
            {
                normalPortraitPosition = portraitRect.localPosition;
                normalPortraitSize = portraitRect.sizeDelta;
                portraitLayoutInitialized = true;
            }

            UpdatePortraitBackground(portrait);
            if (definition.ClassId != CharacterClassId.Monk)
            {
                portraitRect.localPosition = normalPortraitPosition;
                portraitRect.sizeDelta = normalPortraitSize;
                return;
            }

            RectTransform frame = windowRoot != null ? windowRoot.GetComponent<RectTransform>() : null;
            if (frame == null) return;

            // The circular opening in the stats-frame artwork is centered at (50%, 70.6%).
            Rect frameRect = frame.rect;
            Vector3 center = new Vector3(frameRect.center.x,
                frameRect.yMin + frameRect.height * 0.706f, 0f);
            portraitRect.position = frame.TransformPoint(center);
            float diameter = Mathf.Min(frameRect.width * 0.47f, frameRect.height * 0.35f) * 0.96f;
            Vector3 size = portraitRect.parent.InverseTransformVector(
                frame.TransformVector(new Vector3(diameter, diameter, 0f)));
            portraitRect.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
        }
        private void UpdatePortraitBackground(Image portrait)
        {
            RectTransform frame = windowRoot != null ? windowRoot.GetComponent<RectTransform>() : null;
            if (frame == null) return;
            if (portraitBackground == null)
            {
                const int resolution = 256;
                portraitBackgroundTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
                portraitBackgroundTexture.name = "Stats Portrait Circular Backdrop";
                portraitBackgroundTexture.wrapMode = TextureWrapMode.Clamp;
                portraitBackgroundTexture.filterMode = FilterMode.Bilinear;
                Color centerColor = new Color(0.14f, 0.18f, 0.19f, 1f);
                Color edgeColor = new Color(0.025f, 0.035f, 0.04f, 1f);
                Color[] pixels = new Color[resolution * resolution];
                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        Vector2 point = new Vector2((x + 0.5f) / resolution * 2f - 1f,
                            (y + 0.5f) / resolution * 2f - 1f);
                        float radius = point.magnitude;
                        Color color = Color.Lerp(centerColor, edgeColor, Mathf.SmoothStep(0f, 1f, radius));
                        color.a = Mathf.Clamp01((1f - radius) * resolution * 0.5f);
                        pixels[y * resolution + x] = color;
                    }
                }
                portraitBackgroundTexture.SetPixels(pixels);
                portraitBackgroundTexture.Apply(false, true);
                portraitBackgroundSprite = Sprite.Create(portraitBackgroundTexture,
                    new Rect(0f, 0f, resolution, resolution), new Vector2(0.5f, 0.5f));
                GameObject backdrop = new GameObject("Portrait Background", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
                backdrop.layer = portrait.gameObject.layer;
                backdrop.transform.SetParent(portrait.transform.parent, false);
                backdrop.transform.SetSiblingIndex(portrait.transform.GetSiblingIndex());
                portraitBackground = backdrop.GetComponent<Image>();
                portraitBackground.sprite = portraitBackgroundSprite;
                portraitBackground.raycastTarget = false;
                backdrop.SetActive(IsOpen);
            }

            Rect frameRect = frame.rect;
            RectTransform backgroundRect = portraitBackground.rectTransform;
            backgroundRect.position = frame.TransformPoint(new Vector3(frameRect.center.x,
                frameRect.yMin + frameRect.height * 0.706f, 0f));
            // Extend under the gold rim so no scenery shows through the edge of the opening.
            float diameter = Mathf.Min(frameRect.width * 0.50f, frameRect.height * 0.375f);
            Vector3 size = backgroundRect.parent.InverseTransformVector(
                frame.TransformVector(new Vector3(diameter, diameter, 0f)));
            backgroundRect.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
        }
        private void OnDestroy()
        {
            if (playerEntity != null)
                playerEntity.StatsChanged -= Refresh;
            if (playerProgression != null)
                playerProgression.LevelChanged -= OnLevelChanged;

            if (portraitBackgroundSprite != null) Destroy(portraitBackgroundSprite);
            if (portraitBackgroundTexture != null) Destroy(portraitBackgroundTexture);

            if (Instance == this)
                Instance = null;
        }
    }
}
