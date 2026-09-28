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
                    Sprite portraitSprite = definition.DialogueCharacter != null
                        && definition.DialogueCharacter.Portrait != null
                        ? definition.DialogueCharacter.Portrait : definition.Portrait;
                    if (portrait != null && portraitSprite != null)
                    {
                        portrait.sprite = portraitSprite;
                        portrait.preserveAspect = true;
                    }
                }
            }

            if (HPstatsText == null || MPstatsText == null || AttackstatsText == null || DefensestatsText == null || 
            LifeStealstatsText == null || CriticalChancestatsText == null || CriticalDamagestatsText == null ||
            MoveSpeedstatsText == null)
                return;

            HPstatsText.text = $"체력 {playerEntity.HP:F0} / {playerEntity.maxHP:F0}\n\n";
            MonkEnergy monkEnergy = playerEntity.Energy;
            MPstatsText.text = monkEnergy != null
                ? $"기력 {monkEnergy.Current:F0} / {monkEnergy.Maximum:F0}\n\n"
                : $"{playerEntity.ResourceName} {playerEntity.MP:F0} / {playerEntity.maxMP:F0}\n\n";
            AttackstatsText.text = $"공격력 {playerEntity.Attack_Power:F0}\n\n";
            DefensestatsText.text = $"방어력 {playerEntity.Defense:F0}\n\n";
            LifeStealstatsText.text = $"흡혈 {playerEntity.LifeStealRate * 100f:F1}%\n\n";
            CriticalChancestatsText.text = $"크리티컬 확률 {playerEntity.CriticalChance * 100f:F1}%\n\n";
            CriticalDamagestatsText.text = $"크리티컬 데미지 {playerEntity.CriticalDamageMultiplier * 100f:F0}%\n\n";
            MoveSpeedstatsText.text = $"이동속도 {playerEntity.Speed:F1}";
        }

        private void OnDestroy()
        {
            if (playerEntity != null)
                playerEntity.StatsChanged -= Refresh;
            if (playerProgression != null)
                playerProgression.LevelChanged -= OnLevelChanged;

            if (Instance == this)
                Instance = null;
        }
    }
}
