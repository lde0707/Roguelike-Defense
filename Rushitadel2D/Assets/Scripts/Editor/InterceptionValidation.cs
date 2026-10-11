#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rushitadel.Combat;
using Rushitadel.Progression;

namespace Rushitadel.Editor
{
    public static class InterceptionValidation
    {
        const string Key = "Rushitadel.InterceptionValidation";
        static void Check(bool condition, string message)
        { if (!condition) throw new Exception(message); Debug.Log("PASS: " + message); }
        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }
        [InitializeOnLoadMethod] static void Hook()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                SessionState.SetBool(Key, false);
                EditorApplication.update += RunOnce;
            };
        }
        static void RunOnce()
        {
            EditorApplication.update -= RunOnce;
            Validate();
        }
        static EnemyController Enemy(int xp = 20)
        { var e = new GameObject("Test enemy").AddComponent<EnemyController>(); e.ConfigureRun(100, xp, true); return e; }
        static EnemyProjectileController Bullet(EnemyController owner, float hp = 24)
        {
            var b = new GameObject("Test hostile").AddComponent<EnemyProjectileController>();
            b.ConfigureBoss(hp, 0, .1f, 7, 0); b.SetRewardOwner(owner); b.Launch(Vector2.up); return b;
        }
        static ProjectileController Friendly(bool player = true, int pierce = 0)
        {
            var p = new GameObject("Test friendly").AddComponent<ProjectileController>();
            p.Launch(Vector2.up, 30, 10); p.SetModifiers(pierce, false, false);
            if (player) p.EnablePlayerWeb(); return p;
        }
        static int PickupXp()
        {
            int sum = 0;
            foreach (var p in UnityEngine.Object.FindObjectsByType<ExperiencePickup>(FindObjectsSortMode.None))
                sum += (int)typeof(ExperiencePickup).GetField("experienceValue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(p);
            return sum;
        }
        static void Clear()
        { foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(go); }
        static void Validate()
        {
            try
            {
                Time.timeScale = 1;
                var e = Enemy();
                for (int i = 0; i < 20; i++) Bullet(e).TakeDamage(100);
                Check(e.InterceptionXpPaid == 8 && PickupXp() == 8, "Interception budget capped at 40%, batched pickups");
                e.Eliminate(); Check(PickupXp() == 20, "Interception + kill conserve total XP");
                Bullet(e).TakeDamage(100); Check(PickupXp() == 20, "Post-death bullets cannot duplicate rewards"); Clear();
                e = Enemy(); e.Eliminate(); Check(PickupXp() == 20, "Immediate kill pays all unclaimed XP"); Clear();
                e = Enemy(); var b = Bullet(e); b.Despawn(); b.TakeDamage(100);
                Check(PickupXp() == 0, "Cleanup and expired bullets grant no XP");
                b = Bullet(e); Time.timeScale = 0; b.TakeDamage(100);
                Check(b.IsAlive && PickupXp() == 0, "Paused damage grants no XP"); Time.timeScale = 1;
                b.TakeDamage(100); b.TakeDamage(100); Check(e.InterceptionXpPaid == 0, "Duplicate hit does not count twice");
                Bullet(e).TakeDamage(100); Check(PickupXp() == 2, "Two light bullets emit one 2 XP pickup"); Clear();
                e = Enemy(35); for (int i=0;i<20;i++) Bullet(e, 90).TakeDamage(200);
                Check(PickupXp() == 14, "Heavy bullets earn more credit within elite budget");
                e.Eliminate(); Check(PickupXp() == 35, "Elite total reward preserved"); Clear();
                var p = Friendly(); b = Bullet(null); p.Hit(b);
                Check(p.IsFlying && !b.IsAlive && !p.CanHit(b), "Player breaks one light bullet and continues without repeat hit");
                p.Hit(Bullet(null)); Check(!p.IsFlying, "Free interception pierce limited to one");
                p=Friendly(); b=Bullet(null,40); p.Hit(b); Check(!p.IsFlying && b.IsAlive, "Surviving bullet blocks basic shot");
                p=Friendly(); b=Bullet(null,60); b.TakeDamage(40); p.Hit(b);
                Check(!p.IsFlying && !b.IsAlive, "Heavy bullet kill does not grant free pierce");
                p=Friendly(false); p.Hit(Bullet(null)); Check(!p.IsFlying, "Turret shots do not get player-only pierce");
                p=Friendly(true,1); p.Hit(Bullet(null)); p.Hit(Bullet(null)); Check(p.IsFlying, "Augment pierce stacks with free interception");
                p.Hit(Bullet(null)); Check(!p.IsFlying, "Augment pierce still has finite hit count");
                Check(!EnemyRangedAttack.IsReloadWindow(2.79f) && EnemyRangedAttack.IsReloadWindow(2.81f) &&
                    EnemyRangedAttack.IsReloadWindow(4.19f) && !EnemyRangedAttack.IsReloadWindow(4.21f), "Shared 2.8s attack / 1.4s reload cycle");
                Clear();
                var circuit = new WaveVolley();
                var core = Bullet(null); var escort = Bullet(null, 70);
                circuit.Add(core, true); circuit.Add(escort, false);
                core.TakeDamage(100);
                Check(circuit.Broken && escort.CounterArmed && Mathf.Approximately(escort.CurrentHealth,35), "Core kill halves its own formation HP and arms reflection");
                circuit.BreakCore(); Check(escort.CurrentHealth == 35, "Core break cannot halve HP repeatedly");
                Check(circuit.ClaimReturn() && circuit.ClaimReturn() && circuit.ClaimReturn() && !circuit.ClaimReturn(), "Formation limited to three counter ricochets");
                var expired = new WaveVolley(); core=Bullet(null); escort=Bullet(null);
                expired.Add(core,true); expired.Add(escort,false); core.Despawn();
                Check(!expired.Broken && !escort.CounterArmed, "Despawn never triggers counter circuit");
                Clear();
                var boundary = new GameObject("Test boundaries").AddComponent<PlayfieldBoundary>();
                var left = new GameObject("Left wall").AddComponent<BoxCollider2D>(); left.transform.position=new Vector3(-1.5f,0,0);left.size=new Vector2(1,20);
                var right = new GameObject("Right wall").AddComponent<BoxCollider2D>(); right.transform.position=new Vector3(1.5f,0,0);right.size=new Vector2(1,20);
                boundary.Configure(left,right); Physics2D.SyncTransforms();
                circuit=new WaveVolley(); core=Bullet(null); core.transform.position=new Vector3(0,5,0); escort=Bullet(null);
                escort.Launch(Vector2.right);circuit.Add(core,true);circuit.Add(escort,false);core.TakeDamage(100);
                typeof(EnemyProjectileController).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(escort,new object[]{2f});
                var reflectedShot=UnityEngine.Object.FindAnyObjectByType<ProjectileController>();
                Check(!escort.IsAlive && reflectedShot!=null && reflectedShot.Direction.x<-.99f && circuit.ReturnCount==1,
                    "Actual wall contact creates friendly reflected projectile and removes hostile");
                UnityEngine.Object.DestroyImmediate(reflectedShot.gameObject);
                var emitter=Enemy(); emitter.transform.position=new Vector3(-2,3,0);
                circuit=new WaveVolley(); core=Bullet(emitter);core.transform.position=new Vector3(0,5,0);escort=Bullet(emitter);
                escort.Launch(Vector2.right);circuit.Add(core,true);circuit.Add(escort,false);core.TakeDamage(100);
                typeof(EnemyProjectileController).GetMethod("Advance",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(escort,new object[]{2f});
                reflectedShot=UnityEngine.Object.FindAnyObjectByType<ProjectileController>();
                Check(reflectedShot!=null && reflectedShot.Direction.x<0 && reflectedShot.Direction.y>0,
                    "Counter ricochet locks emitter position once instead of continuing downward");
                for(int pattern=0;pattern<5;pattern++)
                {
                    int count=WavePattern.Count(pattern,true);
                    Check(count>=3 && count<=7, "Pattern bullet budget " + pattern);
                    for(int i=0;i<count;i++)
                    {
                        Vector2 heading=WavePattern.Direction(Vector2.down,pattern,i,count,3);
                        Check(Mathf.Abs(heading.magnitude-1)<.001f && Vector2.Dot(heading,Vector2.down)>.5f,
                            "Readable forward heading " + pattern + "/" + i);
                    }
                }
                Debug.Log("INTERCEPTION VALIDATION SUCCESS"); EditorApplication.Exit(0);
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }
    }
}
#endif
