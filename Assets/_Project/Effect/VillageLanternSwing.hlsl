#ifndef EASTERN_FANTASY_VILLAGE_LANTERN_SWING
#define EASTERN_FANTASY_VILLAGE_LANTERN_SWING

float2 LanternSwingOffset(float2 pixel, float4 region, float2 pivot, float phase)
{
    // Coordinates describe the original 3764 x 2118 village illustration.
    // Feather only the small hanging-lantern region; the pole and roof stay fixed.
    float2 low = smoothstep(region.xy, region.xy + float2(3.0, 5.0), pixel);
    float2 high = 1.0 - smoothstep(region.zw - float2(3.0, 6.0), region.zw, pixel);
    float mask = low.x * low.y * high.x * high.y;
    if (mask <= 0.0)
        return 0;
    float time = _Time.y * _SwingSpeed + _SwingPhase + phase;
    float angle = radians(_SwingDegrees) * (sin(time) * 0.82 + sin(time * 0.67 + 1.2) * 0.18);
    float sine = 0.0;
    float cosine = 1.0;
    sincos(angle, sine, cosine);
    float2 delta = pixel - pivot;
    // Inverse rotation samples the stationary source at the pendulum position.
    float2 source = pivot + float2(cosine * delta.x + sine * delta.y,
        -sine * delta.x + cosine * delta.y);
    return (source - pixel) * mask;
}

float2 ApplyVillageLanternSwing(float2 uv)
{
    const float2 mapSize = float2(3764.0, 2118.0);
    float2 pixel = float2(uv.x, 1.0 - uv.y) * mapSize;
    float2 offset = float2(0.0, 0.0);
    if (_ForegroundLanterns > 0.5)
    {
        if (pixel.x >= 1004.0 && pixel.x <= 1179.0 && pixel.y >= 1290.0 && pixel.y <= 1454.0)
        {
            offset += LanternSwingOffset(pixel,
                float4(1004, 1290, 1078, 1454), float2(1045, 1290), 0.45);
            offset += LanternSwingOffset(pixel,
                float4(1109, 1290, 1179, 1454), float2(1141, 1290), 0.73);
        }
    }
    else if (pixel.x >= 1135.0 && pixel.x <= 3071.0 && pixel.y >= 1185.0 && pixel.y <= 1432.0)
    {
    // Left village path: two small lanterns.
    offset += LanternSwingOffset(pixel, float4(1135, 1185, 1165, 1273), float2(1150, 1185), 0.0);
    offset += LanternSwingOffset(pixel, float4(1174, 1185, 1211, 1273), float2(1191, 1185), 0.28);
    // Left end of the stone bridge.
    offset += LanternSwingOffset(pixel, float4(1489, 1310, 1530, 1414), float2(1512, 1310), 1.3);
    offset += LanternSwingOffset(pixel, float4(1542, 1310, 1583, 1414), float2(1564, 1310), 1.56);
    // Right end of the stone bridge.
    offset += LanternSwingOffset(pixel, float4(1938, 1310, 1973, 1414), float2(1958, 1310), 2.6);
    offset += LanternSwingOffset(pixel, float4(1986, 1310, 2025, 1414), float2(2008, 1310), 2.86);
    // Waterside terrace, beside the waterwheel.
    offset += LanternSwingOffset(pixel, float4(2953, 1312, 3001, 1432), float2(2981, 1312), 0.75);
    offset += LanternSwingOffset(pixel, float4(3020, 1312, 3071, 1432), float2(3045, 1312), 1.03);
    }
    return uv + float2(offset.x, -offset.y) / mapSize;
}

#endif
