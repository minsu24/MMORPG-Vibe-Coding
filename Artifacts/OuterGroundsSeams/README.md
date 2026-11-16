# Outer Grounds background connections

Scene: `Assets/_Project/Scenes/OldTemple/OldTemple - Outer Grounds.unity`

The three Background/Square sprite renderers now use sibling `OuterGrounds_Background_*_Seamless.png` assets and scene-specific `OuterGrounds_Harmony_*` materials. The source illustrations remain intact. The three panels are 1672x941 pixels at 100 PPU, with adjacent centers 16.72 world units apart and a common vertical position.

The built-in imagegen editor reconstructed the two 1024x941 boundary crops. The generated crops were resampled to their input dimensions and integrated with a 192-pixel smooth transition at their outside edges. At most 512 pixels on either side of each join are changed. All pixels outside these regions are identical to the source images, retaining the central moon, temple buildings, and entrance gate.

`Before.unity` preserves the scene before this task. `Before_Rendered.png` and `After_Rendered.png` show Unity-rendered comparisons. `Join1_After.png` and `Join2_After.png` show the repaired connections in detail. `Validation.txt` records alignment, protected-pixel comparisons, and border differences. Only the three background SpriteRenderers and two background Transforms changed in the scene; no scene objects were added or removed.

Imagegen prompt: repair only the central join in each attached crop; connect tiled eaves, decorated wall beams, cracked plaster, mountain contours, cloud shapes, rocks, and mist; preserve the original detailed painted style, slate blue-gray night palette, red foliage, and warm lanterns; preserve the outside 192 pixels as closely as possible; add no new landmarks, moons, characters, or text; avoid blurred vertical stripes, stretched pixels, and double exposure.
