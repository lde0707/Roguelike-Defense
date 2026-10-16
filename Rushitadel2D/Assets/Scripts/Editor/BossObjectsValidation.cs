using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Rushitadel.Editor
{
 public static class BossObjectsValidation
 {
  public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");SessionState.SetBool("BossObjects.Check",true);double end=EditorApplication.timeSinceStartup+240;EditorApplication.update+=()=>{if(EditorApplication.timeSinceStartup>end)EditorApplication.Exit(2);};EditorApplication.EnterPlaymode();}
  [InitializeOnLoadMethod]private static void Hook(){EditorApplication.playModeStateChanged+=s=>{if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("BossObjects.Check",false))return;SessionState.SetBool("BossObjects.Check",false);new GameObject("Boss object validation").AddComponent<Rushitadel.Presentation.BossObjectsProbe>();};}
 }
}
