#ifndef EASTERN_FANTASY_SAKURA_WIND
#define EASTERN_FANTASY_SAKURA_WIND

#include "Assets/_Project/Effect/SakuraBreezeMotion.hlsl"

float2 ApplySakuraWindUV(float2 uv)
{
    // Warp inside one sprite cell, so atlas neighbors cannot leak into the
    // canopy. The roots and lower trunk remain completely stationary.
    float2 positionOS = ((uv - _SpriteUVRect.xy) / _SpriteUVRect.zw - 0.5) * _SpriteSize.xy;
    float height = saturate((positionOS.y - _WindBaseY) / max(_WindHeight, 0.001));
    float time = _Time.y * _WindSpeed + _WindPhase;
    float2 offset = SakuraBreezeOffset(positionOS, height, _WindStrength, time);
    float2 sampleUV = uv - offset / _SpriteSize.xy * _SpriteUVRect.zw;
    return clamp(sampleUV, _SpriteUVRect.xy + _MainTex_TexelSize.xy * 0.5,
        _SpriteUVRect.xy + _SpriteUVRect.zw - _MainTex_TexelSize.xy * 0.5);
}

#endif
