using EasternFantasy.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class MonkEnergyBarUI : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text valueText;

        public void Refresh(MonkEnergy energy)
        {
            if (energy == null)
                return;

            if (slider != null)
                slider.value = energy.Maximum > 0f
                    ? Mathf.Clamp01(energy.Current / energy.Maximum) : 0f;
            if (valueText != null)
                valueText.text = $"{energy.Current:F0}/{energy.Maximum:F0}";
        }
    }
}
