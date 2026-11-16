# OldTemple Center background continuity

Scene: `Assets/_Project/Scenes/OldTemple/OldTemple - Center.unity`.

Six 1672x941 panels form a 3-column by 2-row background. New `Center_Background_*_Seamless.png` siblings and scene-specific `Center_Harmony_*` materials are applied to the existing six SpriteRenderers. Original PNGs and other maps' materials remain intact. The lower row is aligned exactly 9.41 units below the upper row, removing the previous 0.07-unit overlap. Horizontal spacing is 16.72 units.

The built-in imagegen editor repaired three horizontal-boundary crops and two full-height vertical-boundary crops, including both four-panel intersections. The final first vertical join uses `Vertical1_Clean.png`, with a local roof correction from `FirstRoofCleanup_Generated.png`; the final second join uses the user-approved `Vertical2_Conservative.png`. Earlier wide/warped architectural drafts are not used. The roof wings now remain distinct and the terraces use upright masonry returns with level horizontal rails.

Generated patches were resampled to their source-crop dimensions and integrated at the boundaries. The original central lotus medallion is protected by an ellipse mask. The original central temple facade, entrance and Buddha remain pixel-identical, as do all pixels outside the repair regions. `Validation.txt` records these checks, border comparisons, exact alignment and material isolation.

`Before.unity` is the pre-edit scene backup. `Before_Rendered.png` and `After_Rendered.png` are Unity-rendered overviews. `UpperJoin1_Final.png` shows the cleaned first roof junction. Only six SpriteRenderer blocks and the three lower-row Transforms changed in the scene; no scene objects were added or removed.
