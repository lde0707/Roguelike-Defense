#if UNITY_EDITOR
using Rushitadel.Combat;
using Rushitadel.Spawning;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rushitadel.Editor
{
    public static class GameplaySceneSetup
    {
        private const string ScenePath = "Assets/Scenes/inGame.unity";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        private const string ProjectilePrefabPath = "Assets/Prefabs/towerProjectile.prefab";

        [InitializeOnLoadMethod]
        private static void ScheduleSetup() => EditorApplication.delayCall += SetupIfPossible;

        [MenuItem("Rushitadel/Setup Gameplay Components")]
        public static void SetupIfPossible()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                return;
            }

            AttachPrefabComponent<EnemyController>(EnemyPrefabPath);
            AttachPrefabComponent<ProjectileController>(ProjectilePrefabPath);
            ConfigureScene();
            AssetDatabase.SaveAssets();
        }

        private static void AttachPrefabComponent<T>(string prefabPath) where T : Component
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"Prefab could not be loaded: {prefabPath}");
                return;
            }

            if (prefabRoot.GetComponent<T>() == null)
            {
                prefabRoot.AddComponent<T>();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        private static void ConfigureScene()
        {
            Scene scene = FindLoadedScene();
            bool openedForSetup = !scene.IsValid();
            if (openedForSetup)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            GameObject towerRoot = FindRoot(scene, "Tower");
            GameObject projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            if (towerRoot != null)
            {
                foreach (Transform towerTransform in towerRoot.transform)
                {
                    TowerController tower = towerTransform.GetComponent<TowerController>();
                    bool isNew = tower == null;
                    if (isNew)
                    {
                        tower = towerTransform.gameObject.AddComponent<TowerController>();
                    }

                    SerializedObject serializedTower = new(tower);
                    SerializedProperty shotPos = serializedTower.FindProperty("shotPos");
                    SerializedProperty projectile = serializedTower.FindProperty("projectilePrefab");
                    if (isNew || shotPos.objectReferenceValue == null)
                    {
                        shotPos.objectReferenceValue = FindShotPosition(towerTransform);
                    }

                    if (isNew || projectile.objectReferenceValue == null)
                    {
                        projectile.objectReferenceValue = projectilePrefab;
                    }

                    serializedTower.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            GameObject systems = FindRoot(scene, "Gameplay Systems");
            if (systems == null)
            {
                systems = new GameObject("Gameplay Systems");
                SceneManager.MoveGameObjectToScene(systems, scene);
            }

            EnemySpawner spawner = systems.GetComponent<EnemySpawner>();
            if (spawner == null)
            {
                spawner = systems.AddComponent<EnemySpawner>();
            }

            SerializedObject serializedSpawner = new(spawner);
            SerializedProperty enemyPrefab = serializedSpawner.FindProperty("enemyPrefab");
            if (enemyPrefab.objectReferenceValue == null)
            {
                enemyPrefab.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
                serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedForSetup)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Scene FindLoadedScene()
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (scene.path == ScenePath)
                {
                    return scene;
                }
            }

            return default;
        }

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == objectName)
                {
                    return rootObject;
                }
            }

            return null;
        }

        private static Transform FindShotPosition(Transform tower)
        {
            foreach (Transform child in tower)
            {
                if (child.name == "shotPos" || child.name == "mainShotPos")
                {
                    return child;
                }
            }

            return tower;
        }
    }
}
#endif
