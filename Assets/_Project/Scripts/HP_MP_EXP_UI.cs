using UnityEngine;
using TMPro;
using EasternFantasy.Player;
using UnityEngine.UI;


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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        playerController = player.GetComponent<PlayerEntity>();
        playerProgression = player.GetComponent<PlayerProgression>();
    }

    // Update is called once per frame
    private void Update()
    {
        if(sliderHP != null) sliderHP.value = Utils.Percent(playerController.HP, playerController.maxHP); // 슬라이드를 이용한 HP 표시
        if(textHP != null) textHP.text = $"{playerController.HP:F0}/{playerController.maxHP:F0}"; // 수치에 따른 텍스트 변경

        if(sliderMP != null) sliderMP.value = Utils.Percent(playerController.MP, playerController.maxMP); // 슬라이드를 이용한 MP 표시
        if(textMP != null) textMP.text = $"{playerController.MP:F0}/{playerController.maxMP:F0}";

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
