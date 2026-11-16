#ifndef TEMPLE_ROAD_PLATFORM_ENDS
#define TEMPLE_ROAD_PLATFORM_ENDS

float TempleRoadEndInset(float depth, float side)
{
    float t = saturate(depth / max(_EndRect.w * .54, .01));
    // Keep the walkable upper lip. Lower masonry ends in asymmetric, chipped facets.
    float contour = lerp(0, .10, saturate(t / (.20 + side * .03)));
    contour = lerp(contour, .29 + side * .05, saturate((t - .23) / .10));
    contour = lerp(contour, .20, saturate((t - .33) / (.08 + side * .02)));
    contour = lerp(contour, .44 + side * .04, saturate((t - .43) / .17));
    contour = lerp(contour, .38, saturate((t - .60) / .10));
    contour = lerp(contour, .70 - side * .08, saturate((t - .70) / .30));
    return _EndDepth * max(0, contour) * smoothstep(.015, .16, t);
}

float TempleRoadEndDistance(float2 local)
{
    float depth = max(0, _EndRect.y + _EndRect.w - local.y);
    float left = local.x - _EndRect.x - TempleRoadEndInset(depth, 0);
    float right = _EndRect.x + _EndRect.z - local.x - TempleRoadEndInset(depth, 1);
    return min(left, right);
}

float TempleRoadEndCoverage(float2 local)
{
    float distance = TempleRoadEndDistance(local);
    float aa = max(fwidth(distance), .001);
    return smoothstep(-aa, aa, distance);
}

half4 ApplyTempleRoadPlatformEnds(half4 color, float2 local)
{
    if (_EndDepth <= 0) return color;
    float distance = TempleRoadEndDistance(local);
    color.a *= TempleRoadEndCoverage(local);
    // A narrow stone facet makes the new silhouette read as a solid broken edge.
    float facet = 1 - smoothstep(.015, .09, distance);
    color.rgb *= 1 - facet * .16;
    return color;
}
#endif
