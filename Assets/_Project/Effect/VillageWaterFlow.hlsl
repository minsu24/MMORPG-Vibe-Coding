#ifndef EASTERN_FANTASY_VILLAGE_WATER_FLOW
#define EASTERN_FANTASY_VILLAGE_WATER_FLOW

float WaterFlowerExclusion(float2 pixel, float2 center, float2 radius)
{
    return smoothstep(1.0, 1.2, length((pixel - center) / radius));
}

float VillageRiverRegion(float2 pixel)
{
    if (_WaterStrength <= 0.0)
        return 0;

    if (_ForegroundLanterns > 0.5)
    {
        if (pixel.x < 1490.0 || pixel.x > 2280.0 || pixel.y < 1664.0 || pixel.y > 1758.0)
            return 0;
        float pond = smoothstep(1490.0, 1530.0, pixel.x)
            * (1.0 - smoothstep(2230.0, 2280.0, pixel.x))
            * smoothstep(1664.0, 1682.0, pixel.y)
            * (1.0 - smoothstep(1745.0, 1758.0, pixel.y));
        // Blossom silhouettes are excluded explicitly. The color mask below
        // additionally holds the green leaves and dark stems stationary.
        pond *= WaterFlowerExclusion(pixel, float2(1620, 1693), float2(49, 33));
        pond *= WaterFlowerExclusion(pixel, float2(1685, 1686), float2(35, 30));
        pond *= WaterFlowerExclusion(pixel, float2(1750, 1643), float2(78, 64));
        pond *= WaterFlowerExclusion(pixel, float2(1980, 1707), float2(39, 29));
        pond *= WaterFlowerExclusion(pixel, float2(2085, 1690), float2(41, 30));
        // Leave enough clearance around every leaf and stem that a displaced
        // water sample cannot pull plant pixels into its animated reflection.
        pond *= WaterFlowerExclusion(pixel, float2(1754, 1738), float2(78, 25));
        pond *= WaterFlowerExclusion(pixel, float2(1868, 1703), float2(78, 25));
        pond *= WaterFlowerExclusion(pixel, float2(1900, 1666), float2(55, 20));
        pond *= WaterFlowerExclusion(pixel, float2(2050, 1659), float2(62, 21));
        pond *= WaterFlowerExclusion(pixel, float2(1955, 1744), float2(95, 24));
        pond *= WaterFlowerExclusion(pixel, float2(2086, 1709), float2(76, 25));
        pond *= WaterFlowerExclusion(pixel, float2(1750, 1705), float2(16, 55));
        pond *= WaterFlowerExclusion(pixel, float2(1620, 1735), float2(14, 25));
        pond *= WaterFlowerExclusion(pixel, float2(1680, 1725), float2(12, 35));
        pond *= WaterFlowerExclusion(pixel, float2(1870, 1735), float2(14, 35));
        pond *= WaterFlowerExclusion(pixel, float2(1900, 1685), float2(12, 35));
        pond *= WaterFlowerExclusion(pixel, float2(2050, 1684), float2(12, 32));
        pond *= WaterFlowerExclusion(pixel, float2(2085, 1732), float2(13, 40));
        pond *= WaterFlowerExclusion(pixel, float2(1950, 1734), float2(12, 35));
        pond *= WaterFlowerExclusion(pixel, float2(1505, 1700), float2(90, 95));
        pond *= WaterFlowerExclusion(pixel, float2(2240, 1700), float2(95, 100));
        return pond;
    }

    if (pixel.x < 1510.0 || pixel.x > 2835.0 || pixel.y < 1610.0 || pixel.y > 1760.0)
        return 0;

    // Stay inside the painted river, below the rocks and bridge supports.
    // The raised right boundary follows the bank beside the terrace.
    float top = lerp(1675.0, 1625.0, smoothstep(1510.0, 1730.0, pixel.x));
    top = lerp(top, 1624.0, smoothstep(1900.0, 2500.0, pixel.x));
    top = lerp(top, 1720.0, smoothstep(2550.0, 2780.0, pixel.x));
    float bottom = 1720.0 + 30.0 * sin(saturate((pixel.x - 1600.0) / 1300.0) * 3.14159265);
    float horizontal = smoothstep(1510.0, 1540.0, pixel.x)
        * (1.0 - smoothstep(2805.0, 2835.0, pixel.x));
    return horizontal * smoothstep(top, top + 12.0, pixel.y)
        * (1.0 - smoothstep(bottom - 12.0, bottom, pixel.y));
}

float VillageWaterColorMask(float4 color)
{
    // Green vegetation, warm stones and transparent image edges remain fixed.
    return smoothstep(0.02, 0.10, color.b - color.r * 0.85)
        * (1.0 - smoothstep(0.02, 0.14, color.r - color.g))
        * smoothstep(0.8, 0.98, color.a);
}

float2 ApplyVillageWaterFlow(float2 uv)
{
    const float2 mapSize = float2(3764.0, 2118.0);
    float2 pixel = float2(uv.x, 1.0 - uv.y) * mapSize;
    float mask = VillageRiverRegion(pixel);
    if (mask <= 0.0)
        return uv;
    mask *= VillageWaterColorMask(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv));
    float time = _Time.y * _WaterSpeed + _WaterPhase;
    float2 drift;
    drift.x = sin(pixel.y * 0.20 - time * 1.2)
        + sin(pixel.y * 0.43 + time * 0.7) * 0.3;
    drift.y = sin(pixel.x * 0.045 - time * 1.4) * 0.45
        + sin(pixel.x * 0.09 + pixel.y * 0.12 + time) * 0.16;
    return uv + float2(drift.x, -drift.y) * (_WaterStrength * mask) / mapSize;
}

float4 ApplyVillageWaterShimmer(float4 color, float2 uv)
{
    float2 pixel = float2(uv.x, 1.0 - uv.y) * float2(3764.0, 2118.0);
    float mask = VillageRiverRegion(pixel);
    if (mask <= 0.0)
        return color;
    mask *= VillageWaterColorMask(color);
    float time = _Time.y * _WaterSpeed + _WaterPhase;
    float ripple = sin(pixel.y * 0.25 + sin(pixel.x * 0.018 + time * 0.9) * 1.2 - time * 1.5)
        * sin(pixel.x * 0.022 - time * 0.6);
    color.rgb *= 1.0 + ripple * mask * _WaterShimmer;
    return color;
}

#endif
