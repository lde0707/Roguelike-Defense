#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Rushitadel.Editor
{
    public static class WebTreeValidation
    {
        public static void Run()
        {
            TitleScenePreviewBuilder.Refresh();
            EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
            SessionState.SetBool("Rushitadel.WebTreeValidation",true);EditorApplication.isPlaying=true;
        }
        [InitializeOnLoadMethod] private static void Hook()
        {
            EditorApplication.playModeStateChanged+=state=>{
                if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("Rushitadel.WebTreeValidation",false))return;
                SessionState.SetBool("Rushitadel.WebTreeValidation",false);
                new GameObject("Web Tree Probe").AddComponent<Rushitadel.Presentation.WebTreeProbe>();
            };
        }
    }
}
#endif
