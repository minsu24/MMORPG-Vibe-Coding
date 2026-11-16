# Human-faced bird

Prefab: `Assets/_Project/Prefabs/Enemy/HumanFacedBird.prefab`

Uses the supplied idle (8 frames), walking (8), claw attack (8), and death (7) artwork. The source PNGs are unchanged. Import settings now preserve full-resolution frames, foot-aligned pivots, and tight meshes that exclude disconnected fragments from neighboring spread-wing poses. New `HumanFacedBird*` clips and controller sit beside the existing animation assets.

The prefab follows the Ginseng ordinary-enemy structure: Enemy layer, SpriteRenderer, Rigidbody2D, CapsuleCollider2D, Animator, the common EnemyController behavior, damage text, currency rewards and EnemyLootDrop. Ginseng-specific drops are removed. It is ready to place in a scene; no map spawn or scene placement is added.

The bird walks/chases on the ground, rises for a claw attack, hits once at the extended-talons frame (0.5 seconds), then descends and restores gravity. Body contact deals no damage. Knockback, death, disable, a lost player, and the timeout fallback cancel an attack and restore gravity. Physics stays active during flight so the body still collides with terrain.

Inspector tuning on `MON_HumanFacedBird`:

- Detection and Patrol: inherited circle/rectangle range, vertical detection extent and patrol settings.
- Airborne claw attack: attack range, vertical range, cooldown, lift height, duration, hitbox offset and size.
- Ground support: terrain mask and ground-check distance.
- Common stats and rewards: inherited HP, attack power, speed, EXP and currency.

Initial stats match the ordinary-enemy template (60 HP, 10 attack, 1.4 speed, 25 EXP, 5 currency) and are editable. The default attack is 1 second; its animation event is tied to the supplied pose sequence.

Validation: `HumanFacedBirdValidation.Run()` in Play mode with `SessionState` key `EasternFantasy.SaveTestDirectory` set to isolated storage. It verifies detection/chase, rise before damage, animation-event timing, one hit per attack, both facing directions, landing, range limits, knockback/disable/death cancellation and complete death playback. See `Validation.txt` and the Takeoff, Claw, Landing and Death PNGs. The final run completed without Unity console errors. The real save directory was not used, and the override was removed after testing.

Appearance: Idle/death source art faces right and is flipped in those animation clips; walk/attack source art faces left. All actions therefore follow the common enemy's facing direction. Each sheet uses its own PPU so its initial upright pose is 1.97 units high. Pose changes retain their natural proportions. The prefab scale is 1.3, making the normalized standing height 2.561 world units and scaling its collider and claw hitbox together. Source PNGs are unchanged.

Builder: `Eastern Fantasy > Monsters > Setup Human Faced Bird`. Existing gameplay tuning is retained on subsequent runs; appearance is restored to the normalized facing and 1.3 scale.
