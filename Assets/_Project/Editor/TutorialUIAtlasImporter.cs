using UnityEditor;
using UnityEngine;

namespace EasternFantasy.EditorTools
{
    public sealed class TutorialUIAtlasImporter : AssetPostprocessor
    {
        public const string AtlasPath = "Assets/_Project/Resources/UI/Tutorial/TutorialUI.png";
        public override uint GetVersion() => 1;
        private void OnPreprocessTexture()
        {
            if (assetPath != AtlasPath) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
#pragma warning disable CS0618
            importer.spritesheet = new[] {
                Region("Panel", 24, 28, 1400, 596),
                Region("DarkButton", 52, 650, 480, 140),
                Region("LightButton", 540, 650, 480, 140),
                Region("Counter", 1085, 675, 320, 100),
                Region("Divider", 35, 806, 1380, 78),
                Region("HintBar", 40, 915, 1370, 130) };
#pragma warning restore CS0618
        }
        private static SpriteMetaData Region(string name, float x, float top, float width, float height)
            => new SpriteMetaData { name = name, rect = new Rect(x, 1086f-top-height, width, height),
                pivot = Vector2.one * .5f, alignment = (int)SpriteAlignment.Center };
    }
}
