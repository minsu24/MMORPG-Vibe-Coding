#ifndef TEMPLE_GATE_SKY_PLATFORM_WIND
#define TEMPLE_GATE_SKY_PLATFORM_WIND
float SkyHangingMask(float2 p, float4 box)
{
    float2 low=smoothstep(box.xy,box.xy+float2(7,5),p);
    float2 high=1-smoothstep(box.zw-float2(7,8),box.zw,p);
    return low.x*low.y*high.x*high.y;
}
float2 SkyPendantOffset(float2 p,float4 box,float2 pivot,float phase,float time)
{
    float mask=SkyHangingMask(p,box);
    float angle=radians(_SwingDegrees)*(sin(time+phase)+.15*sin(time*.63+phase));
    float sn=sin(angle),cs=cos(angle);
    float2 d=p-pivot;
    return (float2(cs*d.x+sn*d.y,-sn*d.x+cs*d.y)-d)*mask;
}
float2 SkyClothOffset(float2 p,float4 box,float phase,float strength,float time)
{
    float h=saturate((p.y-box.y)/(box.w-box.y));
    float wave=sin(time+phase-h*3.1)+.22*sin(time*1.63+phase-h*5.2);
    return float2(wave,.12*sin(time*1.2+phase-h*4))*strength*h*h*SkyHangingMask(p,box);
}
float2 ApplyTempleGateSkyPlatformWind(float2 uv)
{
    // Coordinates refer to the original PNG, independent of import resolution.
    const float2 atlas=float2(2172,724);
    float2 p=float2(uv.x,1-uv.y)*atlas;
    float time=(_PreviewTime>=0 ? _PreviewTime : _Time.y)*_FlagSpeed+_FlagPhase;
    float2 offset=0;
    // Large roof: chain stays attached to the wooden underside.
    offset+=SkyPendantOffset(p,float4(125,222,229,407),float2(176,223),.2,time);
    offset+=SkyClothOffset(p,float4(292,187,432,408),1.1,_FlagStrength,time);
    offset+=SkyClothOffset(p,float4(865,183,950,407),3.1,_FlagStrength*.8,time);
    // Small roof: rotate the hanging lanterns around their upper hooks.
    offset+=SkyPendantOffset(p,float4(100,551,199,716),float2(150,553),.8,time);
    offset+=SkyPendantOffset(p,float4(600,551,701,716),float2(651,553),2.6,time);
    // Rock platform: only the trailing foliage, below the load-bearing stone.
    offset+=SkyClothOffset(p,float4(1410,219,1475,353),2.1,_FlagStrength*.4,time);
    offset+=SkyClothOffset(p,float4(1755,252,1820,347),4.2,_FlagStrength*.35,time);
    // Keep all sampling within this sprite so adjacent atlas items cannot bleed.
    float2 samplePixel=clamp(p+offset,_FlagRegion.xy+.5,_FlagRegion.zw-.5);
    return float2(samplePixel.x/atlas.x,1-samplePixel.y/atlas.y);
}
#endif
