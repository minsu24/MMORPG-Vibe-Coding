#ifndef EASTERN_FANTASY_VILLAGE_PROP_BANNER_WIND
#define EASTERN_FANTASY_VILLAGE_PROP_BANNER_WIND

float2 ApplyVillagePropBannerWind(float2 uv)
{
    // Each material selects its cloth in top-left atlas coordinates.
    // The top seam and the surrounding wooden frame stay fixed.
    const float2 atlasSize = float2(1536.0, 1024.0);
    float2 pixel = float2(uv.x, 1.0 - uv.y) * atlasSize;
    float4 cloth = _FlagRegion;
    if (_FlagStrength <= 0.0 || any(pixel <= cloth.xy) || any(pixel >= cloth.zw))
        return uv;
    float fixedTop = cloth.y + 15.0;
    float v = saturate((pixel.y - fixedTop) / max(cloth.w - fixedTop, 1.0));
    float mask = smoothstep(cloth.x, cloth.x + 3.0, pixel.x)
        * (1.0 - smoothstep(cloth.z - 3.0, cloth.z, pixel.x))
        * (1.0 - smoothstep(cloth.w - 5.0, cloth.w, pixel.y));
    float time = _Time.y * _FlagSpeed + _FlagPhase;
    float wave = sin(time - v * 3.2) + 0.22 * sin(time * 1.67 - v * 5.4);
    float2 offset = float2(wave, sin(time * 1.19 - v * 4.0) * 0.13)
        * (_FlagStrength * v * v * mask);
    return uv - float2(offset.x, -offset.y) / atlasSize;
}

#endif
