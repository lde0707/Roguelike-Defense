#if UNITY_EDITOR
using System.IO;
using System.Linq;
using Rushitadel.Combat;
using Rushitadel.Player;
using Rushitadel.Power;
using Rushitadel.Progression;
using Rushitadel.Spawning;
using Rushitadel.Presentation;
using Rushitadel.World;
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
        private static Sprite square;

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
            square = CreateSquare();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.35f, 0.4f, 0.65f);
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

            var hostileObject = new GameObject("enemyProjectile");
            AttachVisual(hostileObject.transform, "Shell", new Color(1f, 0.24f, 0.12f), new Vector2(0.24f, 0.24f), 45, Vector2.zero);
            hostileObject.AddComponent<EnemyProjectileController>();
            var hostilePrefab = SavePrefab(hostileObject, "enemyProjectile");
            var rangedObject = Object.Instantiate(enemyPrefab);
            rangedObject.name = "RangedEnemy";
            rangedObject.AddComponent<CullBehindCamera>();
            Set(rangedObject.GetComponent<EnemyController>(), "moveSpeed", 0f);
            Set(rangedObject.GetComponent<EnemyController>(), "contactDamage", 0f);
            Set(rangedObject.AddComponent<EnemyRangedAttack>(), "projectilePrefab", hostilePrefab.GetComponent<EnemyProjectileController>());
            var rangedPrefab = SavePrefab(rangedObject, "RangedEnemy");

            GameObject towerBulletObject = Visual("towerProjectile", circle, new Color(1f, 0.65f, 0.05f), new Vector2(0.1f, 0.22f), 40);
            towerBulletObject.AddComponent<ProjectileController>();
            GameObject towerBullet = SavePrefab(towerBulletObject, "towerProjectile");
            GameObject basicBulletObject = Visual("playerBasicProjectile", circle, new Color(0.2f, 0.8f, 1f), new Vector2(0.1f, 0.22f), 40);
            basicBulletObject.AddComponent<ProjectileController>();
            GameObject basicBullet = SavePrefab(basicBulletObject, "playerBasicProjectile");
            GameObject playerBulletObject = new GameObject("playerProjectile");
            AttachVisual(playerBulletObject.transform, "Outer Area", new Color(0.15f, 0.6f, 1f, 0.3f), Vector2.one, 42, Vector2.zero);
            AttachVisual(playerBulletObject.transform, "Execute Core", new Color(0.7f, 0.9f, 1f, 0.8f), Vector2.one * PlayerProjectileController.ExecuteRadiusFraction, 43, Vector2.zero);
            playerBulletObject.AddComponent<PlayerProjectileController>();
            GameObject playerBullet = SavePrefab(playerBulletObject, "playerProjectile");

            const string backgroundPath = "Assets/Image/ScrollingBattlefield.png";
            AssetDatabase.ImportAsset(backgroundPath);
            var backgroundImporter = (TextureImporter)AssetImporter.GetAtPath(backgroundPath);
            backgroundImporter.textureType = TextureImporterType.Sprite;
            backgroundImporter.spriteImportMode = SpriteImportMode.Single;
            backgroundImporter.mipmapEnabled = false;
            backgroundImporter.filterMode = FilterMode.Point;
            backgroundImporter.textureCompression = TextureImporterCompression.Uncompressed;
            backgroundImporter.wrapMode = TextureWrapMode.Repeat;
            backgroundImporter.SaveAndReimport();
            Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(backgroundPath);
            var loop = new GameObject("Looping Battlefield").AddComponent<LoopingBattlefield>();
            var panels = new SpriteRenderer[3];
            for (int i = 0; i < panels.Length; i++)
            {
                var panel = Visual("Ground Segment " + i, background, Color.white, new Vector2(19.2f, 10.8f), -10);
                panel.transform.SetParent(loop.transform, false);
                panels[i] = panel.GetComponent<SpriteRenderer>();
            }
            loop.Configure(camera, panels, 10.8f);
            var boundaries = new GameObject("Cliff Boundaries").AddComponent<PlayfieldBoundary>();
            boundaries.transform.SetParent(camera.transform, false);
            boundaries.transform.localPosition = new Vector3(0f, 0f, 10f);
            BoxCollider2D leftWall = CreateCliffWall(boundaries.transform, "Left Invisible Wall", -5.65f);
            BoxCollider2D rightWall = CreateCliffWall(boundaries.transform, "Right Invisible Wall", 5.65f);
            boundaries.Configure(leftWall, rightWall);
            var rearHazard = CreatePressureZone(camera.transform, "Rear Theme Hazard", 1.2f, true);

            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(0f, -1f, 0f);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            player.AddComponent<CircleCollider2D>().radius = 0.65f;
            player.AddComponent<PlayerHealth>();
            player.AddComponent<PlayerController>();
            player.AddComponent<PlayerExperience>();
            PlayerPower power = player.AddComponent<PlayerPower>();
            Transform rotatingBody = new GameObject("Rotating Body").transform;
            rotatingBody.SetParent(player.transform, false);
            player.AddComponent<PlayerTurretRig>().Configure(rotatingBody);
            AttachVisual(rotatingBody, "Hull", new Color(0.16f, 0.19f, 0.3f), new Vector2(1.3f, 1.3f), 100, Vector2.zero);
            AttachVisual(rotatingBody, "Armor", new Color(0.4f, 0.43f, 0.58f), new Vector2(1.08f, 1.08f), 101, Vector2.zero);
            AttachVisual(rotatingBody, "Core Ring", new Color(0.2f, 0.08f, 0.38f), new Vector2(0.62f, 0.62f), 103, Vector2.zero);
            AttachVisual(rotatingBody, "Power Core", new Color(0.65f, 0.25f, 1f), new Vector2(0.42f, 0.42f), 104, Vector2.zero);
            for (int i = 0; i < PlayerTurretRig.TurretCount; i++)
                CreateTower(rotatingBody, i + 1, towerBullet);
            Transform shot = new GameObject("Power Shot Origin").transform;
            shot.SetParent(player.transform, false);
            player.AddComponent<PlayerPowerShotController>().Configure(power, null, shot, playerBullet);
            player.AddComponent<PlayerAttackController>().Configure(null, player.transform, basicBullet);
            PrefabUtility.SaveAsPrefabAsset(player, Prefabs + "Player.prefab");

            var systems = new GameObject("Gameplay Systems");
            systems.AddComponent<ForwardPressureController>().Configure(camera, rearHazard, null,
                rearHazard.GetComponent<ThemePressureVisual>(), null);
            var spawner = systems.AddComponent<EnemySpawner>();
            spawner.Configure(rangedPrefab);
            systems.AddComponent<GameplayHud>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/inGame.unity");
            ConfigureTitle();
            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene("Assets/Scenes/Title.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/inGame.unity", true)
            };
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
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

        private static BoxCollider2D CreateCliffWall(Transform parent, string name, float x)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = new Vector3(x, 0f, 0f);
            var collider = wall.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.5f, 40f);
            collider.isTrigger = false;
            return collider;
        }

        private static BoxCollider2D CreatePressureZone(Transform camera, string name, float depth, bool rear)
        {
            var root = new GameObject(name);
            root.transform.SetParent(camera, false);
            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(10.8f, depth);
            box.isTrigger = rear;
            var fill = Visual("Zone Fill", square, Color.white, box.size, 5);
            fill.transform.SetParent(root.transform, false);
            var details = new SpriteRenderer[16];
            for (int i = 0; i < details.Length; i++)
            {
                var detail = Visual("Theme Detail " + i, square, Color.white, Vector2.one, 6);
                detail.transform.SetParent(root.transform, false);
                detail.transform.localPosition = new Vector3(-5.1f + i * 0.68f, rear ? depth * 0.5f - 0.22f : 0f, 0f);
                details[i] = detail.GetComponent<SpriteRenderer>();
            }
            root.AddComponent<ThemePressureVisual>().Configure(fill.GetComponent<SpriteRenderer>(), details, square, circle, rear);
            return box;
        }

        private static void CreateTower(Transform root, int slotId, GameObject projectile)
        {
            GameObject go = new GameObject($"T{slotId}");
            go.transform.SetParent(root, false);
            float angle = (slotId - 1) * 72f;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, -angle);
            go.transform.localPosition = Quaternion.Euler(0f, 0f, -angle) * (Vector3.up * 0.55f);
            AttachVisual(go.transform, "Mount", new Color(0.23f, 0.25f, 0.4f), new Vector2(0.32f, 0.32f), 102, Vector2.zero);
            AttachVisual(go.transform, "Barrel", new Color(0.68f, 0.67f, 0.8f), new Vector2(0.18f, 0.46f), 103, new Vector2(0f, 0.12f));
            AttachVisual(go.transform, "Emitter", new Color(0.64f, 0.2f, 1f), new Vector2(0.12f, 0.16f), 104, new Vector2(0f, 0.28f));
            Transform muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(go.transform, false);
            muzzle.localPosition = new Vector3(0f, 0.38f, 0f);
            var tower = go.AddComponent<TowerController>();
            tower.Configure(muzzle, projectile);
            var serialized = new SerializedObject(tower);
            serialized.FindProperty("slotId").intValue = slotId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AttachVisual(Transform parent, string name, Color color, Vector2 size, int order, Vector2 offset)
        {
            GameObject visual = Visual(name, circle, color, size, order);
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = offset;
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

        private static Sprite CreateSquare()
        {
            const string path = "Assets/Generated/Solid.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 1;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
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
