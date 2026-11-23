using UnityEngine;

[DisallowMultipleComponent]
public sealed class CaveLanternGlow : MonoBehaviour
{
    private static readonly int LanternTime = Shader.PropertyToID("_CaveLanternTime");

    private void LateUpdate()
    {
        // The cave bell pauses gameplay while the camera shows the lanterns.
        Shader.SetGlobalFloat(LanternTime, Time.unscaledTime);
    }
}
