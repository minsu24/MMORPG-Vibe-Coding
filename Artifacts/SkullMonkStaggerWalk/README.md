# Skull Monk stagger walk

The ten currently selected walk sprites, their order, pixels, pivots and scale are preserved. Their times are now 0, 0.28, 0.53, 0.75, 0.94, 1.04, 1.13, 1.22, 1.34, 1.50 seconds in a 1.7-second loop. The slower leg drags, then the other leg catches up quickly.

Real horizontal movement follows the normalized walk-animation phase. It eases from 0.65 to 0.50 of normal speed while dragging, then surges up to 2.25 on the catch-up step. The numerical cycle average is 1.00014, preserving the overall travel speed. Both directions use the same phase. Idle, attack and knockback do not use this modulation. The base enemy hook defaults to 1 for all other enemies.

`walk-preview.png` is a lossless looping 30 FPS APNG rendered in Unity. The preview uses a constant-speed tracking camera so the hesitation and catch-up are visible. `WalkBefore.anim` preserves the prior clip, `timing.json` contains actual frame keys, and `runtime.json` contains live checks.

Validation: 18 live Rigidbody2D velocity samples across both directions match the animation-phase multiplier, vertical velocity is preserved, mean speed is effectively unchanged, and the attack remains stationary. Play Mode was stopped and the temporary additive Center scene closed afterward. The character artwork was not regenerated and no new walking sounds were added.
