#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace Rushitadel.Editor
{
    public sealed class PlayerArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if((assetPath.StartsWith("Assets/Resources/EnemyArt/") || assetPath.StartsWith("Assets/Resources/EnemyProjectiles/") || assetPath.StartsWith("Assets/Resources/PressureArt/") || assetPath.StartsWith("Assets/Resources/ThemeCards/") || assetPath.StartsWith("Assets/Resources/WebIcons/")) && assetPath.EndsWith(".png"))
            {
                var enemy=(TextureImporter)assetImporter;
                enemy.textureType=TextureImporterType.Sprite;enemy.spriteImportMode=SpriteImportMode.Single;
                enemy.spritePixelsPerUnit=32;enemy.filterMode=FilterMode.Point;enemy.mipmapEnabled=false;
                enemy.alphaIsTransparency=true;enemy.isReadable=true;enemy.textureCompression=TextureImporterCompression.Uncompressed;
                enemy.wrapMode=TextureWrapMode.Clamp;enemy.npotScale=TextureImporterNPOTScale.None;return;
            }
            if(assetPath.StartsWith("Assets/Resources/Battlefields/"))
            {
                var map=(TextureImporter)assetImporter;map.textureType=TextureImporterType.Sprite;map.spriteImportMode=SpriteImportMode.Single;
                map.filterMode=FilterMode.Point;map.mipmapEnabled=false;map.maxTextureSize=2048;
                map.textureCompression=TextureImporterCompression.Uncompressed;map.wrapMode=TextureWrapMode.Repeat;return;
            }
            if ((!assetPath.StartsWith("Assets/Resources/PlayerForms/") && !assetPath.StartsWith("Assets/Resources/ProjectileArt/") && !assetPath.StartsWith("Assets/Resources/BossArt/")) || !assetPath.EndsWith(".png")) return;
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
            if(assetPath.Contains("BossArt")) importer.maxTextureSize=assetPath.EndsWith("Body.png")?512:256;
            if(assetPath.Contains("ProjectileArt")) importer.maxTextureSize=128;
        }
    }
}
#endif
