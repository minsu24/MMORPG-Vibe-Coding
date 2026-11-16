Tool: built-in image_gen, four native-density tile restorations and one floor-boundary pass.
Saved assets (project-bound):
- Assets/_Project/Art/Old Temple/Map/OldTemple - Center/Center_DetailRestored_2.png
- Assets/_Project/Art/Old Temple/Map/OldTemple - Center/Center_DetailRestored_3.png
- Assets/_Project/Art/Old Temple/Map/OldTemple - Center/Center_DetailRestored_5.png
- Assets/_Project/Art/Old Temple/Map/OldTemple - Center/Center_DetailRestored_6.png
- Assets/_Project/Art/Old Temple/Map/OldTemple - Center/Center_FloorContinuityRestored.png

Tile prompt: High fidelity deblurring/restoration of enlarged Korean temple background tiles. Restore small stone paving joints, red leaves, moss, balustrade carving, stairs, roofs and worn wood at 1672x941. Preserve exact framing, geometry, perspective, object placement, stepped terraces, lighting, palette and atmospheric distant mist. No redesign, added objects, text, halos or exaggerated black outlines. Preserve border composition so neighboring tiles remain aligned.
Floor prompt: Restore a continuous sharp 3344x500 panoramic courtyard floor strip across the horizontal tile-boundary blur. Keep paving crack paths, perspective, stepped walls, balustrade, lotus medallion and small lamp positions unchanged. Preserve removed foreground tall lanterns. No new props, characters, dark halos, blurry bands or composition changes.

Implementation: Separate detail overlays preserve the existing background layout. Native sprite cropping trims the generated floor strip letterbox; its UV bounds also control edge fading. Lantern-removal edge blend is reduced from 12% to 2.5% to reduce doubled paving patterns. Background-only restrained shader detail adjustment (0.2) is used on existing base materials; generated detail overlays do not use artificial sharpening. No character, collision, boss-pattern or gameplay changes.
Verification: Rendered left, center and right close views before and after. Shader compilation checked. Existing stepped terrace and foreground lantern removals retained.
