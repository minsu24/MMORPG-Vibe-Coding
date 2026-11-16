# Side-view perspective revision

Current animation: `depth-ripple-preview.png`.

The ripple expands from the planted staff horizontally and in foreground/background depth. The prior hard depth cap is removed. The near half opens downward at 14% of radius; the far half opens upward at 10.5% of radius. The shallow, asymmetric projection keeps the side-view perspective while visibly spreading over the deeper map surface.

Each circular crest is split into two connected 97-point semicircles. The dimmer, thinner far semicircle renders behind the character; the near semicircle renders in front of the feet. This uses 2D sorting order rather than moving geometry far along world Z, which would cause camera clipping in this scene.

The five-bow sequence, smoke, map-wide horizontal reach, one-hit-per-pulse guard, jumping evasion and exact 40% maximum-health damage are preserved. Live validation passed for all of these plus continued vertical growth and front/back sorting. See updated `runtime.json`. Character artwork and animation scale are unchanged. Vertical screen expansion represents ground depth and does not raise the 0.45-unit damaging band.
