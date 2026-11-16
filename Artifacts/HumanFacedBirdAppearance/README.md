# Human-faced bird appearance correction

The four first-pose sprites now share a 1.97-unit height before prefab scaling. Per-sheet PPU preserves natural posture changes across subsequent frames instead of resizing each pose separately. The root prefab is enlarged exactly 30% to scale 1.3, also scaling the collider, damage-text anchor and claw hitbox.

Idle/death clips set SpriteRenderer.flipX=true; walk/attack set it false. The common root-facing convention therefore points every animation at the same target. Facing was visually checked with a URP 2D unlit preview, and the actual lit prefab was checked in Play mode.

`After.png` shows idle, walk, attack and death first poses from left to right, all facing right. `Validation.txt` records appearance measurements. The complete appearance and combat replay passed in `Artifacts/HumanFacedBird/Validation.txt` with no Unity console errors.

The old prefab is retained in `Before.prefab`. Original PNG artwork was not changed.
