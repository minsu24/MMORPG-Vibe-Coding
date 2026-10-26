#ifndef EASTERN_FANTASY_VILLAGE_PLATFORM_CLOTH_WIND
#define EASTERN_FANTASY_VILLAGE_PLATFORM_CLOTH_WIND

float2 PlatformClothOffset(float2 pixel, float4 bounds, float fixedTop, float phase, float amplitude)
{
    if (any(pixel <= bounds.xy) || any(pixel >= bounds.zw) || pixel.y <= fixedTop)
        return 0;
    float height = saturate((pixel.y - fixedTop) / (bounds.w - fixedTop));
    float mask = smoothstep(bounds.x, bounds.x + 2.0, pixel.x)
        * (1.0 - smoothstep(bounds.z - 2.0, bounds.z, pixel.x))
        * (1.0 - smoothstep(bounds.w - 3.0, bounds.w, pixel.y));
    float time = _Time.y * _ClothSpeed + _ClothPhase + phase;
    float wave = sin(time - height * 3.0) + 0.22 * sin(time * 1.63 - height * 5.0);
    return float2(wave, 0.12 * sin(time * 1.17 - height * 4.0))
        * (_ClothStrength * amplitude * height * height * mask);
}

float2 ApplyVillagePlatformClothWind(float2 uv)
{
    const float2 atlasSize = float2(1536.0, 1024.0);
    float2 pixel = float2(uv.x, 1.0 - uv.y) * atlasSize;
    if (_ClothStrength <= 0.0 || pixel.x < 500.0 || pixel.x > 618.0 || pixel.y < 462.0 || pixel.y > 519.0)
        return uv;
    // Three cloth strips only. Their attachment points, the supporting rope,
    // the wooden platform and the long dangling rope remain stationary.
    float2 offset = PlatformClothOffset(pixel, float4(500, 453, 526, 505), 466, 0.4, 1.0);
    offset += PlatformClothOffset(pixel, float4(572, 458, 596, 519), 477, 1.6, 1.1);
    offset += PlatformClothOffset(pixel, float4(596, 443, 618, 487), 462, 2.8, 0.85);
    return uv - float2(offset.x, -offset.y) / atlasSize;
}

#endif
