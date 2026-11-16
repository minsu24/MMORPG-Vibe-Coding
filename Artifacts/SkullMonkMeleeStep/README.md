# Step-forward melee revision

Uses the supplied `해골승려 기본공격2안.png`, copied unchanged as `Assets/_Project/Art/Old Temple/Monster/SkullMonk/SkullMonkMelee_StepForward.png`. Source and imported copy SHA256: `237A76E6FA4085E1B189D2A404BFAF874D4C23D93B35F6D6CFA74793EAFC6ABD`.

Eight poses are sliced across the top row and then the bottom row. Each pivot anchors the rear foot at the idle rear foot position: x=-0.68, ground y=-2.54624271. Front foot lift and extension are preserved. The attack accelerates through the planted step into the thrust, holds the extension briefly, and recovers in 1.25 seconds.

Thrust and one-hit damage event occur at 0.57 seconds. The existing four effect poses are enlarged to surround the shaft, start at x=2.7/y=0.45 in the art's facing direction, and move 1.25 units forward over their lifetime. The shaft remains visible through the effect.

Validation: rendered idle and all eight attack poses; verified rear foot anchors numerically; live Unity tests passed for left and right facing, damage applied once, and attack completed. Forward effect travel was observed in the first live probe. The second same-frame probe reused a deferred-destruction object in its measurement, so its stored position is not a separate travel verification. Temporary test player was removed and Play Mode stopped. Additive test scenes produced duplicate AudioListener/EventSystem warnings; the temporary additive Center scene was closed afterward to restore the original active editor scene.

See `pose-7.png` for the extended thrust, `heel-alignment.json` for foot alignment, and `runtime-validation.json` for combat checks.
