#ifndef EASTERN_FANTASY_GOBLIN_TONE
#define EASTERN_FANTASY_GOBLIN_TONE
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
// Match walking skin to the idle sheet, leaving pale hair, clothes and club intact.
// Ratios come from median opaque skin pixels in the two original sheets.
half4 MatchGoblinTone(half4 color)
{
    if (_MovementTone <= 0) return color;
    float3 rgb = color.rgb;
#ifndef UNITY_COLORSPACE_GAMMA
    rgb = LinearToSRGB(rgb);
#endif
    float skin = smoothstep(1.25, 1.65, rgb.r / max(rgb.g, 0.001));
    skin *= smoothstep(1.02, 1.18, rgb.g / max(rgb.b, 0.001));
    float3 matched = saturate(rgb * float3(0.875, 1.114, 1.164));
    rgb = lerp(rgb, matched, skin * _MovementTone);
#ifndef UNITY_COLORSPACE_GAMMA
    rgb = SRGBToLinear(rgb);
#endif
    color.rgb = rgb;
    return color;
}
#endif
