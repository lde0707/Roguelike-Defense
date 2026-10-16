using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Rushitadel.Editor
{
 public static class OrbitPressureValidation
 {
  public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");SessionState.SetBool("Orbit.Check",true);double deadline=EditorApplication.timeSinceStartup+180;EditorApplication.update+=()=>{if(EditorApplication.timeSinceStartup>deadline)EditorApplication.Exit(2);};EditorApplication.EnterPlaymode();}
  [InitializeOnLoadMethod]private static void Hook(){EditorApplication.playModeStateChanged+=s=>{if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("Orbit.Check",false))return;SessionState.SetBool("Orbit.Check",false);new GameObject("Orbit probe").AddComponent<Rushitadel.Presentation.OrbitPressureProbe>();};}
 }
}
