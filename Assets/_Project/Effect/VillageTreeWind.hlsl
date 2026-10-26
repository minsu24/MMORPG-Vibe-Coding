#ifndef EASTERN_FANTASY_VILLAGE_TREE_WIND
#define EASTERN_FANTASY_VILLAGE_TREE_WIND

float VillageTreeFixedObject(float2 pixel, float4 bounds)
{
    // Feather outside the protected object, keeping its complete silhouette fixed.
    float2 a = smoothstep(bounds.xy - 14.0, bounds.xy, pixel);
    float2 b = 1.0 - smoothstep(bounds.zw, bounds.zw + 14.0, pixel);
    return 1.0 - a.x * a.y * b.x * b.y;
}

float2 ApplyVillageTreeWind(float2 uv)
{
    if (_TreeStrength <= 0.0 || _ForegroundLanterns < 0.5)
        return uv;
    const float2 mapSize = float2(3764.0, 2118.0);
    float2 pixel = float2(uv.x, 1.0 - uv.y) * mapSize;
    if (pixel.x > 1355.0 || pixel.y > 850.0)
        return uv;

    float mask = (1.0 - smoothstep(1320.0, 1355.0, pixel.x))
        * (1.0 - smoothstep(825.0, 850.0, pixel.y));
    mask *= VillageTreeFixedObject(pixel, float4(0, 720, 245, 850));
    mask *= VillageTreeFixedObject(pixel, float4(276, 644, 494, 1210));
    mask *= VillageTreeFixedObject(pixel, float4(480, 798, 760, 1320));
    mask *= VillageTreeFixedObject(pixel, float4(0, 810, 430, 1050));
    if (mask <= 0.0)
        return uv;

    float height = 1.0 - smoothstep(180.0, 820.0, pixel.y);
    float reach = smoothstep(250.0, 1100.0, pixel.x);
    // Upper branches flex gently; the long right branch has more freedom at its tip.
    float weight = max(height * 0.85,
        reach * (1.0 - smoothstep(770.0, 850.0, pixel.y)) * 0.8);
    float time = _Time.y * _TreeSpeed + _TreePhase;
    float breeze = sin(time) * 0.75 + sin(time * 0.57 + 1.1) * 0.25;
    float leaves = sin(pixel.x * 0.018 + pixel.y * 0.01 - time * 2.1);
    float2 drift;
    drift.x = breeze + leaves * 0.22;
    drift.y = sin(time * 0.83 + pixel.x * 0.005) * 0.18
        + cos(pixel.x * 0.015 + pixel.y * 0.012 - time * 2.4) * 0.07;
    drift *= _TreeStrength * weight * mask;
    return uv - float2(drift.x, -drift.y) / mapSize;
}

#endif
