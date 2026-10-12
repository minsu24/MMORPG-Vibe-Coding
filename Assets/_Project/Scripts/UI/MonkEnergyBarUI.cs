using DG.Tweening;
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
        [SerializeField, Min(0f)] private float tweenDuration = 0.25f;
        private Tween gaugeTween;
        private float gaugeTarget = float.NaN;
        private MonkEnergy boundEnergy;

        public void Refresh(MonkEnergy energy)
        {
            if (energy == null)
                return;

            if (boundEnergy != energy)
            {
                ResetGaugeAnimation();
                boundEnergy = energy;
            }

            Utils.UpdateGauge(slider, Utils.Percent(energy.Current, energy.Maximum),
                tweenDuration, ref gaugeTarget, ref gaugeTween);
            if (valueText != null)
                valueText.text = $"{energy.Current:F0}/{energy.Maximum:F0}";
        }

        private void ResetGaugeAnimation()
        {
            gaugeTween?.Kill();
            gaugeTween = null;
            gaugeTarget = float.NaN;
            boundEnergy = null;
        }

        private void OnDisable() => ResetGaugeAnimation();
        private void OnDestroy() => ResetGaugeAnimation();
    }
}
