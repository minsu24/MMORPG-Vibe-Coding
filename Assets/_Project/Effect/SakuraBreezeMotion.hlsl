#ifndef EASTERN_FANTASY_SAKURA_BREEZE_MOTION
#define EASTERN_FANTASY_SAKURA_BREEZE_MOTION

// Shared by the standalone Sakura and the cherry tree inside village prop 7.
float2 SakuraBreezeOffset(float2 positionOS, float height, float strength, float time)
{
    float weight = height * height * (3.0 - 2.0 * height);
    float breeze = sin(time) * 0.72 + sin(time * 0.61 + 1.7) * 0.28;
    float branch = sin(time * 1.83 + positionOS.x * 2.4 + height * 1.6);
    float2 offset;
    offset.x = strength * weight * (breeze + branch * height * 0.18);
    offset.y = strength * weight * height * sin(time * 1.13 + positionOS.x * 2.0) * 0.12;
    return offset;
}

#endif
