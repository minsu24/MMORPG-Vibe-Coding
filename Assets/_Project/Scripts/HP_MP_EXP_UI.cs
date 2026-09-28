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
    private GameObject manaBarRoot;
    private bool manaBarWasActive;
    private MonkEnergyBarUI energyBar;
    private bool? showingEnergy;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        playerController = player.GetComponent<PlayerEntity>();
        playerProgression = player.GetComponent<PlayerProgression>();

        if (sliderMP != null)
        {
            manaBarRoot = sliderMP.transform.parent.gameObject;
            manaBarWasActive = manaBarRoot.activeSelf;
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if(sliderHP != null) sliderHP.value = Utils.Percent(playerController.HP, playerController.maxHP); // 슬라이드를 이용한 HP 표시
        if(textHP != null) textHP.text = $"{playerController.HP:F0}/{playerController.maxHP:F0}"; // 수치에 따른 텍스트 변경

        MonkEnergy energy = playerController.Energy;
        bool isMonk = energy != null;
        if (showingEnergy != isMonk)
        {
            showingEnergy = isMonk;
            if (isMonk && energyBar == null && manaBarRoot != null)
            {
                MonkEnergyBarUI prefab = Resources.Load<MonkEnergyBarUI>("UI/MonkEnergyBar");
                if (prefab != null)
                {
                    energyBar = Instantiate(prefab, manaBarRoot.transform.parent, false);
                    energyBar.transform.SetSiblingIndex(manaBarRoot.transform.GetSiblingIndex() + 1);
                }
                else
                    Debug.LogError("MonkEnergyBar prefab is missing from Resources/UI.", this);
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
            if (sliderMP != null)
                sliderMP.value = Utils.Percent(playerController.MP, playerController.maxMP);
            if (textMP != null)
                textMP.text = $"{playerController.MP:F0}/{playerController.maxMP:F0}";
        }

        if (playerProgression != null)
        {
            if (sliderEXP != null)
                sliderEXP.value = playerProgression.ExperienceNormalized;

            if (textEXP != null)
            {
                textEXP.text = playerProgression.IsMaximumLevel
                    ? $"Lv.{playerProgression.CurrentLevel} MAX"
                    : $"Lv.{playerProgression.CurrentLevel}  "
                      + $"{playerProgression.CurrentExperience}/{playerProgression.RequiredExperience}";
            }
        }

        
    }
}
