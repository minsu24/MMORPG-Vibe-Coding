# TempleRoad seam repair

Used the built-in image_gen image-editing tool for both boundary strips.

Saved assets:

- Assets/_Project/Art/Old Temple/Map/TempleRoad/TempleRoad_Background_1_SeamFixed.png
- Assets/_Project/Art/Old Temple/Map/TempleRoad/TempleRoad_Background_2_SeamFixed.png
- Assets/_Project/Art/Old Temple/Map/TempleRoad/TempleRoad_Background_3_SeamFixed.png

Scene: Assets/_Project/Scenes/OldTemple/TempleRoad.unity

Original source images and a scene backup are retained under Artifacts/TempleRoadSeams/Originals and TempleRoad_Before.unity.

The two generated strips were resampled to the source crop and integrated within 224 pixels of the joined edges. Original artwork outside those strips is pixel-identical. The scene now references the repaired assets. Both join gaps were closed; each panel remains 1920x1080 at 100 PPU.

## Exact prompt used for both joins

Use case: precise-object-edit. Repair an existing 2D side-scrolling game background seam. The input is a 768x1080 crop straddling the join between two landscape panels, not a whole new scene. There is a badly smeared vertical band through the middle, with ugly horizontal stretched scanlines. Reconstruct ONLY this central seam band (approximately x=200 through x=568) with crisp, detailed painted mountain rock contours, pine tree branches, red leaves, distant clouds, grass, and naturally textured mist connecting the intact scenery on both sides. Eliminate every horizontal streak, smear, repeated edge pixel, blurred stripe and visible join. Keep normal atmospheric mist soft but rock edges, clouds, trees and leaves as sharp and textured as the untouched areas. Match the existing muted blue gray Korean fantasy night landscape painting exactly: same palette, contrast, scale and brushwork. Preserve the intact leftmost 180 and rightmost 180 pixels as close to pixel-exact as possible. Keep every existing landmark and composition outside the damaged band, including ghost flames, waterfall, stone monuments/pavilion if visible. Do not add moons, characters, buildings, borders, text, new lights or other landmarks. The result must read as ONE continuous scene at the middle boundary, with uninterrupted natural shapes across it, not two images blended with fog. Preserve the full original canvas, framing and proportions. Output exactly 768x1080 if possible, opaque background.

