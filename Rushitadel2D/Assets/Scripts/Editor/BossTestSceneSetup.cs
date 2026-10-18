using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Rushitadel.Editor
{
    public static class BossTestSceneSetup
    {
        public const string Path="Assets/Scenes/Test.unity";
        [MenuItem("Rushitadel/보스 패턴 테스트 열기")]
        public static void Open()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.playModeStartScene=null;
            EditorSceneManager.OpenScene(Path);
        }
        public static void Generate()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Boss Pattern Test").AddComponent<Rushitadel.Presentation.BossTestController>();
            EditorSceneManager.SaveScene(scene,Path);
            var scenes=EditorBuildSettings.scenes.ToList();
            if(!scenes.Any(s=>s.path==Path))scenes.Add(new EditorBuildSettingsScene(Path,true));
            EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();
            Debug.Log("BOSS TEST SCENE GENERATED");
        }
    }
}
