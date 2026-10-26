#ifndef EASTERN_FANTASY_VILLAGE_CLOTH_WIND
#define EASTERN_FANTASY_VILLAGE_CLOTH_WIND

float ClothRegionMask(float2 pixel, float4 bounds)
{
    float2 a = smoothstep(bounds.xy, bounds.xy + float2(3.0, 4.0), pixel);
    float2 b = 1.0 - smoothstep(bounds.zw - float2(3.0, 4.0), bounds.zw, pixel);
    return a.x * a.y * b.x * b.y;
}

float ClothFixedEllipse(float2 pixel, float2 center, float2 radius)
{
    return smoothstep(1.0, 1.16, length((pixel - center) / radius));
}

float2 ClothBannerOffset(float2 pixel, float4 bounds, float phase, float amplitude, float fixedMask)
{
    float mask = ClothRegionMask(pixel, bounds) * fixedMask;
    if (mask <= 0.0)
        return float2(0.0, 0.0);
    float height = saturate((pixel.y - bounds.y - 12.0) / max(bounds.w - bounds.y - 12.0, 1.0));
    float weight = height * height * (3.0 - 2.0 * height);
    float time = _Time.y * _ClothSpeed + _ClothPhase + phase;
    float gust = 0.82 + 0.18 * sin(time * 0.47 + phase);
    float wave = sin(time - height * 3.8) + 0.28 * sin(time * 1.73 - height * 7.0 + phase);
    float2 offset = float2(wave, 0.18 * sin(time * 1.3 + pixel.x * 0.035 - height * 4.0));
    return offset * (_ClothStrength * amplitude * weight * mask * gust);
}

float2 ClothCanopyOffset(float2 pixel, float4 bounds, float4 topProfile, float3 bottomProfile, float phase)
{
    if (any(pixel < bounds.xy) || any(pixel > bounds.zw))
        return float2(0.0, 0.0);
    float u = saturate((pixel.x - bounds.x) / (bounds.z - bounds.x));
    float top = lerp(topProfile.x, topProfile.y, u) + sin(u * 3.14159265) * topProfile.w;
    if (topProfile.z > bounds.x && topProfile.z < bounds.z)
    {
        top = pixel.x < topProfile.z
            ? lerp(topProfile.x, topProfile.w, saturate((pixel.x - bounds.x) / (topProfile.z - bounds.x)))
            : lerp(topProfile.w, topProfile.y, saturate((pixel.x - topProfile.z) / (bounds.z - topProfile.z)));
    }
    float bottom = lerp(bottomProfile.x, bottomProfile.y, u) + sin(u * 3.14159265) * bottomProfile.z;
    float mask = ClothRegionMask(pixel, bounds) * smoothstep(top + 4.0, top + 14.0, pixel.y)
        * (1.0 - smoothstep(bottom, bottom + 7.0, pixel.y));
    if (mask <= 0.0)
        return float2(0.0, 0.0);
    float v = saturate((pixel.y - top) / max(bottom - top, 1.0));
    // Both rope ends and the attached upper seam stay stationary.
    float weight = smoothstep(0.05, 0.95, v) * pow(max(sin(u * 3.14159265), 0.0), 0.6);
    float time = _Time.y * _ClothSpeed + _ClothPhase + phase;
    float wave = sin(time + u * 6.0 - v * 1.5) + 0.25 * sin(time * 1.61 - u * 11.0);
    return float2(sin(time * 0.83 + u * 5.0) * 0.30, wave * 0.75)
        * (_ClothStrength * weight * mask);
}

float2 ApplyVillageClothWind(float2 uv)
{
    if (_ClothStrength <= 0.0)
        return uv;
    const float2 mapSize = float2(3764.0, 2118.0);
    float2 pixel = float2(uv.x, 1.0 - uv.y) * mapSize;
    float2 offset = float2(0.0, 0.0);
    if (_ForegroundLanterns > 0.5)
    {
        // Seven ribbons tied to the large tree, each with its own wind phase.
        if (pixel.x >= 180.0 && pixel.x <= 590.0 && pixel.y >= 350.0 && pixel.y <= 800.0)
        {
            offset += ClothBannerOffset(pixel, float4(189, 403, 233, 535), 0.0, 0.55, 1.0);
            offset += ClothBannerOffset(pixel, float4(266, 358, 310, 488), 0.6, 0.55, 1.0);
            offset += ClothBannerOffset(pixel, float4(319, 360, 353, 455), 1.1, 0.48, 1.0);
            offset += ClothBannerOffset(pixel, float4(224, 624, 269, 797), 1.7, 0.62, 1.0);
            offset += ClothBannerOffset(pixel, float4(358, 576, 404, 657), 2.1, 0.45, 1.0);
            offset += ClothBannerOffset(pixel, float4(488, 531, 542, 775), 2.6, 0.68, 1.0);
            offset += ClothBannerOffset(pixel, float4(546, 508, 586, 655), 3.0, 0.55, 1.0);
        }
        // Keep the roof and stone lanterns that overlap the banners fixed.
        if (pixel.y >= 700.0 && pixel.y <= 1550.0)
        {
            // The large red crest banner on the left stays stationary.
            offset += ClothBannerOffset(pixel, float4(600, 904, 717, 1302), 1.5, 0.95, 1.0);
            offset += ClothBannerOffset(pixel, float4(2880, 1180, 3012, 1542), 2.5, 1.0,
                ClothFixedEllipse(pixel, float2(2860, 1535), float2(95, 80)));
            offset += ClothBannerOffset(pixel, float4(3620, 1180, 3747, 1514), 3.4, 1.0,
                ClothFixedEllipse(pixel, float2(3750, 1530), float2(90, 75)));
        }
        offset += ClothCanopyOffset(pixel, float4(255, 1238, 610, 1352),
            float4(1245, 1290, 0, -12), float3(1315, 1325, 22), 0.8)
            * ClothFixedEllipse(pixel, float2(587, 1300), float2(13, 90));
        offset += ClothCanopyOffset(pixel, float4(3170, 1318, 3490, 1490),
            float4(1423, 1355, 3408, 1328), float3(1465, 1450, 25), 2.4);
    }
    else
    {
        offset += ClothCanopyOffset(pixel, float4(0, 1125, 257, 1225),
            float4(1133, 1166, 180, 1135), float3(1190, 1204, 17), 1.3);
        offset += ClothCanopyOffset(pixel, float4(3418, 1238, 3700, 1328),
            float4(1246, 1248, 0, 9), float3(1300, 1300, 17), 3.1);
    }
    return uv - float2(offset.x, -offset.y) / mapSize;
}

#endif
