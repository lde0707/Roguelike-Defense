#if UNITY_EDITOR
using System.IO;
using System.Linq;
using Rushitadel.Combat;
using Rushitadel.Player;
using Rushitadel.Power;
using Rushitadel.Progression;
using Rushitadel.Spawning;
using Rushitadel.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rushitadel.Editor
{
    /// <summary>Explicit, repeatable scene generation. Never rewrites scenes on editor reload.</summary>
    public static class GameplaySceneSetup
    {
        private const string Prefabs = "Assets/Prefabs/";
        private static Material spriteMaterial;
        private static Sprite circle;

        [MenuItem("Rushitadel 2D/Rebuild Gameplay Scene")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Prefabs);
            Directory.CreateDirectory("Assets/Generated");
            spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/SpriteUnlit.mat");
            if (spriteMaterial == null)
            {
                spriteMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
                AssetDatabase.CreateAsset(spriteMaterial, "Assets/Generated/SpriteUnlit.mat");
            }
            circle = CreateCircle();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0.73f, 0.85f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 6.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.white;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            var settings = AssetDatabase.LoadAssetAtPath<MouseWorldInteractionSettings>("Assets/Generated/MouseWorldInteractionSettings.asset");
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<MouseWorldInteractionSettings>();
                AssetDatabase.CreateAsset(settings, "Assets/Generated/MouseWorldInteractionSettings.asset");
            }
            GameObject pickupObject = Visual("EXP", circle, new Color(0.15f, 0.85f, 0.35f), new Vector2(0.12f, 0.12f), 30);
            pickupObject.AddComponent<CircleCollider2D>().isTrigger = true;
            pickupObject.AddComponent<ExperiencePickup>();
            GameObject pickup = SavePrefab(pickupObject, "EXP");

            GameObject enemyObject = Visual("Enemy", circle, new Color(0.78f, 0.15f, 0.18f), new Vector2(0.38f, 0.55f), 20);
            enemyObject.AddComponent<CircleCollider2D>().isTrigger = true;
            EnemyController enemy = enemyObject.AddComponent<EnemyController>();
            Set(enemy, "experiencePickupPrefab", pickup.GetComponent<ExperiencePickup>());
            GameObject enemyPrefab = SavePrefab(enemyObject, "Enemy");

            GameObject towerBulletObject = Visual("towerProjectile", circle, new Color(1f, 0.65f, 0.05f), new Vector2(0.1f, 0.22f), 40);
            towerBulletObject.AddComponent<ProjectileController>();
            GameObject towerBullet = SavePrefab(towerBulletObject, "towerProjectile");
            GameObject playerBulletObject = Visual("playerProjectile", circle, new Color(0f, 0.6f, 1f), new Vector2(0.12f, 0.24f), 40);
            Set(playerBulletObject.AddComponent<PlayerProjectileController>(), "interactionSettings", settings);
            GameObject playerBullet = SavePrefab(playerBulletObject, "playerProjectile");

            Sprite citadelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/Citadel2.png");
            GameObject citadel = Visual("Citadel", citadelSprite, Color.white, new Vector2(20.05f, 11.27f), -10);
            citadel.transform.position = new Vector3(0.601f, 0.85f, 0f);

            // Keep the original seven weapons and stats; align their XY origins with the artwork.
            GameObject towers = new GameObject("Tower");
            CreateTower(towers.transform, "mainTower", new Vector2(0.6f, 1.65f), 3f, 25f, towerBullet);
            float[] rows = { 4.25f, 2.3f, -0.55f };
            for (int row = 0; row < rows.Length; row++)
                for (int side = 0; side < 2; side++)
                    CreateTower(towers.transform, $"Tower {row * 2 + side + 1}", new Vector2(side == 0 ? -3.1f : 4.4f, rows[row]), 4f, 50f, towerBullet);

            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(0f, -1.34f, 0f);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            player.AddComponent<CircleCollider2D>().radius = 0.25f;
            PlayerController controller = player.AddComponent<PlayerController>();
            Set(controller, "aimCamera", camera);
            Set(controller, "playerBulletPrefab", playerBullet);
            Transform shot = new GameObject("playerShotPos").transform;
            shot.SetParent(player.transform, false);
            shot.localPosition = new Vector3(0f, 0.3f, 0f);
            Set(controller, "playerShotPos", shot);
            player.AddComponent<PlayerExperience>();
            PlayerPower power = player.AddComponent<PlayerPower>();
            player.AddComponent<PlayerPowerShotController>().Configure(power, camera, shot, playerBullet);

            Sprite playerSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Image/Player/PlayerAnimation.png")
                .OfType<Sprite>().First(s => s.name == "PlayerAnimation_8");
            GameObject visual = Visual("Player Visual", playerSprite, Color.white, new Vector2(3.2f, 1.8f), 100);
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = new Vector3(-0.015f, 0.665f, 0f);
            visual.AddComponent<Animator>().runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/PlayerAnimation_8.controller");
            visual.AddComponent<PlayerSpriteMotion>();

            var systems = new GameObject("Gameplay Systems");
            var spawner = systems.AddComponent<EnemySpawner>();
            spawner.Configure(enemyPrefab);
            spawner.SetEnemyCountModifier(power);
            systems.AddComponent<GameplayHud>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/inGame.unity");
            ConfigureTitle();
            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene("Assets/Scenes/Title.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/inGame.unity", true)
            };
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            AssetDatabase.SaveAssets();
            Debug.Log("RUSHITADEL_2D_BUILD_SUCCESS");
        }

        private static void ConfigureTitle()
        {
            Scene title = EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
            foreach (var root in title.GetRootGameObjects())
            {
                if (root.name == "Directional Light" || root.name == "Global Volume")
                    Object.DestroyImmediate(root);
            }
            if (Object.FindFirstObjectByType<TitleMenu>() == null) new GameObject("Title Menu").AddComponent<TitleMenu>();
            EditorSceneManager.SaveScene(title);
        }

        private static void CreateTower(Transform root, string name, Vector2 position, float range, float damage, GameObject projectile)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(root);
            go.transform.position = position;
            var tower = go.AddComponent<TowerController>();
            tower.Configure(go.transform, projectile);
            Set(tower, "attackRange", range);
            Set(tower, "damage", damage);
        }

        private static GameObject Visual(string name, Sprite sprite, Color color, Vector2 size, int order)
        {
            GameObject go = new GameObject(name);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = spriteMaterial;
            renderer.color = color;
            renderer.sortingOrder = order;
            go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1f);
            return go;
        }

        private static GameObject SavePrefab(GameObject go, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, Prefabs + name + ".prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static Sprite CreateCircle()
        {
            const string path = "Assets/Generated/Circle.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(32, 32);
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                        texture.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) <= 15f ? Color.white : Color.clear);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void Set(Object target, string name, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Set(Object target, string name, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
