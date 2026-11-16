# TempleRoad unified night tone and single moon

Used built-in image_gen to remove the moons from the outer two panels. Only localized sky patches were composited back; the middle panel remains pixel-identical to the seam-repaired source. All repaired edge strips are unchanged.

Saved scene: Assets/_Project/Scenes/OldTemple/TempleRoad.unity

Saved background assets:

- Assets/_Project/Art/Old Temple/Map/TempleRoad/TempleRoad_Background_1_MoonUnified.png
- Assets/_Project/Art/Old Temple/Map/TempleRoad/TempleRoad_Background_2_MoonUnified.png
- Assets/_Project/Art/Old Temple/Map/TempleRoad/TempleRoad_Background_3_MoonUnified.png

Matching grading materials: Assets/_Project/Art/Old Temple/Map/TempleRoad/TempleRoad_NightHarmony_1.mat through TempleRoad_NightHarmony_3.mat. All use the existing Village Harmony shader with contrast 1.035, saturation 0.92, brightness 0.96, and tint (0.96,0.98,1,1). No softening. Per-panel textures are explicitly assigned. Existing 2D lighting behavior is retained.

## Exact prompt for both outer moon crops

Use case: precise-object-edit. This image is an exact crop from an existing painted Korean fantasy side-scrolling game background. REMOVE the large bright round MOON entirely, along with its circular glow and lunar surface. Reconstruct the occupied area with the same blue-gray night sky and naturally flowing clouds that connect seamlessly to the surrounding existing cloud shapes. There must be NO moon, NO circular bright disc, NO round glowing halo, NO sun, NO stars or new light source anywhere in the result. Preserve the exact image canvas, original proportions and composition. Preserve mountains, pine tree silhouettes, pagoda, temple roof, and all other scenery if visible; change only the moon and immediately surrounding sky. Match the existing muted colors, brightness, cloud linework, textured painterly illustration style. Keep the outer edges pixel-identical if possible so this crop can be composited back into its source. Retain the original landscape aspect ratio of 600x390 (20:13), output that size if possible. No text, no border, no extra objects.

