using EasternFantasy.Dialogue;
using EasternFantasy.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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
                Profile.SetActive(false);
        }

        private void Start()
        {
            if (playerEntity == null)
                playerEntity = FindFirstObjectByType<PlayerEntity>();
            if (playerProgression == null)
                playerProgression = FindFirstObjectByType<PlayerProgression>();

            if (playerEntity == null || playerProgression == null)
            {
                Debug.LogError("PlayerStatsWindowUI needs PlayerEntity and PlayerProgression.", this);
                enabled = false;
                return;
            }

            playerEntity.StatsChanged += Refresh;
            playerProgression.LevelChanged += OnLevelChanged;
            Refresh();
        }

        private void Update()
        {
            if (Keyboard.current?.cKey.wasPressedThisFrame == true)
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
            Profile.SetActive(open);
        }

        private void OnLevelChanged(int newLevel)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (playerEntity == null || playerProgression == null)
                return;

            if (classAndLevelText != null)
            {
                classAndLevelText.text =
                    $"{playerEntity.ClassName}    LEVEL  {playerProgression.CurrentLevel}";
            }

            if (HPstatsText == null || MPstatsText == null || AttackstatsText == null || DefensestatsText == null || 
            LifeStealstatsText == null || CriticalChancestatsText == null || CriticalDamagestatsText == null ||
            MoveSpeedstatsText == null)
                return;

            HPstatsText.text = $"체력 {playerEntity.HP:F0} / {playerEntity.maxHP:F0}\n\n";
            MPstatsText.text = $"마나 {playerEntity.MP:F0} / {playerEntity.maxMP:F0}\n\n";
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
