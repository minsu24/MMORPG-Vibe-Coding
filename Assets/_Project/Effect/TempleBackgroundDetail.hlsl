#ifndef EASTERN_FANTASY_TEMPLE_BACKGROUND_DETAIL
#define EASTERN_FANTASY_TEMPLE_BACKGROUND_DETAIL

// Restore local stone/roof contrast when a background is enlarged. Limit the
// correction to avoid bright outlines, and leave broad mist gradients intact.
float3 TempleBackgroundDetail(float3 center, float2 uv)
{
    if (_DetailSharpness <= 0) return center;
    float2 d = _MainTex_TexelSize.xy * _DetailRadius;
    float3 neighbors =
        SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(d.x, 0)).rgb +
        SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(d.x, 0)).rgb +
        SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, d.y)).rgb +
        SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0, d.y)).rgb;
    float3 detail = clamp(center - neighbors * .25, -.055, .055);
    return saturate(center + detail * _DetailSharpness);
}
#endif
