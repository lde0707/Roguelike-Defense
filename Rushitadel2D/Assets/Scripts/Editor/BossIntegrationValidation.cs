#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Rushitadel.Editor
{
    public static class BossIntegrationValidation
    {
        public static void Run()
        {
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
            SessionState.SetBool("Rushitadel.BossIntegration",true);
            double deadline=EditorApplication.timeSinceStartup+240;
            EditorApplication.update+=()=>{if(EditorApplication.timeSinceStartup>deadline)EditorApplication.Exit(2);};
            EditorApplication.isPlaying=true;
        }
        [InitializeOnLoadMethod]private static void Hook()
        {
            EditorApplication.playModeStateChanged+=s=>{
                if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("Rushitadel.BossIntegration",false))return;
                SessionState.SetBool("Rushitadel.BossIntegration",false);
                new GameObject("Boss integration validation").AddComponent<Rushitadel.Progression.BossIntegrationProbe>();
            };
        }
    }
}
#endif
