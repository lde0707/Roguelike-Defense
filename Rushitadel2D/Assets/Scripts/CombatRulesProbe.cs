#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Rushitadel.Combat;
using Rushitadel.Player;
using Rushitadel.Power;
using Rushitadel.Progression;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace Rushitadel
{
    internal static class CombatRulesProbe
    {
        public static IEnumerator Run(PlayerController player)
        {
            var boundary = PlayfieldBoundary.Instance;
            var health = player.GetComponent<PlayerHealth>();
            var body = player.GetComponent<Rigidbody2D>();
            var power = player.GetComponent<PlayerPower>();
            var attack = player.GetComponent<PlayerPowerShotController>();
            var walls = boundary.GetComponentsInChildren<BoxCollider2D>();
            Check(walls.Length == 2 && walls.All(w => !w.isTrigger) &&
                boundary.GetComponentInChildren<Renderer>() == null, "Cliffs have two solid invisible player walls");
            Check(Mathf.Approximately(boundary.LeftEdge, -5.4f) && Mathf.Approximately(boundary.RightEdge, 5.4f),
                "Cliff collision faces match the current battlefield path edges");

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            body.position = new Vector2(boundary.RightEdge - player.BodyRadius - 0.03f, -2f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            yield return new WaitForSeconds(0.25f);
            Check(body.position.x + player.BodyRadius <= boundary.RightEdge + 0.005f, "Player collider cannot cross the right cliff");
            body.position = new Vector2(boundary.LeftEdge + player.BodyRadius + 0.03f, -2f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
            yield return new WaitForSeconds(0.25f);
            Check(body.position.x - player.BodyRadius >= boundary.LeftEdge - 0.005f, "Player collider cannot cross the left cliff");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            player.ApplyRecoil(Vector2.right, 0.18f, 0.12f);
            yield return new WaitForSeconds(0.2f);
            Check(body.position.x - player.BodyRadius >= boundary.LeftEdge - 0.005f, "Recoil also stops at the invisible cliff wall");
            InputSystem.RemoveDevice(keyboard);
            body.position = new Vector2(0f, -2.5f);
            yield return new WaitForFixedUpdate();

            foreach (int side in new[] { -1, 1 })
            {
                float edge = side < 0 ? boundary.LeftEdge : boundary.RightEdge;
                var bullet = Bullet(new Vector2(edge + side, 3f), new Vector2(-side, 0f));
                Advance(bullet, 1.8f);
                Check(boundary.Contains(bullet.transform.position) && bullet.BounceCount == 0,
                    $"Enemy bullet enters from {(side < 0 ? "left" : "right")} without reflection");
                Advance(bullet, 10f);
                Check(bullet.BounceCount == 1 && Mathf.Sign(bullet.Direction.x) == side && boundary.Contains(bullet.transform.position),
                    "The same bullet reflects only when leaving the far cliff");
                Advance(bullet, 0.01f);
                Check(bullet.BounceCount == 1, "Reflected bullet does not stick to or retrigger the wall");
                Object.DestroyImmediate(bullet.gameObject);
            }

            var fast = Bullet(new Vector2(boundary.LeftEdge - 1f, 3f), Vector2.right);
            Advance(fast, 40f);
            Check(fast.BounceCount >= 3 && boundary.Contains(fast.transform.position), "Fast bullet can enter and ricochet repeatedly within one frame");
            Object.DestroyImmediate(fast.gameObject);
            var away = Bullet(new Vector2(boundary.LeftEdge - 1f, 3f), Vector2.left);
            Advance(away, 2f);
            Check(away.BounceCount == 0, "Bullet outside the lane moving away does not ricochet");
            Object.DestroyImmediate(away.gameObject);
            var parallel = Bullet(new Vector2(2f, 2f), Vector2.up);
            Advance(parallel, 1f);
            Check(parallel.BounceCount == 0 && Mathf.Approximately(parallel.transform.position.x, 2f), "Parallel bullet has no false wall collision");
            Object.DestroyImmediate(parallel.gameObject);

            var oblique = Bullet(new Vector2(boundary.RightEdge - 0.5f, 2f), new Vector2(1f, 0.3f));
            Vector2 incoming = oblique.Direction;
            Advance(oblique, 0.6f);
            Check(oblique.BounceCount == 1 && Mathf.Approximately(oblique.Direction.x, -incoming.x) &&
                Mathf.Approximately(oblique.Direction.y, incoming.y), "Ricochet preserves tangential velocity and reflects the normal component");
            Object.DestroyImmediate(oblique.gameObject);

            int pickupCount = Object.FindObjectsByType<ExperiencePickup>(FindObjectsSortMode.None).Length;
            var durable = Bullet(new Vector2(3f, 3f), Vector2.down);
            durable.TakeDamage(10f);
            Check(durable.CurrentHealth == 15f && durable.IsAlive, "Hostile projectile has HP and survives partial damage");
            durable.TakeDamage(15f);
            durable.TakeDamage(100f);
            yield return null;
            Check(durable == null && Object.FindObjectsByType<ExperiencePickup>(FindObjectsSortMode.None).Length == pickupCount,
                "Destroyed projectile is removed once and grants no enemy XP");

            var shellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/towerProjectile.prefab");
            var outsideEnemy = Enemy(new Vector3(boundary.RightEdge + 0.8f, 1f, 0f));
            var crossing = Object.Instantiate(shellPrefab, new Vector3(boundary.RightEdge - 0.5f, 1f, 0f), Quaternion.identity).GetComponent<ProjectileController>();
            Set(crossing, "speed", 1000f);
            crossing.Launch(Vector3.right, 25f, 5f);
            yield return new WaitForSeconds(0.05f);
            Check(outsideEnemy.CurrentHealth == 175f, "Friendly shot crosses the cliff to damage outside enemies");
            Object.DestroyImmediate(outsideEnemy.gameObject);
            var intercept = Bullet(new Vector2(2f, 3f), Vector2.left);
            var shell = Object.Instantiate(shellPrefab, new Vector3(1f, 3f, 0f), Quaternion.identity).GetComponent<ProjectileController>();
            Set(shell, "speed", 1000f);
            shell.Launch(Vector3.right, 25f, 5f);
            yield return new WaitForSeconds(0.05f);
            Check(intercept == null && shell == null, "Swept friendly shell destroys hostile bullet on first contact");

            var counter = Bullet(new Vector2(3f, 3f), Vector2.left);
            var stationary = Object.Instantiate(shellPrefab, new Vector3(2f, 3f, 0f), Quaternion.identity).GetComponent<ProjectileController>();
            Set(stationary, "speed", 0f);
            stationary.Launch(Vector3.up, 10f, 5f);
            Advance(counter, 2f);
            Check(counter.CurrentHealth == 15f && !stationary.IsFlying, "Moving hostile bullet detects an incoming friendly shell and loses its damage amount");
            Object.DestroyImmediate(counter.gameObject);
            yield return null;

            var tower = player.GetComponentsInChildren<TowerController>()[0];
            var turretTarget = Bullet(tower.transform.position + tower.transform.up * 1.5f, Vector2.down);
            Set(tower, "nextShotTime", 0f);
            tower.enabled = true;
            yield return new WaitForSeconds(0.25f);
            tower.enabled = false;
            Check(turretTarget == null, "Attached automatic turret can destroy an enemy projectile");
            var mouseTarget = Bullet(player.transform.position + Vector3.right * 2f, Vector2.down);
            Check(player.GetComponent<PlayerAttackController>().TryFire(Camera.main.WorldToScreenPoint(mouseTarget.transform.position)),
                "Player can aim basic fire at an enemy projectile");
            yield return new WaitForSeconds(0.3f);
            Check(mouseTarget == null, "Player basic attack destroys enemy projectile HP");

            health.Heal(100f);
            Set(health, "invulnerableUntil", 0f);
            var danger = Bullet(player.transform.position + Vector3.left * 2f, Vector2.right);
            Advance(danger, 3f);
            Check(health.CurrentHealth == health.MaximumHealth - 10f && !danger.IsAlive, "Enemy projectile sweeps into player, damages once and disappears");
            yield return null;
            health.Heal(100f);

            Check(Mathf.Approximately(PlayerProjectileController.DamageFraction(0f), 1f) &&
                Mathf.Approximately(PlayerProjectileController.DamageFraction(0.3f), 1f) &&
                Mathf.Approximately(PlayerProjectileController.DamageFraction(0.65f), 0.75f) &&
                Mathf.Approximately(PlayerProjectileController.DamageFraction(1f), 0.5f),
                "Radial damage is 100 percent in inner 30 percent, falling to 50 percent at rim");
            float radius = attack.AttackRadius;
            Check(Mathf.Approximately(radius, player.BodyRadius * 1.5f), "Power area diameter is 150 percent of player body diameter");
            Vector3 target = player.transform.position + Vector3.up * 2f;
            var center = Enemy(target + Vector3.left * (radius * 0.3f));
            var middle = Enemy(target + Vector3.right * (radius * 0.65f));
            var rim = Enemy(target + Vector3.up * radius);
            var outside = Enemy(target + Vector3.down * (radius * 1.02f));
            // Proves proportional damage uses MAXIMUM HP, not current HP.
            Set(middle, "currentHealth", 180f);
            var centerBullet = Bullet(target + Vector3.down * (radius * 0.2f), Vector2.down);
            var rimBullet = Bullet(target + Vector3.left * radius, Vector2.down);
            Set(power, "maximumPower", 200f);
            Set(power, "currentPower", 200f);
            Vector2 aim = Camera.main.WorldToScreenPoint(target);
            Vector2 before = body.position;
            Check(attack.TryFire(aim) && Mathf.Approximately(power.CurrentPower, 60f), "Right click spends exactly 70 percent of maximum power (140 of 200)");
            var orb = Object.FindFirstObjectByType<PlayerProjectileController>();
            Check(Mathf.Approximately(orb.ImpactRadius, radius) && Mathf.Approximately(orb.transform.localScale.x, radius * 2f),
                "Visible power orb matches its actual damage radius");
            Check(!attack.TryFire(aim) && Mathf.Approximately(power.CurrentPower, 60f), "Insufficient power blocks another shot without spending");
            yield return new WaitForSeconds(0.15f);
            Check(before.y - body.position.y > 0.12f && before.y - body.position.y < 0.22f && Mathf.Abs(body.position.x - before.x) < 0.01f,
                "Right click produces a brief small recoil opposite aim");
            yield return new WaitForSeconds(0.3f);
            Check(center == null && middle.CurrentHealth > 29.9f && middle.CurrentHealth < 30.1f,
                "Inner target instantly dies; outer target takes maximum-HP proportional falloff damage");
            Check(Mathf.Abs(rim.CurrentHealth - 100f) < 0.1f && outside.CurrentHealth == 200f,
                "Power shot deals exactly 50 percent max HP at rim and nothing outside");
            Check(centerBullet == null && Mathf.Abs(rimBullet.CurrentHealth - 12.5f) < 0.1f,
                "Power area destroys central enemy bullets and damages edge bullet HP");
            Vector2 after = body.position;
            yield return new WaitForSeconds(0.15f);
            Check(Vector2.Distance(after, body.position) < 0.01f, "Recoil ends without persistent drift");
            foreach (var enemy in new[] { middle, rim, outside }) Object.DestroyImmediate(enemy.gameObject);
            Object.DestroyImmediate(rimBullet.gameObject);
            yield return new WaitForSeconds(0.5f); // Let XP from the central kill finish collecting.
            Set(power, "maximumPower", 100f);
            Set(power, "currentPower", 100f);

            Mouse mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = aim, buttons = 2 });
            yield return new WaitForSeconds(0.05f);
            Check(Mathf.Approximately(power.CurrentPower, 30f) && Object.FindFirstObjectByType<PlayerProjectileController>() != null,
                "Actual right mouse input launches the area attack and consumes 70 of 100 power");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = aim });
            yield return null;
            InputSystem.RemoveDevice(mouse);
            yield return new WaitForSeconds(0.4f);
            Set(power, "currentPower", 70f);
            Check(attack.TryFire(aim) && power.CurrentPower == 0f && power.State == PowerCycleState.Recovering,
                "Exactly 70 percent power can fire and enters recovery at zero");
            Set(power, "currentPower", 60f);
            yield return null;
            Check(power.State == PowerCycleState.Ready && !attack.TryFire(aim), "Recovery unlock at 60 percent does not bypass 70 percent firing cost");
            Set(power, "currentPower", 100f);
            yield return new WaitForSeconds(0.4f);

            var paused = Bullet(new Vector2(3f, 3f), Vector2.left);
            Set(paused, "speed", 4f);
            Vector3 pausedPosition = paused.transform.position;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.15f);
            Check(paused.transform.position == pausedPosition, "Pause freezes enemy projectile movement and lifetime");
            Time.timeScale = 1f;
            Set(paused, "remainingLifetime", 0.01f);
            yield return new WaitForSeconds(0.05f);
            Check(paused == null, "Hostile projectile expires to keep repeated ricochets bounded");
        }

        private static EnemyProjectileController Bullet(Vector2 position, Vector2 direction)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/enemyProjectile.prefab");
            var bullet = Object.Instantiate(prefab, position, Quaternion.identity).GetComponent<EnemyProjectileController>();
            Set(bullet, "speed", 0f);
            bullet.Launch(direction);
            return bullet;
        }
        private static EnemyController Enemy(Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
            var enemy = Object.Instantiate(prefab, position, Quaternion.identity).GetComponent<EnemyController>();
            Set(enemy, "moveSpeed", 0f);
            Set(enemy, "contactDamage", 0f);
            Set(enemy, "maxHealth", 200f);
            Set(enemy, "currentHealth", 200f);
            return enemy;
        }
        private static void Advance(EnemyProjectileController bullet, float distance) =>
            typeof(EnemyProjectileController).GetMethod("Advance", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bullet, new object[] { distance });
        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("COMBAT RULE TEST FAILED: " + message);
            Debug.Log("PASS: " + message);
        }
    }
}
#endif
