#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace Rushitadel.Editor
{
    public sealed class PlayerArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if ((!assetPath.StartsWith("Assets/Resources/PlayerForms/") && !assetPath.StartsWith("Assets/Resources/ProjectileArt/")) || !assetPath.EndsWith(".png")) return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.isReadable=true;
            importer.filterMode=FilterMode.Point;
            importer.mipmapEnabled=false;
            importer.alphaIsTransparency=true;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.wrapMode=TextureWrapMode.Clamp;
        }
    }
}
#endif
