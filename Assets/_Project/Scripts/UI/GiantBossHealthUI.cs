using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class GiantBossHealthUI : MonoBehaviour
    {
        public CanvasGroup visibility;
        public RectTransform panel;
        public Slider health;
        public TMP_Text healthText;
        private MON_Giant boss;
        private Tween gaugeTween;
        private float gaugeTarget = float.NaN;

        public void Bind(MON_Giant value)
        {
            boss = value;
            visibility.alpha = 0;
            visibility.interactable = visibility.blocksRaycasts = false;
        }

        public void Refresh(bool show)
        {
            if (!show || boss == null || !boss.isActiveAndEnabled || boss.HP <= 0f)
            {
                visibility.alpha = 0;
                ResetGauge();
                return;
            }
            // Same percentage calculation and DOTween gauge behavior as the player HUD.
            Utils.UpdateGauge(health, Utils.Percent(boss.HP, boss.maxHP), .25f,
                ref gaugeTarget, ref gaugeTween);
            healthText.SetText("{0:0} / {1:0}", boss.HP, boss.maxHP);
            visibility.alpha = 1;
            // Reserve the two upper corners for the player's HUD and minimap.
            float width = ((RectTransform)panel.parent).rect.width;
            float panelWidth = Mathf.Clamp(width - 800f, 420f, 800f);
            panel.sizeDelta = new Vector2(panelWidth, panelWidth * 400f / 2172f);
            panel.anchoredPosition = new Vector2(0, width < 1260f ? -420f : -201f);
        }

        private void ResetGauge()
        {
            gaugeTween?.Kill();
            gaugeTween = null;
            gaugeTarget = float.NaN;
        }

        private void OnDisable() => ResetGauge();
        private void OnDestroy() => ResetGauge();
    }
}
