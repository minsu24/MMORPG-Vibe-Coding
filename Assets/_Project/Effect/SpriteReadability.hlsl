#ifndef EASTERN_FANTASY_SPRITE_READABILITY
#define EASTERN_FANTASY_SPRITE_READABILITY

float4 _MainTex_TexelSize;

float4 ApplyReadability(float4 color, float2 uv)
{
    float luminance = color.r * 0.2126 + color.g * 0.7152 + color.b * 0.0722;
    color.rgb = lerp(float3(luminance, luminance, luminance), color.rgb, _Saturation) * _Brightness;
    color.rgb = lerp(color.rgb, half3(1, 1, 1), _FlashAmount);

    // An inward contour works with tightly trimmed sprites and packed animation frames.
    // Only the transparent silhouette boundary darkens; the original alpha is unchanged.
    if (_OutlineWidth > 0)
    {
        float2 step = _MainTex_TexelSize.xy * _OutlineWidth;
        half minAlpha = 1;
        minAlpha = min(minAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(step.x, 0)).a);
        minAlpha = min(minAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(step.x, 0)).a);
        minAlpha = min(minAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, step.y)).a);
        minAlpha = min(minAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0, step.y)).a);
        minAlpha = min(minAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + step * 0.7071).a);
        minAlpha = min(minAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - step * 0.7071).a);
        minAlpha = min(minAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(step.x, -step.y) * 0.7071).a);
        minAlpha = min(minAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-step.x, step.y) * 0.7071).a);
        half contour = 1 - smoothstep(0.05, 0.8, minAlpha);
        color.rgb = lerp(color.rgb, _OutlineColor.rgb, contour * _OutlineColor.a);
    }
    return color;
}
#endif
