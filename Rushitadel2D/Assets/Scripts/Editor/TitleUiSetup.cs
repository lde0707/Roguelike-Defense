#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Rushitadel.Editor
{
    [InitializeOnLoad]
    public static class TitleUiSetup
    {
        static TitleUiSetup()
        {
            EditorApplication.delayCall += () => {
                if (!Application.isBatchMode && !EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Title.unity");
            };
        }
        [MenuItem("Rushitadel/Prepare Title UI Assets")]
        public static void Prepare()
        {
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            string output = "Assets/Resources/TitleUI";
            Directory.CreateDirectory(output);
            var sheet = Read(Path.Combine(project, "output/title-implementation-20261006/button-states.png"));
            string[] names = {"Start", "Continue", "Settings", "Quit"}, states = {"Normal", "Hover", "Pressed"};
            // Pixel bounds measured on the generated sheet preview (2048 x 683).
            int[] xs = {94, 743, 1391}, ys = {74, 216, 357, 500};
            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 3; col++)
                    Crop(sheet, xs[col], ys[row], 562, 110, 2048, 683, output + "/" + names[row] + "_" + states[col] + ".png", false);
            UnityEngine.Object.DestroyImmediate(sheet);
            var title = Read(Path.Combine(project, "output/ui-title-backgrounds-20261005/13_matte_classic_pixel_ui.png"));
            Crop(title, 567, 151, 544, 87, 1672, 941, output + "/Logo.png", true);
            UnityEngine.Object.DestroyImmediate(title);
            AssetDatabase.Refresh();
            foreach (string path in Directory.GetFiles(output, "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true; importer.maxTextureSize = 2048; importer.SaveAndReimport();
            }
            EditorBuildSettings.scenes = new[] {new EditorBuildSettingsScene("Assets/Scenes/Title.unity", true), new EditorBuildSettingsScene("Assets/Scenes/inGame.unity", true)};
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Title.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("TITLE_UI_ASSETS_READY");
        }
        private static Texture2D Read(string path)
        {
            var texture = new Texture2D(2,2); texture.LoadImage(File.ReadAllBytes(path)); return texture;
        }
        private static void Crop(Texture2D source, int x, int y, int w, int h, int rw, int rh, string path, bool logo)
        {
            int px = Mathf.RoundToInt(x * source.width / (float)rw), py = Mathf.RoundToInt(y * source.height / (float)rh);
            int pw = Mathf.RoundToInt(w * source.width / (float)rw), ph = Mathf.RoundToInt(h * source.height / (float)rh);
            var pixels = source.GetPixels(px, source.height - py - ph, pw, ph);
            if (logo) for (int i = 0; i < pixels.Length; i++)
                pixels[i] = pixels[i].r > 0.45f && pixels[i].g > 0.45f ? new Color32(183,195,173,255) : Color.clear;
            var result = new Texture2D(pw, ph); result.SetPixels(pixels); result.Apply();
            File.WriteAllBytes(path, result.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(result);
        }
    }
}
#endif
