#ifndef TEMPLE_GATE_ATMOSPHERE
#define TEMPLE_GATE_ATMOSPHERE
float4 ApplyTempleGateAtmosphere(float4 color, float2 uv)
{
    float height=saturate((uv.y-_AtmosphereRect.y)/max(_AtmosphereRect.w,.0001));
    // Lift lower silhouettes into the same cool mist as the painted scenery.
    // Preserve coverage: solid ledges remain opaque and readable for jumping.
    float haze=_AtmosphereAmount*lerp(1,.45,height);
    color.rgb=lerp(color.rgb,_AtmosphereColor.rgb,haze);
    // Small contact shadow under the hanging prop's fixed upper attachment.
    color.rgb*=1-_AttachmentShade*smoothstep(.78,.98,height);
    return color;
}
#endif
