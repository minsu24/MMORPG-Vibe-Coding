#ifndef EASTERN_FANTASY_VILLAGE_HARMONY
#define EASTERN_FANTASY_VILLAGE_HARMONY

float4 _MainTex_TexelSize;

float4 ApplyReadability(float4 color, float2 uv, float4 tint)
{
    // Alpha-weight neighboring detail so transparent frame edges do not bleed
    // black into the sprite. Keep the original silhouette and animation alpha.
    if (_Softness > 0)
    {
        float2 offset = _MainTex_TexelSize.xy * 0.75;
        float4 center = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
        float4 left = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(offset.x, 0));
        float4 right = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(offset.x, 0));
        float4 down = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0, offset.y));
        float4 up = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, offset.y));
        float weight = center.a * 4 + left.a + right.a + down.a + up.a;
        float3 smoothRgb = (center.rgb * center.a * 4 + left.rgb * left.a
            + right.rgb * right.a + down.rgb * down.a + up.rgb * up.a) / max(weight, 0.0001);

        color.rgb = lerp(color.rgb, smoothRgb, _Softness);
    }
    color *= tint;
    float luminance = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
    color.rgb = lerp(luminance.xxx, color.rgb, _Saturation);
    color.rgb = max(0, (color.rgb - 0.18) * _Contrast + 0.18) * _Brightness;
    return color;
}
#endif