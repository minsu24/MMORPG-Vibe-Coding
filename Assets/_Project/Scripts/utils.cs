using UnityEngine;

public class Utils
{
    public static float Percent(float current, float max)
    {
        return current != 0 && max != 0 ? current/max : 0; // 슬라이더 시각적 표시를 위한 비율 계산 식
    }
}
