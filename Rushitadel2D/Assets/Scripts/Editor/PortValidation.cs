#if UNITY_EDITOR
using System;
using System.Linq;
using Rushitadel.Combat;
using Rushitadel.Player;
using Rushitadel.Power;
using Rushitadel.Progression;
using Rushitadel.Spawning;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Rushitadel.Editor
{
    public static class PortValidation
    {
        public static void BuildAndRun()
        {
            GameplaySceneSetup.Build();
            Run();
        }

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/inGame.unity");
            Require(UnityEngine.Object.FindObjectsByType<TowerController>(FindObjectsSortMode.None).Length == 5, "Five attached turrets");
            Require(UnityEngine.Object.FindFirstObjectByType<Rigidbody>() == null, "No 3D rigidbody");
            Require(UnityEngine.Object.FindFirstObjectByType<Collider>() == null, "No 3D collider");
            Require(UnityEngine.Object.FindFirstObjectByType<PlayerController>() != null, "Player movement");
            Require(UnityEngine.Object.FindFirstObjectByType<PlayerAttackController>() != null, "Mouse aimed player attack");
            Require(UnityEngine.Object.FindFirstObjectByType<PlayerPowerShotController>() != null, "Power shot");
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "No missing script: " + child.name);
            Require(EditorBuildSettings.scenes.Count(s => s.enabled) == 2, "Title and gameplay in build");
            SessionState.SetBool("Rushitadel2D.Validation", true);
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("Rushitadel2D.Validation", false)) return;
                SessionState.SetBool("Rushitadel2D.Validation", false);
                new GameObject("Port Smoke Test").AddComponent<PortSmokeProbe>();
            };
        }

        private static void Require(bool condition, string description)
        {
            if (!condition) throw new Exception("PORT VALIDATION FAILED: " + description);
            Debug.Log("PASS: " + description);
        }
    }
}
#endif
