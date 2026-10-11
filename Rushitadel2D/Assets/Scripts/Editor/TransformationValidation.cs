#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Rushitadel.Editor
{
    public static class TransformationValidation
    {
        public static void Run()
        {
            Application.runInBackground=true;
            double deadline=EditorApplication.timeSinceStartup+180;
            EditorApplication.update+=()=>
            {
                if(EditorApplication.timeSinceStartup>deadline){Debug.LogError("Transformation validation timed out");EditorApplication.Exit(1);}
                if(EditorApplication.isPlaying){EditorApplication.isPaused=false;EditorApplication.QueuePlayerLoopUpdate();}
            };
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/PlayerForms"}))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.isReadable=true;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
                importer.SaveAndReimport();
            }
            Debug.Log("TRANSFORMATION_IMPORT_OK");
            EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
            SessionState.SetBool("Rushitadel.FormValidation",true);EditorApplication.isPlaying=true;
        }
        [InitializeOnLoadMethod] private static void Hook()
        {
            EditorApplication.playModeStateChanged+=s=>{if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("Rushitadel.FormValidation",false))return;SessionState.SetBool("Rushitadel.FormValidation",false);new GameObject("Transformation Probe").AddComponent<Rushitadel.Progression.TransformationProbe>();};
        }
    }
}
#endif
