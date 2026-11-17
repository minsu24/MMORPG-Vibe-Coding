using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

var art = "Assets/_Project/Art/Old Temple/Monster/SkullMonk/";
string[] names = { "SkullMonkPrayerRise_HD.png", "SkullMonkPrayerPeak_HD.png", "SkullMonkPrayerFade_HD.png" };
int[] counts = { 4, 4, 5 };
var rows = new System.Collections.Generic.List<object>();
for (int i = 0; i < names.Length; i++)
{
    var path = art + names[i];
    var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
    if (sprites.Length != counts[i]) throw new Exception("Incorrect frame count in " + names[i]);
    if (sprites.Any(s => s.rect.height < 800f)) throw new Exception("Insufficient native frame height");
    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
    if (importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled)
        throw new Exception("Unexpected texture settings");
    rows.Add(new { asset = names[i], frames = sprites.Length, nativeFrameHeight = sprites[0].rect.height });
}
return new { passed = true, playing = EditorApplication.isPlaying, compiling = EditorApplication.isCompiling, assets = rows };
