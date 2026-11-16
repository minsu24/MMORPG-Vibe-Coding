#ifndef TEMPLE_GATE_DECOR_MOTION
#define TEMPLE_GATE_DECOR_MOTION
// Shares the Village URP sprite lighting passes, with per-sprite atlas bounds.
float2 TempleDecorRegionOffset(float2 pixel, float4 region, float time)
{
    if (any(region.zw <= 0) || any(pixel <= region.xy) || any(pixel >= region.xy + region.zw))
        return 0;
    float2 local = saturate((pixel - region.xy) / region.zw);
    float height = _MotionMode > 0.5 ? 1 - local.y : local.y;
    // Foliage roots / cloth suspension stay fixed. Feather the region into the static artwork.
    float2 edge = min(pixel - region.xy, region.xy + region.zw - pixel);
    float mask = smoothstep(0, 12, edge.x) * smoothstep(0, 12, edge.y);
    float wave = sin(time - local.y * 2.7) + 0.23 * sin(time * 1.71 + local.y * 5.2);
    return float2(wave, 0.12 * sin(time * 1.23 + local.x * 3.1))
        * _FlagStrength * height * height * mask;
}

float2 ApplyTempleGateDecorMotion(float2 uv)
{
    float2 atlas = max(_AtlasSize.xy, 1);
    float2 pixel = uv * atlas;
    float time = (_PreviewTime >= 0 ? _PreviewTime : _Time.y) * _FlagSpeed + _FlagPhase;
    if (all(_MotionRect.zw > 0))
    {
        float2 offset = TempleDecorRegionOffset(pixel, _MotionRect, time)
            + TempleDecorRegionOffset(pixel, _SecondaryMotionRect, time);
        return clamp(pixel - offset, _FlagRegion.xy + 0.5,
            _FlagRegion.xy + _FlagRegion.zw - 0.5) / atlas;
    }
    float2 local = saturate((pixel - _FlagRegion.xy) / _FlagRegion.zw);
    float weight = local.y * local.y;
    if (_MotionMode > 0.5 && _MotionMode < 1.5)
        weight = (1-local.y) * (1-local.y); // Fix the suspension point.
    if (_MotionMode > 1.5)
        weight = smoothstep(0.58,0.92,local.y); // Keep the incense bowl and rim still.
    float wave = sin(time - local.y * 2.7) + 0.23 * sin(time * 1.71 + local.y * 5.2);
    float2 delta = float2(wave, 0.12*sin(time*1.23+local.x*3.1)) * _FlagStrength * weight;
    float2 samplePixel = clamp(pixel-delta, _FlagRegion.xy+0.5, _FlagRegion.xy+_FlagRegion.zw-0.5);
    return samplePixel / atlas;
}
#endif
