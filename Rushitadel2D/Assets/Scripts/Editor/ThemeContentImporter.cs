using UnityEditor;
using UnityEngine;
namespace Rushitadel.Editor
{
    public sealed class ThemeContentImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Resources/ThemeContent/"))return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;
            importer.isReadable=true;importer.mipmapEnabled=false;
            importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;
            importer.wrapMode=TextureWrapMode.Clamp;
        }
    }
}
