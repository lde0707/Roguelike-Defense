#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rushitadel.Combat;
using Rushitadel.Player;
using Rushitadel.Presentation;
using Rushitadel.Spawning;
using Rushitadel.World;

namespace Rushitadel.Progression
{
    public sealed class BossIntegrationProbe:MonoBehaviour
    {
        private string output;
        private int checks;
        private void Check(bool value,string label){if(!value)throw new Exception("BOSS FAIL: "+label);checks++;Debug.Log("BOSS PASS: "+label);}
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../output/boss-integration-20261007"));Directory.CreateDirectory(output);
            RunStore.TestDirectory=Path.Combine(output,"test-saves");
            Application.logMessageReceived+=(msg,stack,type)=>{
                if(stack.Contains("UnityEditor.Search."))return;
                if(type==LogType.Exception||type==LogType.Error)EditorApplication.delayCall+=()=>EditorApplication.Exit(1);
            };
        }
        private IEnumerator Start()
        {
            yield return null;
            RunSession.Pending=new RunData{slot=1,seed=731,runId=Guid.NewGuid().ToString()};
            SceneManager.LoadScene("inGame");yield return null;yield return null;
            CampaignDirector.Instance.enabled=false;
            var pressure=ForwardPressureController.Instance;pressure.HoldAdvance=true;pressure.enabled=false;
            GameFrontend.Instance.ShowBattle();Time.timeScale=4;
            Check(pressure.FrontBarrier==null||!pressure.FrontBarrier.gameObject.activeInHierarchy,"front barrier removed");
            Check(pressure.ClampPlayer(new Vector2(0,pressure.GameplayCamera.transform.position.y+20),.5f).y>pressure.GameplayCamera.transform.position.y+19,"no upper movement clamp");
            var pursuit=pressure.RearHazard.GetComponent<ThemePressureVisual>();
            for(int theme=0;theme<4;theme++)
            {
                pressure.SetTheme((PressureTheme)theme);
                for(int frame=0;frame<12;frame++)Check(Resources.Load<Texture2D>("PressureArt/"+ThemePressureVisual.ThemeFolder((PressureTheme)theme)+"/"+frame.ToString("00"))!=null,"pursuit frame "+theme+"/"+frame);
                Check(pursuit.FrameCount==12,"twelve frame pursuit sequence");
                int initial=pursuit.FrameIndex;yield return new WaitForSeconds(.15f);
                Check(pursuit.FrameIndex!=initial,"pursuit animation advances");
                Capture("Pursuit-"+theme+".png");
            }
            var player=PlayerHealth.Instance;player.SetMaximumHealth(100000);player.RestoreHealth(100000);
            float savedCameraY=pressure.GameplayCamera.transform.position.y;
            float savedDistance=pressure.DistanceTravelled;
            var testBody=player.GetComponent<Rigidbody2D>();Vector2 savedPosition=testBody.position;
            testBody.position=new Vector2(0,savedCameraY+3);
            pressure.enabled=true;yield return null;yield return null;
            Check(Mathf.Approximately(pressure.GameplayCamera.transform.position.y,savedCameraY),"held camera does not follow forward player");
            pressure.enabled=false;testBody.position=savedPosition;
            pressure.RestoreProgress(savedCameraY,savedDistance,PressureTheme.Grass);
            int pausedFrame=pursuit.FrameIndex;Time.timeScale=0;yield return new WaitForSecondsRealtime(.2f);
            Check(pursuit.FrameIndex==pausedFrame,"pause freezes pursuit animation");Time.timeScale=4;
            foreach(var tower in player.GetComponentsInChildren<TowerController>())tower.enabled=false;
            foreach(var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))Destroy(enemy.gameObject);
            foreach(var texture in Resources.LoadAll<Texture2D>("ProjectileArt"))Check(ProjectileArt.Load(texture.name)!=null,"friendly art "+texture.name);
            for(int f=1;f<=16;f++)
            {
                TransformationShot.Fire(f,player.transform.position,Vector2.up,1,1,Color.white);
                yield return null;
                Check(FindObjectsByType<ProjectileVisual>(FindObjectsSortMode.None).Any(v=>v.AssetName=="ROT-"+f.ToString("00")),"form sprite bound "+f);
            }
            CampaignDirector.ClearProjectiles();yield return null;
            Check(Vector2.Distance(ProjectileArt.Thicker(new Vector2(.1f,.3f)),new Vector2(.12f,.3f))<.0001f,"vertical shot thicker, length unchanged");
            Check(Vector2.Distance(ProjectileArt.Thicker(new Vector2(.3f,.1f)),new Vector2(.3f,.12f))<.0001f,"horizontal shot thicker, length unchanged");
            Check(Vector2.Distance(ProjectileArt.Thicker(Vector2.one),Vector2.one*1.2f)<.0001f,"round shot twenty percent larger");
            var friendlySample=new GameObject("friendly size test");
            var friendly=ProjectileArt.Set(friendlySample,"PlayerBasic",new Vector2(.12f,.24f),Color.white);
            Check(Vector2.Distance(friendly.Renderer.bounds.size,new Vector2(.432f,.72f))<.001f,"friendly current size tripled");
            Check(friendly.Renderer.sharedMaterial.GetFloat("_Hostile")==0,"friendly palette selected");
            Check(BossArt.HostileMaterial.GetFloat("_Hostile")==1,"hostile palette selected");
            var collisionSample=friendlySample.AddComponent<ProjectileController>();
            collisionSample.FitVisualCollision(friendly.DisplaySize);
            Check(Vector2.Distance(collisionSample.VisualCollisionSize,friendly.DisplaySize)<.0001f,"friendly collision matches displayed footprint");
            Check(Vector2.Distance(friendlySample.GetComponent<CapsuleCollider2D>().size*(Vector2)friendlySample.transform.lossyScale,friendly.DisplaySize)<.0001f,"capsule local scale compensation");
            Destroy(friendlySample);
            var flight=player.GetComponent<PlayerFlightEffects>();
            var controller=player.GetComponent<PlayerController>();
            Check(flight!=null,"movement trail bound to player");
            controller.enabled=false;
            var motionBody=player.GetComponent<Rigidbody2D>();
            var oldInterpolation=motionBody.interpolation;motionBody.interpolation=RigidbodyInterpolation2D.None;
            Vector3 savedPlayerPosition=player.transform.position;
            foreach(var heading in new[]{Vector2.up,Vector2.right,Vector2.down,Vector2.left})
            {
                int emitted=flight.MovementTrailsShown;
                for(int frame=0;frame<20;frame++){motionBody.position+=heading*.15f;yield return null;}
                Check(flight.MovementTrailsShown>emitted,"movement emits pixel wake");
                Check(Vector2.Dot(flight.LastTrailDirection,-heading)>.99f,"wake follows opposite movement direction");
                Check(flight.ActiveMovementPuffs<=8,"wake pool bounded");
                Capture("Player-trail-"+heading.x+"-"+heading.y+".png");
                yield return new WaitForSeconds(.8f);
                yield return null;
                Check(flight.ActiveMovementPuffs==0,"wake fades after stopping");
            }
            motionBody.position=savedPlayerPosition;motionBody.interpolation=oldInterpolation;controller.enabled=true;
            for(int theme=0;theme<4;theme++)
            {
                pressure.SetTheme((PressureTheme)theme);
                var rosterSpawner=FindFirstObjectByType<EnemySpawner>();
                for(int i=0;i<7;i++)
                {
                    Check(EnemyArchetype.Load((PressureTheme)theme,i)!=null,"enemy sprite "+theme+"/"+i);
                    Check(EnemyProjectileArt.Load(EnemyProjectileArt.PathFor((PressureTheme)theme,i))!=null,"round projectile sprite "+theme+"/"+i);
                    var enemyTexture=EnemyArchetype.Load((PressureTheme)theme,i).texture;
                    Check(enemyTexture.width==32 && enemyTexture.height==32 && enemyTexture.filterMode==FilterMode.Point,"32px point-filtered enemy");
                    var sentry=rosterSpawner.SpawnCampaignEnemy(1);
                    Check(sentry!=null,"role placement available "+i);
                    var archetype=sentry.GetComponent<EnemyArchetype>();
                    Check(!PlayfieldBoundary.Instance.Contains(sentry.transform.position),"enemy stays above cliff");
                    Check(sentry.MaximumHealth>=162,"upgraded enemy health");
                    Check(archetype.BulletHealth==(archetype.Elite?70:archetype.Variant==2?38.4f:24),"grade bullet HP");
                }
                Capture("Roster-"+theme+".png");
                yield return new WaitForSeconds(2.2f);
                Check(FindObjectsByType<EnemyProjectileController>(FindObjectsSortMode.None).Any(b=>b.MaximumBounces>0&&b.MaximumBounces<=2&&b.MaximumHealth>=70),"elite ricochet emitted");
                foreach(var round in FindObjectsByType<EnemyProjectileController>(FindObjectsSortMode.None))
                {
                    Check(Vector2.Distance(round.VisualCollisionSize,Vector2.one*.768f)<.001f,"round shot visible-size collision");
                    var visual=round.transform.Find("Round enemy projectile");
                    Check(visual!=null&&Mathf.Abs(visual.GetComponent<SpriteRenderer>().sprite.bounds.size.x*Mathf.Abs(visual.lossyScale.x)-.768f)<.001f,"round shot visual bound");
                }
                Capture("Roster-shots-"+theme+".png");
                foreach(var sentry in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))Destroy(sentry.gameObject);
                CampaignDirector.ClearProjectiles();yield return null;
            }
            for(int kind=0;kind<4;kind++)
            {
                pressure.SetTheme((PressureTheme)kind);
                var enemy=FindFirstObjectByType<EnemySpawner>().SpawnCampaignEnemy(2,true);
                var boss=enemy.GetComponent<CampaignBoss>();boss.StopAllCoroutines();
                Check((int)boss.Kind==kind,"theme selects "+boss.Kind);
                enemy.ConfigureRun(100000,0,true);
                if(boss.Kind==BossKind.Twins)
                {
                    Check(!EnemyRegistry.FindAllWithinRadius(enemy.transform.position,.5f).Contains(enemy),"twins empty midpoint is not area target");
                    Check(EnemyRegistry.FindAllWithinRadius(enemy.transform.position+Vector3.left*2,.5f).Contains(enemy),"twins left body is area target");
                    Check(EnemyRegistry.FindAllWithinRadius(enemy.transform.position+Vector3.right*2,.5f).Contains(enemy),"twins right body is area target");
                }
                for(int pattern=0;pattern<5;pattern++)
                {
                    var method=typeof(CampaignBoss).GetMethod(boss.Kind.ToString(),BindingFlags.NonPublic|BindingFlags.Instance);
                    int before=boss.ShotsFired+boss.LasersFired;
                    boss.StartCoroutine((IEnumerator)method.Invoke(boss,new object[]{pattern}));
                    yield return new WaitForSeconds(1.6f);
                    if(pattern==1||pattern==3)Capture(boss.Kind+"-pattern-"+pattern+".png");
                    yield return new WaitForSeconds((pattern==4?13:9)-1.6f);
                    Check(boss.ShotsFired+boss.LasersFired>before,boss.Kind+" pattern "+pattern+" emits");
                    Check(FindObjectsByType<BossLaser>(FindObjectsSortMode.None).Length<=2,"laser cap");
                    boss.StopAllCoroutines();
                    if(pattern==1)Capture(boss.Kind+".png");
                    // Clear only ordinary bullets: satellites remain destructible scene actors.
                    foreach(var motion in FindObjectsByType<BossShotMotion>(FindObjectsSortMode.None))Destroy(motion.gameObject);
                    foreach(var laser in FindObjectsByType<BossLaser>(FindObjectsSortMode.None))Destroy(laser.gameObject);
                    yield return null;
                }
                var socket=new GameObject("test emitter");socket.transform.position=enemy.transform.position;
                var test=new GameObject("laser timing test").AddComponent<BossLaser>();
                test.Configure(boss,socket.transform,Vector2.down,boss.Kind==BossKind.Lava?"Lava/07-palm-laser":boss.Kind==BossKind.Butterfly?"Butterfly/06-boss-laser":"Ice/06-ring-laser",Color.cyan,.3f,1);
                yield return new WaitForSeconds(.5f);Check(!test.IsDamaging,"charge harmless");
                Time.timeScale=0;float age=test.Age;yield return new WaitForSecondsRealtime(.1f);Check(test.Age==age,"laser pause");Time.timeScale=4;
                yield return new WaitForSeconds(1);Check(test.IsDamaging,"laser active");
                var tiledBeam=test.GetComponentsInChildren<SpriteRenderer>().First(s=>s.sortingOrder==115);
                Check(tiledBeam.drawMode==SpriteDrawMode.Tiled,"laser shaft uses repeatable pixel tiles");
                Check(Mathf.Abs(tiledBeam.transform.localScale.x-tiledBeam.transform.localScale.y)<.0001f,"laser pixels retain square aspect");
                Check(Mathf.Abs(tiledBeam.bounds.size.x-test.Width*.85f)<.01f,"vertical laser visual width matches collision width");
                Capture(boss.Kind+"-laser.png");
                Destroy(socket);Destroy(enemy.gameObject);yield return null;yield return null;
                Check(FindObjectsByType<BossLaser>(FindObjectsSortMode.None).Length==0,"death clears beams");
                CampaignDirector.ClearProjectiles();yield return null;
            }
            File.WriteAllText(Path.Combine(output,"result.txt"),"PASS "+checks+" checks\n4 themes / 20 pattern executions / 16 transformation visuals\n");
            Debug.Log("BOSS_INTEGRATION_SUCCESS "+checks);EditorApplication.Exit(0);
        }
        private void Capture(string filename)
        {
            var camera=Camera.main;var target=new RenderTexture(1280,720,24);var previous=RenderTexture.active;
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,filename),image.EncodeToPNG());
            RenderTexture.active=previous;Destroy(image);Destroy(target);
        }
    }
}
#endif
