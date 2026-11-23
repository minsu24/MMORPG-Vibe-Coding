using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
public sealed class RemoteMountainAtmosphere : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float fogDensity = 0.28f;
    [SerializeField, Range(0f, 1f)] private float cloudDensity = 0.72f;

    private MeshRenderer visual;
    private MaterialPropertyBlock properties;
    private float elapsed;
    private static readonly int MotionTime = Shader.PropertyToID("_MotionTime");
    private static readonly int FogDensity = Shader.PropertyToID("_FogDensity");
    private static readonly int CloudDensity = Shader.PropertyToID("_CloudDensity");

    private void OnEnable() => Sample(elapsed);

    private void Update()
    {
        elapsed += Time.deltaTime;
        Sample(elapsed);
    }

    public void Sample(float time)
    {
        if (visual == null) visual = GetComponent<MeshRenderer>();
        if (properties == null) properties = new MaterialPropertyBlock();
        visual.GetPropertyBlock(properties);
        properties.SetFloat(MotionTime, time);
        properties.SetFloat(FogDensity, fogDensity);
        properties.SetFloat(CloudDensity, cloudDensity);
        visual.SetPropertyBlock(properties);
    }
}
