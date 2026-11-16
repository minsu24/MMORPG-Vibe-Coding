# TempleRoad decoration sheet

Updated asset: Assets/_Project/Art/Old Temple/Map/TempleRoad/절로 향하는 길 장식.png

The built-in image_gen tool produced isolated versions of the supplied props. The generated output was repacked into 20 transparent cells (4 columns x 5 rows), 768x768 per cell, 3072x3840 total. Every cell has a verified fully transparent 64px perimeter. Alpha-weighted resampling removed near-invisible alpha<=8 matte residue. The original two sprite IDs were retained for the first two props; all 20 props have individual names and non-overlapping SpriteRects. Existing asset GUID and 100 PPU are retained. Source artwork and import metadata are backed up as Original.png and Original.png.meta.txt in this directory.

## Exact built-in edit prompt

Use case: precise-object-edit / sprite-sheet layout cleanup.
Edit the supplied existing Korean fantasy roadside decoration asset sheet into an organized, NON-OVERLAPPING transparent sprite atlas. Preserve the identity, weathered painted texture, muted gray brown stone and wood, faded burgundy cloth, moss, amber lantern light, ivory grasses/flowers, red maple leaves and white cloud motifs of the reference. Do not redesign or add types of props. Remove every opaque or translucent rectangular background, haze backdrop, color-gradient backdrop and unwanted connecting pixels: true transparent alpha between props.
Arrange exactly TWENTY separate sprite cells in a strict FOUR COLUMNS by FIVE ROWS regular grid, reading left to right:
Row 1: (1) complete tall wooden banner pole with its faded red floral banner and tassels; (2) mossy wooden multi-arrow signpost; (3) crooked wooden post with one hanging amber lantern and paper tag; (4) bamboo post with the two round paper lanterns and red tassels.
Row 2: (5) lit stone lantern; (6) tall upright floral-carved gravestone with moss at base; (7) short upright floral-carved gravestone with moss at base; (8) half-buried broken wagon wheel with vines and small rocks.
Row 3: (9) broken round earthenware jar with floral carving, red ivy and rocks; (10) leaning fallen floral-carved gravestone and small rocks; (11) stacked stone cairn; (12) complete clump of pale pampas/reed grass.
Row 4: (13) red maple shrub; (14) complete clump of white spider lilies with leaves; (15) small mossy rock cluster; (16) small scattered red maple leaf group.
Row 5: four distinct separate white painted ornamental cloud-wisp groups, one group in each cell. Each can be used independently.
Every individual cell must have at least 48 pixels of totally transparent padding on all sides and the FULL silhouette of its one prop. Nothing may touch or cross its cell border, touch any other asset, overlap, get clipped, or connect via leaves, branches, glow, roots or background pixels to another cell. Entire tall posts and grass blades must fit inside their own cell. Keep glow local to each lantern; do not add drop shadows on a background. No labels, no text, no visible grid, no borders. Keep existing floral carvings, do not introduce readable writing.
Output a portrait atlas with width:height 4:5, ideally 3072x3840 or 2048x2560, on a real TRANSPARENT background. Crisp detailed 2D painted game sprites, consistent art style and sharp detail.

