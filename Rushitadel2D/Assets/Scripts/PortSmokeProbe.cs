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
using Rushitadel.World;
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
            var pressure = FindFirstObjectByType<ForwardPressureController>();
            pressure.enabled = false; // Isolate existing combat tests; the full scrolling system is exercised below.
            pressure.GameplayCamera.transform.position = new Vector3(0f, 0f, -10f);
            pressure.RefreshAnchors();
            yield return new WaitForSeconds(1f);
            Check(EnemyRegistry.ActiveEnemies.Any(), "Automatic enemy spawning");
            Check(FindObjectsByType<EnemyRangedAttack>(FindObjectsSortMode.None).Length > 0 &&
                FindObjectsByType<EnemyProjectileController>(FindObjectsSortMode.None).Length > 0,
                "Outside ranged enemies automatically launch hostile projectiles");
            var player = FindFirstObjectByType<PlayerController>();
            var health = player.GetComponent<PlayerHealth>();
            var rig = player.GetComponent<PlayerTurretRig>();
            var towers = player.GetComponentsInChildren<TowerController>().OrderBy(t => t.SlotId).ToArray();
            Check(towers.Length == 5 && towers.Select(t => t.SlotId).SequenceEqual(new[] { 1, 2, 3, 4, 5 }), "Five attached stable slots T1-T5");
            Check(FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None).Length == 1, "One player survival HP owner");
            float angle = rig.RotatingBody.eulerAngles.z;
            yield return new WaitForSeconds(0.2f);
            Check(Mathf.Abs(Mathf.DeltaAngle(angle, rig.RotatingBody.eulerAngles.z)) > 2f, "Automatic body rotation");
            CapturePreview();
            Destroy(FindFirstObjectByType<EnemySpawner>());
            foreach (TowerController tower in towers) tower.enabled = false;
            foreach (EnemyController enemy in EnemyRegistry.ActiveEnemies.ToArray()) Destroy(enemy.gameObject);
            foreach (ProjectileBehaviour shot in FindObjectsByType<ProjectileBehaviour>(FindObjectsSortMode.None)) Destroy(shot.gameObject);
            yield return null;

            Vector3 start = player.transform.position;
            Vector3 localSlot = towers[0].transform.localPosition;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
            yield return new WaitForSeconds(0.3f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForFixedUpdate();
            Vector3 delta = player.transform.position - start;
            Check(delta.x > 0.2f && delta.y > 0.2f && Mathf.Abs(delta.z) < 0.001f && delta.magnitude < 1f, "Normalized WASD movement in XY");
            Check(towers[0].transform.localPosition == localSlot && towers.All(t => t.transform.IsChildOf(player.transform)), "Turrets move with player without slot drift");
            InputSystem.RemoveDevice(keyboard);
            rig.enabled = false;

            TowerController active = towers[0];
            Check(towers.All(t => Mathf.Approximately(t.AttackRange, 3f)), "All five turret ranges are 3 world units");
            SetField(active, "nextShotTime", 0f);
            Vector3 forward = active.transform.up;
            active.enabled = true;
            yield return null;
            active.enabled = false;
            var emptyShot = FindObjectsByType<ProjectileController>(FindObjectsSortMode.None).Single();
            Check(Vector3.Dot(emptyShot.Direction, forward) > 0.999f, "Turret fires forward with no enemies present");
            Vector3 shotStart = emptyShot.transform.position;
            rig.RotatingBody.Rotate(0f, 0f, 40f);
            yield return new WaitForSeconds(0.1f);
            Check(emptyShot != null && Vector3.Dot(emptyShot.Direction, forward) > 0.999f &&
                Vector3.Cross(emptyShot.transform.position - shotStart, forward).sqrMagnitude < 0.001f,
                "Already fired shell keeps its direction while the rig rotates");
            Destroy(emptyShot.gameObject);
            yield return null;

            EnemyController front = SpawnEnemy(active.transform.position + active.transform.up * 1.5f);
            EnemyController back = SpawnEnemy(active.transform.position - active.transform.up);
            SetField(front, "moveSpeed", 0f);
            SetField(back, "moveSpeed", 0f);
            SetField(back, "contactDamage", 0f);
            SetField(active, "nextShotTime", 0f);
            active.enabled = true;
            yield return new WaitForSeconds(0.45f);
            active.enabled = false;
            Check(front.CurrentHealth < 100f && back.CurrentHealth == 100f, "Forward shell hits an enemy on its path and ignores enemy behind turret");
            Destroy(front.gameObject);
            Destroy(back.gameObject);
            foreach (ProjectileBehaviour shot in FindObjectsByType<ProjectileBehaviour>(FindObjectsSortMode.None)) Destroy(shot.gameObject);
            yield return null;

            var shellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/towerProjectile.prefab");
            EnemyController firstOnPath = SpawnEnemy(new Vector3(20f, 1.5f, 0f));
            EnemyController secondOnPath = SpawnEnemy(new Vector3(20f, 3f, 0f));
            SetField(firstOnPath, "moveSpeed", 0f);
            SetField(secondOnPath, "moveSpeed", 0f);
            var fastShell = Instantiate(shellPrefab, new Vector3(20f, 0f, 0f), Quaternion.identity).GetComponent<ProjectileController>();
            SetField(fastShell, "speed", 1000f);
            fastShell.Launch(Vector3.up, 25f, 5f);
            yield return new WaitForSeconds(0.05f);
            Check(firstOnPath.CurrentHealth == 75f && secondOnPath.CurrentHealth == 100f && fastShell == null,
                "Swept collision hits the first enemy once without tunnelling");
            Destroy(firstOnPath.gameObject);
            Destroy(secondOnPath.gameObject);

            EnemyController beyondRange = SpawnEnemy(new Vector3(26f, 0f, 0f));
            SetField(beyondRange, "moveSpeed", 0f);
            var limited = Instantiate(shellPrefab, new Vector3(20f, 0f, 0f), Quaternion.identity).GetComponent<ProjectileController>();
            SetField(limited, "speed", 1000f);
            limited.Launch(Vector3.right, 25f, 3f);
            yield return new WaitForSeconds(0.05f);
            Check(limited == null && Mathf.Approximately(limited.DistanceTravelled, 3f) && beyondRange.CurrentHealth == 100f,
                "Shell stops at exactly 3 units and cannot hit enemy beyond range");
            Destroy(beyondRange.gameObject);

            var basicAttack = player.GetComponent<PlayerAttackController>();
            EnemyController mouseTarget = SpawnEnemy(player.transform.position + Vector3.right * 2f);
            SetField(mouseTarget, "moveSpeed", 0f);
            Vector2 mouseAim = Camera.main.WorldToScreenPoint(mouseTarget.transform.position);
            float powerBeforeBasic = player.GetComponent<PlayerPower>().CurrentPower;
            Check(basicAttack.TryFire(mouseAim) && !basicAttack.TryFire(mouseAim), "Mouse aimed attack fires and respects cooldown");
            var basicShot = FindObjectsByType<ProjectileController>(FindObjectsSortMode.None).Single();
            Check(Vector3.Dot(basicShot.Direction, Vector3.right) > 0.999f, "Player aim follows cursor independently of turret rotation");
            Check(player.GetComponent<PlayerPower>().CurrentPower == powerBeforeBasic, "Basic mouse attack does not spend power");
            yield return new WaitForSeconds(0.3f);
            Check(mouseTarget.CurrentHealth == 75f, "Player basic projectile deals collision damage");
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = mouseAim, buttons = 1 });
            yield return new WaitForSeconds(0.2f);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = mouseAim });
            yield return new WaitForSeconds(0.1f);
            Check(mouseTarget.CurrentHealth <= 50f, "Actual left mouse input fires a player projectile");
            InputSystem.RemoveDevice(mouse);
            Destroy(mouseTarget.gameObject);
            foreach (ProjectileBehaviour shot in FindObjectsByType<ProjectileBehaviour>(FindObjectsSortMode.None)) Destroy(shot.gameObject);
            yield return null;

            health.Heal(100f);
            EnemyController attacker = SpawnEnemy(player.transform.position + Vector3.right * 2f);
            float distance = Vector3.Distance(attacker.transform.position, player.transform.position);
            yield return new WaitForSeconds(0.25f);
            Check(Vector3.Distance(attacker.transform.position, player.transform.position) < distance, "Enemy pursues player instead of turret slots");
            attacker.transform.position = player.transform.position + Vector3.right * 0.5f;
            float hp = health.CurrentHealth;
            yield return new WaitForSeconds(0.05f);
            Check(health.CurrentHealth == hp - 10f, "Enemy contact damages player HP");
            health.TakeDamage(10f);
            Check(health.CurrentHealth == hp - 10f, "Hit invulnerability prevents same-frame damage stacking");
            Destroy(attacker.gameObject);
            health.Heal(1000f);
            Check(health.CurrentHealth == health.MaximumHealth, "Healing clamps at maximum HP");

            var power = player.GetComponent<PlayerPower>();
            Check(power.TrySpend(power.CurrentPower), "Power can reach zero");
            Check(power.State == PowerCycleState.Recovering && !power.TrySpend(1f), "Zero power locks power attack");
            yield return new WaitForSeconds(0.25f);
            Check(power.CurrentPower > 0f && !power.TrySpend(0.1f), "Recovery remains locked below 60 percent");
            SetField(power, "recoveryPerSecond", 600f);
            yield return new WaitForSeconds(0.2f);
            Check(power.State == PowerCycleState.Ready && power.CurrentPower <= power.MaximumPower, "Power unlocks at threshold and clamps at maximum");
            SetField(power, "recoveryPerSecond", 0f);
            yield return CombatRulesProbe.Run(player);
            EnemyController near = SpawnEnemy(player.transform.position + Vector3.up * 2f);
            int xp = player.GetComponent<PlayerExperience>().TotalExperience;
            near.TakeDamage(200f);
            yield return new WaitForSeconds(1f);
            Check(player.GetComponent<PlayerExperience>().TotalExperience == xp + 20, "Enemy death awards and collects XP once");

            rig.enabled = true;
            var hud = FindFirstObjectByType<Rushitadel.Presentation.GameplayHud>();
            hud.SetPaused(true);
            angle = rig.RotatingBody.eulerAngles.z;
            float pausedPower = power.CurrentPower;
            int pausedXp = player.GetComponent<PlayerExperience>().TotalExperience;
            var pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EXP.prefab");
            Instantiate(pickupPrefab, player.transform.position, Quaternion.identity);
            yield return new WaitForSecondsRealtime(0.2f);
            Check(Time.timeScale == 0f && rig.RotatingBody.eulerAngles.z == angle && power.CurrentPower == pausedPower, "Pause freezes combat and resources");
            Check(player.GetComponent<PlayerExperience>().TotalExperience == pausedXp, "Pause prevents XP collection even inside collection radius");
            Check(!power.TrySpend(1f), "Pause blocks power spending");
            Check(!basicAttack.TryFire(mouseAim), "Pause blocks basic mouse attack");
            Check(!player.GetComponent<PlayerPowerShotController>().TryFire(mouseAim), "Pause blocks right mouse attack");
            hud.SetPaused(false);
            Check(Time.timeScale == 1f, "Resume restores time");
            yield return new WaitForSeconds(0.1f);
            Check(player.GetComponent<PlayerExperience>().TotalExperience == pausedXp + 20, "Resume collects pending XP once");

            yield return ForwardPressureProbe.Run(player, pressure);

            int deaths = 0;
            health.Died += () => deaths++;
            health.TakeEnvironmentalDamage(10000f);
            health.TakeEnvironmentalDamage(10000f);
            hud.SetPaused(false);
            Check(!health.IsAlive && Time.timeScale == 0f && deaths == 1 && health.Heal(100f) == 0f, "Environmental HP zero causes one defeat and cannot resume or heal");
            Check(!basicAttack.TryFire(mouseAim), "Defeat blocks basic mouse attack");
            Check(!player.GetComponent<PlayerPowerShotController>().TryFire(mouseAim), "Defeat blocks right mouse attack");
            SceneManager.LoadScene("Title");
            yield return new WaitForSecondsRealtime(0.5f);
            var buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
            Check(buttons.Length == 4 && Time.timeScale == 1f, "Title restores time and menu");
            Check(!buttons.First(button => button.name == "ContinueButton").interactable, "Continue unavailable without checkpoint persistence");
            buttons.First(button => button.name == "NewGameButton").onClick.Invoke();
            yield return new WaitForSeconds(0.3f);
            Check(SceneManager.GetActiveScene().name == "inGame" && TowerRegistry.ActiveTowers.Count() == 5, "New game resets five-turret registry");
            Check(PlayerHealth.Instance.IsAlive && PlayerHealth.Instance.CurrentHealth == PlayerHealth.Instance.MaximumHealth, "New game resets HP");
            Check(ForwardPressureController.Instance.enabled && ForwardPressureController.Instance.DistanceTravelled < 1f &&
                ForwardPressureController.Instance.ExposureSeconds == 0f, "New game resets pursuit distance and hazard timer");
            Destroy(FindFirstObjectByType<EnemySpawner>());
            foreach (var enemy in EnemyRegistry.ActiveEnemies.ToArray()) Destroy(enemy.gameObject);
            foreach (var shot in FindObjectsByType<ProjectileBehaviour>(FindObjectsSortMode.None)) Destroy(shot.gameObject);
            foreach (var tower in FindObjectsByType<TowerController>(FindObjectsSortMode.None)) tower.enabled = false;
            var idleKeyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(idleKeyboard, new KeyboardState());
            yield return new WaitForSecondsRealtime(6.5f);
            Check(!PlayerHealth.Instance.IsAlive && Time.timeScale == 0f, "Standing still from new-game spawn naturally leads to pursuit-hazard defeat");
            Vector3 defeatedCamera = Camera.main.transform.position;
            yield return new WaitForSecondsRealtime(0.1f);
            Check(Camera.main.transform.position == defeatedCamera, "Defeat freezes the advancing camera");
            InputSystem.RemoveDevice(idleKeyboard);
            SceneManager.LoadScene("inGame");
            yield return new WaitForSecondsRealtime(0.3f);
            Check(PlayerHealth.Instance.IsAlive && Time.timeScale == 1f && ForwardPressureController.Instance.DistanceTravelled < 1f,
                "Restart after pursuit defeat resets player, camera advance and distance");
            CapturePreview();
            Check(!failed, "No runtime errors");
            Debug.Log("RUSHITADEL_2D_VALIDATION_SUCCESS");
            EditorApplication.Exit(0);
        }

        private static void CapturePreview()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            Camera camera = Camera.main;
            var target = new RenderTexture(1280, 720, 24);
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            var picture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            picture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            picture.Apply();
            System.IO.File.WriteAllBytes("GameplayPreview.png", picture.EncodeToPNG());
            RenderTexture.active = null;
            Destroy(target);
            Destroy(picture);
        }

        private static EnemyController SpawnEnemy(Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
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
