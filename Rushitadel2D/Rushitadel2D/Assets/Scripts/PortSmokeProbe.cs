#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Rushitadel.Combat;
using Rushitadel.Player;
using Rushitadel.Power;
using Rushitadel.Progression;
using Rushitadel.Spawning;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Rushitadel
{
    /// <summary>Batch-mode regression probe. Not included in player builds.</summary>
    public sealed class PortSmokeProbe : MonoBehaviour
    {
        private bool failed;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Application.logMessageReceived += OnLog;
        }
        private void OnDestroy() => Application.logMessageReceived -= OnLog;
        private void OnLog(string message, string stack, LogType type)
        {
            // Unity's optional online services can report this during offline batch runs.
            if (message.StartsWith("Curl error", StringComparison.Ordinal)) return;
            if (stack.Contains("UnityEditor.Search.SearchDatabase") && !stack.Contains("Rushitadel")) return;
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                failed = true;
                System.IO.File.AppendAllText("ValidationErrors.txt", message + "\n" + stack + "\n");
                EditorApplication.delayCall += () => EditorApplication.Exit(1);
            }
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(8f);
            Check(EnemyRegistry.ActiveEnemies.Any(), "Automatic spawning and live registry");
            Check(FindFirstObjectByType<PlayerExperience>().TotalExperience > 0, "Automatic tower combat and XP collection");
            var sceneSpawner = FindFirstObjectByType<EnemySpawner>();
            var modifier = typeof(EnemySpawner).GetField("enemyCountModifier", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sceneSpawner);
            Check(ReferenceEquals(modifier, FindFirstObjectByType<PlayerPower>()), "Scene bindings connect the spawner to player power");
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Camera camera = Camera.main;
                var target = new RenderTexture(1280, 720, 24);
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                    new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                var picture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                picture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                picture.Apply();
                System.IO.File.WriteAllBytes("GameplayPreview.png", picture.EncodeToPNG());
                camera.targetTexture = null;
                RenderTexture.active = null;
                Destroy(target);
                Destroy(picture);
            }
            Destroy(FindFirstObjectByType<EnemySpawner>());
            foreach (TowerController tower in TowerRegistry.ActiveTowers.ToArray()) tower.enabled = false;
            foreach (EnemyController enemy in EnemyRegistry.ActiveEnemies.ToArray()) Destroy(enemy.gameObject);
            foreach (ProjectileBehaviour bullet in FindObjectsByType<ProjectileBehaviour>(FindObjectsSortMode.None)) Destroy(bullet.gameObject);
            yield return null;

            PlayerController player = FindFirstObjectByType<PlayerController>();
            Vector3 start = player.transform.position;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
            yield return new WaitForSeconds(0.3f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForFixedUpdate();
            Vector3 delta = player.transform.position - start;
            Debug.Log($"Movement delta: {delta}; keyboard: {Keyboard.current?.name}; velocity: {player.GetComponent<Rigidbody2D>().linearVelocity}");
            Check(delta.x > 0.2f && delta.y > 0.2f && Mathf.Abs(delta.z) < 0.001f, "WASD moves in XY only");
            Check(delta.magnitude < 1f, "Diagonal speed is normalized");
            InputSystem.RemoveDevice(keyboard);

            EnemyController near = SpawnEnemy(new Vector3(0f, 2f, 0f));
            EnemyController far = SpawnEnemy(new Vector3(0f, 4f, 0f));
            Check(near.Definition == far.Definition, "Enemy instances share the same immutable definition");
            Check(EnemyRegistry.FindAllWithinRadius(new Vector3(0f, 2f, 0f), 1f).Count == 1, "Area damage respects vertical distance in XY");
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/PlayerProjectile.prefab");
            var bulletObject = Instantiate(projectilePrefab, Vector3.zero, Quaternion.identity);
            bulletObject.GetComponent<PlayerProjectileController>().Launch(Vector3.up, near.transform.position, 25f);
            yield return new WaitForSeconds(0.3f);
            Check(near.CurrentHealth == 75f && far.CurrentHealth == 100f, "Normal shot deals 25 only inside its destination radius");

            var power = player.GetComponent<PlayerPower>();
            Check(power.TrySpend(power.CurrentPower), "Can deplete remaining power");
            Check(power.State == PowerCycleState.Recovering && power.EnemyCountMultiplier == 1.5f, "Recovery increases spawn pressure by 1.5");
            Check(!power.TrySpend(20f), "Insufficient power blocks shot");
            var originalPowerDefinition = power.Definition;
            string powerAssetBefore = JsonUtility.ToJson(originalPowerDefinition);
            var fastRecovery = Instantiate(originalPowerDefinition);
            SetField(fastRecovery, "recoveryPerSecond", 600f);
            SetField(power, "definition", fastRecovery);
            yield return new WaitForSeconds(0.2f);
            Check(power.State == PowerCycleState.Depleting && power.CurrentPower <= 60f, "Recovery reaches 60 and resumes depletion");
            int experience = player.GetComponent<PlayerExperience>().TotalExperience;
            float beforeShot = power.CurrentPower;
            Vector2 aimPosition = Camera.main.WorldToScreenPoint(new Vector3(0f, 2f, 0f));
            typeof(PlayerPowerShotController).GetMethod("TryFire", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(player.GetComponent<PlayerPowerShotController>(), new object[] { aimPosition });
            Check(Mathf.Approximately(power.CurrentPower, beforeShot - 20f), "Actual power shot costs 20");
            yield return new WaitForSeconds(1f);
            Check(near == null && far == null, "Power explosion eliminates enemies within 2.5 units");
            Check(player.GetComponent<PlayerExperience>().TotalExperience == experience + 40, "Two kills collect 40 experience");

            near = SpawnEnemy(new Vector3(0f, 1f, 0f));
            far = SpawnEnemy(new Vector3(0f, 2f, 0f));
            var homingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/TowerProjectile.prefab");
            var homing = Instantiate(homingPrefab, Vector3.zero, Quaternion.identity).GetComponent<ProjectileController>();
            homing.Launch(near, 50f, Vector3.zero, 4f);
            Destroy(near.gameObject);
            yield return new WaitForSeconds(0.4f);
            Check(far.CurrentHealth == 50f, "Tower projectile retargets when original target disappears");
            var gauge = player.GetComponent<PlayerExperience>();
            int oldThresholds = gauge.CompletedThresholds;
            gauge.AddExperience(200);
            Check(gauge.CompletedThresholds == oldThresholds + 2, "XP crosses multiple thresholds");
            Check(JsonUtility.ToJson(originalPowerDefinition) == powerAssetBefore, "Shared power asset is unchanged during gameplay");
            SetField(power, "definition", originalPowerDefinition);
            Destroy(fastRecovery);
            SceneManager.LoadScene("Title");
            yield return new WaitForSeconds(0.5f);
            var buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
            Check(buttons.Length == 4, "Title menu keeps four original buttons");
            Check(!buttons.First(b => b.name == "ContinueButton").interactable, "Unavailable continue is disabled");
            buttons.First(b => b.name == "NewGameButton").onClick.Invoke();
            yield return new WaitForSeconds(0.5f);
            Check(SceneManager.GetActiveScene().name == "inGame" && TowerRegistry.ActiveTowers.Count() == 7, "Title starts a fresh game with clean registries");
            Check(!failed, "No runtime errors");
            Debug.Log("RUSHITADEL_2D_VALIDATION_SUCCESS");
            EditorApplication.Exit(0);
        }

        private static EnemyController SpawnEnemy(Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Actors/Enemy.prefab");
            return Instantiate(prefab, position, Quaternion.identity).GetComponent<EnemyController>();
        }
        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Check(bool condition, string description)
        {
            if (!condition) throw new Exception("PORT TEST FAILED: " + description);
            Debug.Log("PASS: " + description);
        }
    }
}
#endif
