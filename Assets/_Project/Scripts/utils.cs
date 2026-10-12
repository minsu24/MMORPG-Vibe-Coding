using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Utils
{
    public static float Percent(float current, float max)
    {
        return current != 0 && max != 0 ? current/max : 0; // 슬라이더 시각적 표시를 위한 비율 계산 식
    }

    // Call every frame; start a tween only when the actual target changes.
    public static void UpdateGauge(Slider slider, float value, float duration,
        ref float previousTarget, ref Tween tween)
    {
        if (slider == null) return;
        float target = Mathf.Clamp01(value);
        if (!float.IsNaN(previousTarget) && Mathf.Approximately(previousTarget, target))
            return;

        tween?.Kill();
        tween = null;
        bool initialize = float.IsNaN(previousTarget);
        previousTarget = target;
        if (initialize || duration <= 0f || !slider.gameObject.activeInHierarchy)
        {
            slider.value = target;
            return;
        }

        tween = slider.DOValue(target, duration).SetEase(Ease.OutQuad).SetUpdate(true);
    }
}
