# Skull Monk five-wave pattern

Implemented on the existing Skull in `Assets/_Project/Scenes/OldTemple/OldTemple - Center.unity`.

The supplied wave attack 1 and 2 sheets remain unchanged. Dedicated copies are sliced into eight poses each, with consistent character scale, grounded feet and a fixed planted staff anchor during bowing. The supplied staff effect is sliced into four trimmed ground-smoke poses. No character redraw or image generation was used.

Sequence:

1. `SkullMonkWavePlant`: raise and plant the staff. The last pose at 1.00s emits ground smoke at the staff foot.
2. `SkullMonkWavePulse`: a 1.00s bow cycle, releasing one bidirectional ground pulse at its lowest pose (0.40s). Repeat exactly five times; duplicate events cannot add pulses.
3. `SkullMonkWaveRecover`: lift the staff and return naturally to idle. Already-released waves continue to the map edges during recovery.

The pattern first becomes available after five seconds and has a twelve-second cooldown after completion. It is selected while the boss detects a living player and is on the floor; existing melee and stagger walking remain available between rituals. Once committed, normal knockback does not interrupt the five bows. Boss defeat or disabling cancels active waves.

Ground waves are generated in Unity using layered LineRenderers, with no external wave image. Each pulse is now one continuous circular ripple centered on the planted staff, expanding at 12 world units/second. A ground-plane perspective compresses its vertical axis to 22% of its horizontal axis. A soft glow and a fainter inner ring follow the main crest, like ripples after a drop hits water. The center remains fixed as both axes grow. Its two intersections with the ground advance to the scene's CameraCollider boundaries. The damage band is 0.45 world units above the floor; jumping with feet above this band evades it. Swept overlap queries cover movement between physics frames. Each pulse shares one hit set across both sides, preventing double hits from the same pulse.

Successful hits remove exactly 40% of the player's maximum health, without armor or shield scaling. Existing immunity/parry checks, damage notifications, combat state and death events remain intact. No new hit sounds were added.

Successful nonlethal wave hits also call the player's existing ApplyKnockback method, directed away from the staff. This reuses normal knockback, input lock, sprite flashing and the configured post-hit invincibility duration. Immune/parried hits do not trigger a new hit reaction. Live checks passed for both directions and confirmed that a separate following wave cannot deal damage while this invincibility is active.

Verification passed in live Play Mode with deterministic animation/physics stepping: staff planting and supplied smoke, five release events, duplicate-event guard, idle recovery, left/right ground hits of exactly 40%, no duplicate damage, jumping evasion, both endpoint cleanup, immunity and lethal damage death notification. See `runtime.json`.

`ripple-preview.png` is the current looping APNG showing all five expanding circular ripples and recovery. It replaces the earlier two-front visual shown in `wave-preview.png`. This is a close-up presentation; gameplay uses the actual Center scene bounds. `plant-smoke.png` and `wave.png` are stills. Source character pixels and resolution are preserved in all dedicated copies. Play Mode was stopped, temporary players removed, and the original active editor scene restored. The updated runtime test additionally verifies the fixed center and the full 192-point closed circular crest.

The project had a pre-existing editor setup error for missing `PlayerIdle8Frames.png`; additive test scenes also produced duplicate EventSystem/AudioListener notices. Those unrelated systems were not changed.
