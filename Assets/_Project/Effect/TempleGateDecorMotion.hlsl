#ifndef TEMPLE_GATE_DECOR_MOTION
#define TEMPLE_GATE_DECOR_MOTION
// Shares the Village URP sprite lighting passes, with per-sprite atlas bounds.
float2 ApplyTempleGateDecorMotion(float2 uv)
{
    float2 atlas = float2(1448,1086);
    float2 pixel = uv * atlas;
    float2 local = saturate((pixel - _FlagRegion.xy) / _FlagRegion.zw);
    float time = (_PreviewTime >= 0 ? _PreviewTime : _Time.y) * _FlagSpeed + _FlagPhase;
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
