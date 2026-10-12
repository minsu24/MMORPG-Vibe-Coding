# Village texture quality check

Scope: texture import quality only. No scene files were saved.

The five active village background layers already import at 3764 x 2118 in RGBA32. Their settings were retained. The older single-image village background is inactive and was not changed.

Changed `Assets/_Project/Art/tutorialMapImage/마을 발판.png.meta`: default texture compression from Compressed to Uncompressed. The shared platform texture now uses RGBA32 instead of DXT5 at the same 1536 x 1024 size. Sprite IDs, slice rectangles, pivots, filtering, and pixels per unit are unchanged. Existing platform instances share this one texture.

Unity Profiler.GetRuntimeMemorySizeLong reported 3,146,624 bytes before and 12,583,808 bytes after (increase: 9,437,184 bytes, approximately 9 MiB).

Compare `before-1.png` and `after-1.png`: same village camera position, 1920 x 1080, editor camera rendering without overlay UI. This is a subtle compression-quality improvement, not a replacement or upscale of the background artwork. The images differ in 320,313 pixels (15.447%). Pixel difference is not a quality score.

Validation: imported texture format and dimensions checked in Unity; no missing sprites on active scene renderers; metadata diff contains only the default compression setting. No gameplay code changed. No console errors were reported; one existing MCP WebSocket warning was present.

Original metadata is preserved in `VillagePlatforms.before.meta.txt` for rollback.
