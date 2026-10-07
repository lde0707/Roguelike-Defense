#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Rushitadel.Editor
{
    public static class FlightValidation
    {
        public static void Run()
        {
            AssetDatabase.Refresh();
            foreach (string file in new[]{"ElementSprite","FlightVfx"})
            {
                var shader=Resources.Load<Shader>("PlayerForms/"+file);
                if(shader==null || ShaderUtil.ShaderHasError(shader))
                    throw new System.Exception("Invalid shader: "+file);
            }
            EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
            Application.runInBackground=true;
            double deadline=EditorApplication.timeSinceStartup+120;
            EditorApplication.update+=()=>
            {
                if(EditorApplication.timeSinceStartup>deadline){Debug.LogError("Flight validation timed out");EditorApplication.Exit(1);}
                if(!EditorApplication.isPlaying)return;
                EditorApplication.isPaused=false;
                EditorApplication.QueuePlayerLoopUpdate();
            };
            SessionState.SetBool("Rushitadel.FlightValidation",true);
            EditorApplication.isPlaying=true;
        }
        [InitializeOnLoadMethod] private static void Hook()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("Rushitadel.FlightValidation",false))return;
                SessionState.SetBool("Rushitadel.FlightValidation",false);
                new GameObject("Flight validation").AddComponent<Rushitadel.Presentation.FlightValidationProbe>();
            };
        }
    }
}
#endif
