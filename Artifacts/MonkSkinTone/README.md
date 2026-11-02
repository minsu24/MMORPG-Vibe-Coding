# Monk straight punch skin correction

Final asset: `Assets/_Project/Art/Character/Monk/정권 찌르기 비율 수정.png`

Used the built-in image generation editor to create a brighter skin color reference. Its generated sheet changed dimensions and placement slightly, so only its measured skin color difference was transferred to the original using Unity texture processing and spatial/color masks. The generated geometry was not used.

Original.png is the untouched backup. GeneratedColorReference.png is the generated reference. Corrected.png is the final integrated asset.

Validation: final dimensions 2170 x 725; 62,850 RGB pixels adjusted; zero alpha changes; metadata SHA256 unchanged (E81184566ECA45C8DBA6F5C41CF8B56F6B0939DB4BE8F1DC92F32E661F10A827); all 6 imported sprite IDs and rectangles preserved; all 7 animation sprite keys resolve. Visual inspection completed. No gameplay code changed.

## Built-in image editor prompt

EDIT TARGET: the FIRST provided PNG only, a production Unity 2D sprite sheet of a chibi monk performing a straight punching attack. The SECOND PNG is a SKIN COLOR REFERENCE ONLY (idle animation); do not output the second image. Make a restrained skin-tone-only color correction: gently brighten the bald scalp, face, ears, exposed fists/hands, and exposed feet in all 8 frames to match the lighter warm peach skin of the idle reference. Lift skin midtones approximately 10%, reduce the overly dark reddish skin shading slightly, and retain shading, eyes, eyebrows, red forehead marks and crisp dark outlines. This is NOT a redraw or animation redesign: preserve every original pose, expression, silhouette, clothing, jewelry, belt, red cloth, linework, and all non-skin colors. Preserve EXACT original sheet canvas 2170 x 725 pixels, all eight original sprites at their EXACT original coordinates, sizes, pivots, framing and spacing; preserve all transparent margins. Do NOT enlarge/reposition/reframe any sprite or add extra sprites. Preserve transparent background and alpha. Output ONE corrected complete horizontal 8-frame sprite sheet only, suitable as a drop-in replacement for the first PNG.
