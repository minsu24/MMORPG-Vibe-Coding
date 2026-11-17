using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    public sealed class SkullMonkBossHealthUI : MonoBehaviour
    {
        // Reserve the upper-left player HUD area even when the canvases scale differently.
        public const float MaximumPanelWidth = 920f;
        public const float PanelWidthFraction = .48f;
        public CanvasGroup visibility;
        public RectTransform panel;
        public Slider health;
        public TMP_Text healthText;
        private MON_SkullMonk boss;
        private Tween gaugeTween;
        private float gaugeTarget = float.NaN;

        public void Bind(MON_SkullMonk value)
        {
            boss = value;
            visibility.alpha = 0;
            visibility.interactable = visibility.blocksRaycasts = false;
            health.SetValueWithoutNotify(Utils.Percent(boss.HP, boss.maxHP));
        }

        public void Refresh(bool show)
        {
            if (!show || boss == null || !boss.isActiveAndEnabled || boss.HP <= 0f)
            {
                visibility.alpha = 0;
                ResetGauge();
                return;
            }
            Utils.UpdateGauge(health, Utils.Percent(boss.HP, boss.maxHP), .25f,
                ref gaugeTarget, ref gaugeTween);
            healthText.SetText("{0:0} / {1:0}", boss.HP, boss.maxHP);
            float width = ((RectTransform)panel.parent).rect.width;
            float panelWidth = Mathf.Min(MaximumPanelWidth, width * PanelWidthFraction);
            float aspect = panel.GetComponent<Image>().sprite.rect.height / panel.GetComponent<Image>().sprite.rect.width;
            panel.sizeDelta = new Vector2(panelWidth, panelWidth * aspect);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, 1f);
            panel.anchoredPosition = new Vector2(0, -12f);
            visibility.alpha = 1;
        }

        private void ResetGauge()
        {
            gaugeTween?.Kill(); gaugeTween = null; gaugeTarget = float.NaN;
        }
        private void OnDisable() => ResetGauge();
        private void OnDestroy() => ResetGauge();
    }
}
