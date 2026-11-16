# Four staff-ring pattern

Added to the existing Skull in `Assets/_Project/Scenes/OldTemple/OldTemple - Center.unity`.

The four supplied sheets are copied unchanged as `SkullMonkRingCharge.png`, `SkullMonkRingGesture.png`, `SkullMonkRingSpin.png` and `SkullMonkRingFlight.png` under the existing SkullMonk art folder. Sprite rectangles, outlines and pivots are edited for the animation; character pixels and resolution are preserved. No image generation or character redraw was used.

The gesture sheet's fourth frame (zero-based index 3) is excluded from lift, holding and recovery clips because of the extra hand.

Sequence:

1. Play the eight charge poses. The last pose shows the supplied four baked rings around the staff.
2. At the transition to the second sheet, replace those four rings with four independent spinning instances at matching positions.
3. When the valid raised-hand pose appears, move the same ring instances into a four-ring overhead formation over 0.65 seconds.
4. Maintain the one-hand prayer pose while launching one ring every 0.55 seconds, after a short 0.25-second hold. Exactly four instances detach and fly.
5. Each ring steers toward the player only for its first 0.4 seconds, with a limited turning rate. It then locks its heading and continues straight. Swept circle casts detect player and wall/floor impacts without tunneling. Flight uses frames 0–5 of the supplied firing effect; frames 6–7 provide impact/dissipation.
6. After the fourth launch, lower the hand and return to the existing idle pose. Already-fired rings continue independently; boss defeat or disabling cancels them.

The pattern becomes ready after nine seconds and has a fourteen-second cooldown after completion. When both special patterns are ready, the earlier-ready one is selected; existing melee, walking and five-wave attacks remain available. Ring damage uses the boss's existing attack power and normal player defense/immunity handling. No hit sounds were added.

Verification passed in live Unity Play Mode using deterministic animation and projectile stepping: excluded frame absent, four replacement rings, overhead lift, sequential counts 1–4 in both directions, bounded early tracking, fixed direction after cutoff even when the target reverses, swept hit detection, single damage per projectile, idle recovery and cleanup on owner disable. See `runtime.json`.

`ring-pattern-preview.png` is a looping APNG of the full sequence with a jumping player for illustration. `charged.png`, `overhead.png` and `launch.png` are stills. The preview uses the actual sprite sequences and projectile implementation. Play Mode was stopped, temporary player objects removed and the original active editor scene restored.

The project had an existing setup error for missing `PlayerIdle8Frames.png`. Additive test scenes also produced duplicate EventSystem/AudioListener messages; the temporary Center scene was closed after validation. Those unrelated systems were not changed.
