using DG.Tweening;
using UnityEngine;
using TMPro;
using EasternFantasy.Player;
using UnityEngine.UI;
using EasternFantasy.UI;


public class HP_MP_EXP_UI : MonoBehaviour
{
    [SerializeField]
    private PlayerEntity playerController;
    private PlayerProgression playerProgression;
    private GameObject player;
    [SerializeField]
    private Slider sliderHP;
    [SerializeField]
    private TextMeshProUGUI textHP;
    [SerializeField]
    private Slider sliderMP;
    [SerializeField]
    private TextMeshProUGUI textMP;
    [SerializeField]
    private Slider sliderEXP;
    [SerializeField]
    private TextMeshProUGUI textEXP;
    [SerializeField] private MonkEnergyBarUI monkEnergyBarPrefab;
    private GameObject manaBarRoot;
    private bool manaBarWasActive;
    private MonkEnergyBarUI energyBar;
    private bool? showingEnergy;

    [Header("DOTween gauge timing")]
    [SerializeField, Min(0f)] private float hpTweenDuration = 0.3f;
    [SerializeField, Min(0f)] private float mpTweenDuration = 0.25f;
    [SerializeField, Min(0f)] private float expTweenDuration = 0.45f;
    private Tween hpTween, mpTween, expTween;
    private float hpTarget = float.NaN, mpTarget = float.NaN, expTarget = float.NaN;
    private int displayedLevel = -1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (playerController != null)
            playerProgression = playerController.GetComponent<PlayerProgression>();
        if (sliderMP != null)
        {
            manaBarRoot = sliderMP.transform.parent.gameObject;
            manaBarWasActive = manaBarRoot.activeSelf;
        }
    }

    private bool TryBindPlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;
        playerController = player.GetComponent<PlayerEntity>();
        playerProgression = player.GetComponent<PlayerProgression>();
        showingEnergy = null;
        ResetGaugeAnimations();
        return playerController != null;
    }
    // Update is called once per frame
    private void Update()
    {
        if ((playerController == null || !playerController.gameObject.activeInHierarchy)
            && !TryBindPlayer()) return;

        Utils.UpdateGauge(sliderHP, Utils.Percent(playerController.HP, playerController.maxHP),
            hpTweenDuration, ref hpTarget, ref hpTween);
        if(textHP != null) textHP.text = $"{playerController.HP:F0}/{playerController.maxHP:F0}"; // 수치에 따른 텍스트 변경

        MonkEnergy energy = playerController.Energy;
        bool isMonk = energy != null;
        if (showingEnergy != isMonk)
        {
            showingEnergy = isMonk;
            if (isMonk && energyBar == null && manaBarRoot != null)
            {
                MonkEnergyBarUI prefab = monkEnergyBarPrefab;
                if (prefab != null)
                {
                    energyBar = Instantiate(prefab, manaBarRoot.transform.parent, false);
                    energyBar.transform.SetSiblingIndex(manaBarRoot.transform.GetSiblingIndex() + 1);
                }
                else
                    Debug.LogError("Assign the MonkEnergyBar prefab to the HUD.", this);
            }
            if (manaBarRoot != null)
                manaBarRoot.SetActive((!isMonk || energyBar == null)
                    && manaBarWasActive);
            if (energyBar != null)
                energyBar.gameObject.SetActive(isMonk);
        }
        if (isMonk)
            energyBar?.Refresh(energy);
        else
        {
            Utils.UpdateGauge(sliderMP, Utils.Percent(playerController.MP, playerController.maxMP),
                mpTweenDuration, ref mpTarget, ref mpTween);
            if (textMP != null)
                textMP.text = $"{playerController.MP:F0}/{playerController.maxMP:F0}";
        }

        if (playerProgression != null)
        {
            // A new level starts a new EXP range; do not animate backwards through the old one.
            if (displayedLevel != playerProgression.CurrentLevel)
            {
                displayedLevel = playerProgression.CurrentLevel;
                expTarget = float.NaN;
            }
            Utils.UpdateGauge(sliderEXP, playerProgression.ExperienceNormalized,
                expTweenDuration, ref expTarget, ref expTween);

            if (textEXP != null)
            {
                textEXP.text = playerProgression.IsMaximumLevel
                    ? $"Lv.{playerProgression.CurrentLevel} MAX"
                    : $"Lv.{playerProgression.CurrentLevel}  "
                      + $"{playerProgression.CurrentExperience}/{playerProgression.RequiredExperience}";
            }
        }

    }

    private void ResetGaugeAnimations()
    {
        hpTween?.Kill();
        mpTween?.Kill();
        expTween?.Kill();
        hpTween = mpTween = expTween = null;
        hpTarget = mpTarget = expTarget = float.NaN;
        displayedLevel = -1;
    }

    private void OnDisable() => ResetGaugeAnimations();
    private void OnDestroy() => ResetGaugeAnimations();
}
