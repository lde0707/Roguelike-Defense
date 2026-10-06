#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Rushitadel.Editor
{
    public static class CampaignValidation
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
            SessionState.SetBool("Rushitadel.CampaignValidation", true); EditorApplication.isPlaying = true;
        }
        [InitializeOnLoadMethod]
        private static void Hook()
        {
            EditorApplication.playModeStateChanged += state => {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("Rushitadel.CampaignValidation", false)) return;
                SessionState.SetBool("Rushitadel.CampaignValidation", false);
                new GameObject("Campaign Validation").AddComponent<Rushitadel.Progression.CampaignProbe>();
            };
        }
    }
}
#endif
