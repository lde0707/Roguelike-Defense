using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Rushitadel.Editor
{
    public static class ThemeContentValidation
    {
        public static void Run()
        {
            AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
            SessionState.SetBool("ThemeContent.Check",true);
            double deadline=EditorApplication.timeSinceStartup+180;
            EditorApplication.update+=()=>{if(EditorApplication.timeSinceStartup>deadline)EditorApplication.Exit(2);};
            EditorApplication.EnterPlaymode();
        }
        [InitializeOnLoadMethod]private static void Hook()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("ThemeContent.Check",false))return;
                SessionState.SetBool("ThemeContent.Check",false);
                new GameObject("Theme content validation").AddComponent<Rushitadel.Progression.ThemeContentProbe>();
            };
        }
    }
}
