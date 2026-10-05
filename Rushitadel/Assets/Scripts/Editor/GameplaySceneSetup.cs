#if UNITY_EDITOR
using Rushitadel.Combat;
using Rushitadel.Presentation;
using Rushitadel.Power;
using Rushitadel.Player;
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
        private const string PlayerProjectilePrefabPath = "Assets/Prefabs/playerProjectile.prefab";
        private const float GameplayCameraOrthographicSize = 620f;
        private const int PlayerSortingOrder = 100;

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

            ConfigurePlayerPresentation(scene);
            ConfigurePowerSystem(scene, spawner);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedForSetup)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ConfigurePlayerPresentation(Scene scene)
        {
            GameObject player = FindRoot(scene, "Player");
            GameObject playerVisual = FindRoot(scene, "PlayerAnimation_8");
            Camera mainCamera = FindMainCamera(scene);
            if (player == null || playerVisual == null || mainCamera == null)
            {
                return;
            }

            Collider characterCollider = player.GetComponent<Collider>();
            if (characterCollider == null)
            {
                Debug.LogError("Player needs a Collider for sprite foot grounding.");
                return;
            }

            MeshRenderer placeholderRenderer = player.GetComponent<MeshRenderer>();
            if (placeholderRenderer != null)
            {
                placeholderRenderer.enabled = false;
            }

            CameraAlignedSprite alignedSprite = playerVisual.GetComponent<CameraAlignedSprite>();
            if (alignedSprite == null)
            {
                alignedSprite = playerVisual.AddComponent<CameraAlignedSprite>();
            }

            alignedSprite.SetTargetCamera(mainCamera);
            alignedSprite.SetSortingOrder(PlayerSortingOrder);
            alignedSprite.SetCharacterCollider(characterCollider);
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = GameplayCameraOrthographicSize;
        }

        private static Camera FindMainCamera(Scene scene)
        {
            GameObject cameraObject = FindRoot(scene, "Main Camera");
            return cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
        }

        private static void ConfigurePowerSystem(Scene scene, EnemySpawner spawner)
        {
            GameObject player = FindRoot(scene, "Player");
            Camera mainCamera = FindMainCamera(scene);
            if (player == null || mainCamera == null || spawner == null)
            {
                return;
            }

            PlayerPower power = player.GetComponent<PlayerPower>();
            if (power == null)
            {
                power = player.AddComponent<PlayerPower>();
            }

            PlayerPowerShotController powerShot = player.GetComponent<PlayerPowerShotController>();
            if (powerShot == null)
            {
                powerShot = player.AddComponent<PlayerPowerShotController>();
            }

            GameObject projectilePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerProjectilePrefabPath);
            powerShot.Configure(power, mainCamera, FindShotPosition(player.transform), projectilePrefab);
            spawner.SetEnemyCountModifier(power);
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
                if (child.name == "shotPos" ||
                    child.name == "mainShotPos" ||
                    child.name == "playerShotPos")
                {
                    return child;
                }
            }

            return tower;
        }
    }
}
#endif
