#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Rushitadel.Combat;
using Rushitadel.Player;
using Rushitadel.Presentation;
using Rushitadel.Progression;
using Rushitadel.Spawning;
using Rushitadel.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace Rushitadel
{
    internal static class ForwardPressureProbe
    {
        public static IEnumerator Run(PlayerController player, ForwardPressureController pressure)
        {
            var camera = pressure.GameplayCamera;
            var body = player.GetComponent<Rigidbody2D>();
            var health = player.GetComponent<PlayerHealth>();
            var loop = Object.FindFirstObjectByType<LoopingBattlefield>();
            var hud = Object.FindFirstObjectByType<GameplayHud>();
            health.Heal(1000f);
            Set(health, "invulnerableUntil", 0f);
            body.position = Vector2.zero;
            yield return new WaitForFixedUpdate();
            pressure.enabled = true;
            Vector3 cameraStart = camera.transform.position;
            Vector2 playerStart = body.position;
            Vector3 groundStart = loop.Panels[0].transform.position;
            float screenStart = camera.WorldToViewportPoint(body.position).y;
            Vector3 rearAnchor = camera.WorldToViewportPoint(pressure.RearHazard.transform.position);
            yield return new WaitForSeconds(0.4f);
            Check(camera.transform.position.y > cameraStart.y + 0.25f && pressure.DistanceTravelled > 0.25f,
                "Camera advances steadily in world space and records forward distance");
            Check(Vector2.Distance(body.position, playerStart) < 0.02f && camera.WorldToViewportPoint(body.position).y < screenStart - 0.02f,
                "Idle player stays in the world but falls behind on screen");
            Check(loop.Panels[0].transform.position == groundStart, "Ground is world-fixed: camera motion creates downward screen scroll");
            Check(Vector3.Distance(rearAnchor, camera.WorldToViewportPoint(pressure.RearHazard.transform.position)) < 0.0001f,
                "Rear pursuit stays camera-relative");
            Check(pressure.RearHazard.isTrigger && (pressure.FrontBarrier==null||!pressure.FrontBarrier.gameObject.activeInHierarchy),
                "Rear danger is a damage volume; front barrier is removed");

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            float beforeAdvance = body.position.y - camera.transform.position.y;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return new WaitForSeconds(0.5f);
            Check(body.position.y - camera.transform.position.y > beforeAdvance + 0.65f && health.CurrentHealth == health.MaximumHealth,
                "Forward input overtakes camera speed and escapes the rear threat");
            body.position = new Vector2(0f, camera.transform.position.y+3f);
            yield return new WaitForSeconds(0.3f);
            Check(body.position.y-camera.transform.position.y<=1.6f,"Camera follows forward player without barrier");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            player.ApplyRecoil(Vector2.down, 0.18f, 0.12f);
            yield return new WaitForSeconds(0.2f);
            Check(pressure.ClampPlayer(new Vector2(0,camera.transform.position.y+20),player.BodyRadius).y>camera.transform.position.y+19,
                "No upper barrier clamp");
            InputSystem.RemoveDevice(keyboard);

            body.position = new Vector2(0f, pressure.RearHazard.bounds.max.y - 0.2f);
            yield return new WaitForSeconds(0.55f);
            Check(pressure.InDanger && pressure.ExposureSeconds > 0.45f && health.CurrentHealth == health.MaximumHealth,
                "Rear hazard warns immediately and waits one second before first damage");
            health.TakeDamage(1f);
            Set(health, "invulnerableUntil", Time.time + 10f);
            float hp = health.CurrentHealth;
            yield return new WaitForSeconds(0.55f);
            Check(Mathf.Approximately(health.CurrentHealth, hp - health.MaximumHealth * 0.5f),
                "Rear hazard ticks for 50 percent maximum HP and ignores normal hit invulnerability");
            body.position = new Vector2(0f, camera.transform.position.y);
            yield return new WaitForSeconds(0.1f);
            Check(!pressure.InDanger && pressure.ExposureSeconds == 0f, "Leaving the hazard clears its exposure timer");
            health.Heal(1000f);
            Set(health, "invulnerableUntil", 0f);

            body.position = new Vector2(0f, pressure.RearHazard.bounds.max.y - 0.2f);
            yield return new WaitForSeconds(0.3f);
            hud.SetPaused(true);
            float pausedExposure = pressure.ExposureSeconds;
            float pausedDistance = pressure.DistanceTravelled;
            Vector3 pausedCamera = camera.transform.position;
            hp = health.CurrentHealth;
            yield return new WaitForSecondsRealtime(0.25f);
            Check(pressure.ExposureSeconds == pausedExposure && pressure.DistanceTravelled == pausedDistance &&
                camera.transform.position == pausedCamera && health.CurrentHealth == hp,
                "Pause freezes camera advance, pursuit exposure, damage and distance");
            hud.SetPaused(false);
            body.position = new Vector2(0f, camera.transform.position.y);
            yield return new WaitForSeconds(0.1f);

            var colors = new System.Collections.Generic.HashSet<Color>();
            foreach (PressureTheme theme in Enum.GetValues(typeof(PressureTheme)))
            {
                pressure.SetTheme(theme);
                Check(pressure.RearHazard.GetComponent<ThemePressureVisual>().FrameCount==12,"Twelve animated pursuit frames");
                Check(pressure.Theme == theme && !string.IsNullOrEmpty(ThemePressureVisual.HazardName(theme)),
                    "Theme pursuit profile available: " + theme);
            }
            Check(ThemePressureVisual.ThemeFolder(PressureTheme.Water)==ThemePressureVisual.ThemeFolder(PressureTheme.Electric),"Water and electricity share electric tsunami");
            pressure.SetTheme(PressureTheme.Electric);

            float savedY = camera.transform.position.y;
            foreach (float y in new[] { 10.79f, 10.81f, 1080.25f, 10000.25f })
            {
                camera.transform.position = new Vector3(0f, y, -10f);
                loop.RefreshLayout();
                Bounds[] panels = loop.Panels.Select(p => p.bounds).OrderBy(b => b.min.y).ToArray();
                bool covered = panels[0].min.y <= y - camera.orthographicSize && panels[^1].max.y >= y + camera.orthographicSize;
                bool adjacent = true;
                for (int i = 1; i < panels.Length; i++) adjacent &= Mathf.Abs(panels[i - 1].max.y - panels[i].min.y) < 0.002f;
                Check(covered && adjacent, "Repeated background covers view without gaps at world Y=" + y);
            }
            camera.transform.position = new Vector3(0f, savedY, -10f);
            loop.RefreshLayout();

            var bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/enemyProjectile.prefab");
            var bullet = Object.Instantiate(bulletPrefab, new Vector3(3f, camera.transform.position.y, 0f), Quaternion.identity).GetComponent<EnemyProjectileController>();
            Set(bullet, "speed", 0f);
            bullet.Launch(Vector2.up);
            Vector3 bulletStart = bullet.transform.position;
            yield return new WaitForSeconds(0.2f);
            Check(bullet.transform.position == bulletStart && bullet.BounceCount == 0,
                "Camera advance does not add world velocity or false ricochets to enemy bullets");
            Object.DestroyImmediate(bullet.gameObject);

            camera.transform.position = new Vector3(0f, 250f, -10f);
            pressure.RefreshAnchors();
            body.position = new Vector2(0f, 250f);
            var spawnerObject = new GameObject("Forward Spawn Test");
            spawnerObject.SetActive(false);
            var spawner = spawnerObject.AddComponent<EnemySpawner>();
            spawner.Configure(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RangedEnemy.prefab"));
            typeof(EnemySpawner).GetMethod("SpawnBatch", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(spawner, null);
            var spawned = EnemyRegistry.ActiveEnemies.ToArray();
            Check(spawned.Length == 2 && spawned.All(e => e.transform.position.y >= 251.5f && e.transform.position.y <= 255.5f && Mathf.Abs(e.transform.position.x) > PlayfieldBoundary.Instance.RightEdge),
                "Enemies spawn ahead of the advancing camera outside the cliffs");
            Object.DestroyImmediate(spawnerObject);
            int xp = player.GetComponent<PlayerExperience>().TotalExperience;
            spawned[0].transform.position = new Vector3(7.3f, pressure.Bottom - 2.2f, 0f);
            Object.DestroyImmediate(spawned[1].gameObject);
            yield return new WaitForSeconds(0.1f);
            Check(spawned[0] == null && player.GetComponent<PlayerExperience>().TotalExperience == xp,
                "Enemies passed behind camera are culled without XP so spawn capacity recovers");

            pressure.enabled = false;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            pressure.RefreshAnchors();
            loop.RefreshLayout();
            body.position = new Vector2(0f, -1f);
            health.Heal(1000f);
            Set(health, "invulnerableUntil", 0f);
            yield return new WaitForFixedUpdate();
        }

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("FORWARD PRESSURE TEST FAILED: " + message);
            Debug.Log("PASS: " + message);
        }
    }
}
#endif
