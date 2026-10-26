#ifndef EASTERN_FANTASY_BLACKSMITH_CLOTH_WIND
#define EASTERN_FANTASY_BLACKSMITH_CLOTH_WIND

float2 BlacksmithPanelOffset(float2 pixel, float4 span, float2 hem, float phase)
{
    if (pixel.x <= span.x || pixel.x >= span.y)
        return float2(0.0, 0.0);
    float u = saturate((pixel.x - span.x) / (span.y - span.x));
    float top = lerp(span.z, span.w, u);
    float bottom = lerp(hem.x, hem.y, u);
    float mask = smoothstep(span.x, span.x + 4.0, pixel.x)
        * (1.0 - smoothstep(span.y - 4.0, span.y, pixel.x))
        * smoothstep(top + 6.0, top + 15.0, pixel.y)
        * (1.0 - smoothstep(bottom - 5.0, bottom - 1.0, pixel.y));
    if (mask <= 0.0)
        return float2(0.0, 0.0);
    float v = saturate((pixel.y - top) / max(bottom - top, 1.0));
    float time = _Time.y * _AwningSpeed + _AwningPhase + phase;
    float wave = sin(time - v * 3.2 + u * 0.5) * 0.85
        + sin(time * 1.73 - v * 5.0 + phase) * 0.15;
    return float2(wave, sin(time * 1.1 + u * 4.0) * 0.17)
        * (_AwningStrength * v * v * mask);
}

float2 BlacksmithSideBannerOffset(float2 pixel, float4 bounds, float phase, float fixedMask)
{
    if (any(pixel <= bounds.xy) || any(pixel >= bounds.zw))
        return float2(0.0, 0.0);
    float2 a = smoothstep(bounds.xy, bounds.xy + float2(4.0, 15.0), pixel);
    float2 b = 1.0 - smoothstep(bounds.zw - float2(4.0, 5.0), bounds.zw, pixel);
    float height = saturate((pixel.y - bounds.y - 14.0) / (bounds.w - bounds.y - 14.0));
    float mask = a.x * a.y * b.x * b.y * fixedMask;
    float time = _Time.y * _AwningSpeed + _AwningPhase + phase;
    float wave = sin(time - height * 3.0) + 0.22 * sin(time * 1.63 - height * 5.5);
    return float2(wave, 0.12 * sin(time * 1.1 - height * 4.0))
        * (_BannerStrength * height * height * mask);
}

float2 BlacksmithRightLanternOffset(float2 pixel)
{
    const float4 bounds = float4(1152.0, 438.0, 1262.0, 577.0);
    if (any(pixel <= bounds.xy) || any(pixel >= bounds.zw) || _LanternSwingDegrees <= 0.0)
        return float2(0.0, 0.0);
    float2 a = smoothstep(bounds.xy, bounds.xy + float2(5.0, 12.0), pixel);
    float2 b = 1.0 - smoothstep(bounds.zw - float2(5.0, 4.0), bounds.zw, pixel);
    float mask = a.x * a.y * b.x * b.y;
    float time = _Time.y * _LanternSwingSpeed + _AwningPhase + 0.5;
    float angle = radians(_LanternSwingDegrees)
        * (sin(time) * 0.85 + sin(time * 0.57 + 1.3) * 0.15);
    float sine = sin(angle);
    float cosine = cos(angle);
    float2 delta = pixel - float2(1209.0, 434.0);
    float2 source = float2(cosine * delta.x + sine * delta.y,
        -sine * delta.x + cosine * delta.y);
    return (delta - source) * mask;
}

float2 ApplyBlacksmithClothWind(float2 uv)
{
    if (_AwningStrength <= 0.0 && _BannerStrength <= 0.0 && _LanternSwingDegrees <= 0.0)
        return uv;
    const float2 imageSize = float2(1448.0, 1086.0);
    float2 pixel = float2(uv.x, 1.0 - uv.y) * imageSize;
    if (pixel.x < 60.0 || pixel.x > 1262.0 || pixel.y < 438.0 || pixel.y > 782.0)
        return uv;
    // Masks stay inside the cloth silhouettes; ropes, lanterns, and the wall
    // behind the separate panels never receive the displacement.
    float2 offset = float2(0.0, 0.0);
    offset += BlacksmithPanelOffset(pixel, float4(491, 539, 483, 485), float2(540, 541), 0.2);
    offset += BlacksmithPanelOffset(pixel, float4(570, 734, 487, 501), float2(541, 555), 1.0);
    offset += BlacksmithPanelOffset(pixel, float4(748, 892, 499, 506), float2(554, 563), 1.7);
    offset += BlacksmithPanelOffset(pixel, float4(908, 1058, 507, 509), float2(559, 552), 2.5);
    offset += BlacksmithPanelOffset(pixel, float4(1095, 1136, 512, 512), float2(563, 564), 3.1);
    // The left banner overlaps tool handles at its foot. Keep those foreground
    // tools stationary while the exposed fabric and lower left fringe sway.
    float tools = smoothstep(98.0, 106.0, pixel.x)
        * (1.0 - smoothstep(190.0, 198.0, pixel.x))
        * smoothstep(736.0, 744.0, pixel.y);
    offset += BlacksmithSideBannerOffset(pixel, float4(68, 538, 183, 780), 0.4, 1.0 - tools);
    offset += BlacksmithSideBannerOffset(pixel, float4(1155, 578, 1237, 762), 2.1, 1.0);
    offset += BlacksmithRightLanternOffset(pixel);
    return uv - float2(offset.x, -offset.y) / imageSize;
}

#endif
