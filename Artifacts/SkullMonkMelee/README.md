# Skull Monk basic melee

Applied to `Assets/_Project/Scenes/OldTemple/OldTemple - Center.unity`, existing `Skull` object.

- Original idle and walk sprite sequences preserved.
- Clean attack sheet: `Assets/_Project/Art/Old Temple/Monster/SkullMonk/SkullMonkMelee_Clean.png`.
- Effect sheet: `Assets/_Project/Art/Old Temple/Monster/SkullMonk/SkullMonkMelee_Effect.png`, unchanged pixels from the supplied effect with four complete poses instead of automatically sliced particle fragments.
- Attack: 13 keys at 10 FPS, seven forward poses followed by recovery. Thrust event at 0.5 seconds, finish at 1.29 seconds.
- Separate tight sprite outlines exclude neighboring poses. Sprite pivots keep the feet level with idle.
- Initial editable combat values: 3000 HP, 30 attack, 1.5 movement speed, 14 detection range, 3.8 melee initiation range, 1.8 seconds cooldown after recovery. Contact damage is zero.
- Boss uses the existing persistent boss defeat system with encounter ID `Chapter1.SkullMonk`.

Validation: live Unity compilation succeeded; temporary player tests passed facing both left and right. HP fell from 100 to 70.87379 after defense, repeated thrust callbacks caused no second damage, effect was present, and attack finished. Test player was destroyed and Play Mode exited. See `runtime-validation.json` and `thrust.png`. Final runtime console had no errors or warnings.

The active scene has no player placed in it, so testing used a temporary instance of the existing Player prefab. Normal gameplay requires entry with the game's player.

Image editing used the built-in image generation tool. Prompt:

> Edit this exact game animation sprite sheet for production use. Remove ONLY the white/light halo outlines around the seven skeleton monk poses and remove the pale checkerboard background to true transparent alpha. Preserve all seven original poses, positions, spacing, dark ink outlines, skull/bone colors, clothing, staff shapes and sizes exactly. Do not redesign, redraw, rearrange, crop, add, or omit any pose. Keep canvas aspect 3:1 and full seven poses. This is precise background extraction and white fringe removal, not new artwork.
