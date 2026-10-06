#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Rushitadel.Editor
{
    public static class TitleUiValidation
    {
        public static void Run()
        {
            TitleUiSetup.Prepare();
            EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
            SessionState.SetBool("Rushitadel.UiValidation", true);
            EditorApplication.isPlaying = true;
        }
        [InitializeOnLoadMethod]
        private static void Hook()
        {
            EditorApplication.playModeStateChanged += state => {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("Rushitadel.UiValidation", false)) return;
                SessionState.SetBool("Rushitadel.UiValidation", false);
                new GameObject("Title UI Validation").AddComponent<Rushitadel.Presentation.TitleUiProbe>();
            };
        }
    }
}
#endif
