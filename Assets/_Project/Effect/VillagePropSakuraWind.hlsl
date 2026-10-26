#ifndef EASTERN_FANTASY_VILLAGE_PROP_SAKURA_WIND
#define EASTERN_FANTASY_VILLAGE_PROP_SAKURA_WIND

#include "Assets/_Project/Effect/SakuraBreezeMotion.hlsl"

float2 ApplyVillagePropSakuraWind(float2 uv)
{
    const float2 atlasSize = float2(1536.0, 1024.0);
    float2 pixel = float2(uv.x, 1.0 - uv.y) * atlasSize;
    // Restrict movement to the blossoms and upper branches of prop 7.
    // The stone lantern, lower trunk, rocks, wall and ground foliage stay fixed.
    const float4 canopy = float4(872.0, 734.0, 1092.0, 895.0);
    if (_TreeStrength <= 0.0 || any(pixel <= canopy.xy) || any(pixel >= canopy.zw))
        return uv;
    float height = saturate((895.0 - pixel.y) / 161.0);
    float mask = smoothstep(canopy.x, canopy.x + 8.0, pixel.x)
        * (1.0 - smoothstep(canopy.z - 8.0, canopy.z, pixel.x));
    float time = _Time.y * _TreeSpeed + _TreePhase;
    float2 positionOS = float2(pixel.x - 982.0, 895.0 - pixel.y) / 100.0;
    float2 offset = SakuraBreezeOffset(positionOS, height, _TreeStrength / 100.0, time) * (100.0 * mask);
    float2 sampleUV = uv - float2(offset.x, -offset.y) / atlasSize;
    return clamp(sampleUV, float2(712.5, 43.5) / atlasSize, float2(1112.5, 289.5) / atlasSize);
}

#endif
