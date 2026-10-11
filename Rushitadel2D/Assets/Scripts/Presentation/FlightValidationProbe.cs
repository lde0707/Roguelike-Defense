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
using Rushitadel.Power;
using Rushitadel.Progression;

namespace Rushitadel.Presentation
{
    public sealed class FlightValidationProbe : MonoBehaviour
    {
        private string output;
        private static void Check(bool condition,string message)
        {
            if(!condition)throw new Exception("FLIGHT FAIL: "+message);
            Debug.Log("FLIGHT PASS: "+message);
        }
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.runInBackground=true;
            output=Environment.GetEnvironmentVariable("RUSHITADEL_FLIGHT_OUTPUT") ?? Path.GetFullPath(Path.Combine(Application.dataPath,"../../../output/flight-validation"));
            Directory.CreateDirectory(output);
            RunStore.TestDirectory=Path.Combine(output,"test-saves");
            Application.logMessageReceived+=(message,stack,type)=>
            {
                // Unity 6000.5 can fail its editor-only search index in a fresh
                // batch project. This is unrelated to scene or shader execution.
                if(stack.Contains("UnityEditor.Search."))return;
                if((type==LogType.Exception || type==LogType.Error)&&!message.StartsWith("Curl error"))
                    EditorApplication.delayCall+=()=>EditorApplication.Exit(1);
            };
        }
        private IEnumerator Start()
        {
            Debug.Log("FLIGHT validation started");
            yield return null;
            RunSession.Pending=new RunData{slot=1,seed=173,runId=Guid.NewGuid().ToString()};
            SceneManager.LoadScene("inGame");yield return null;yield return null;
            var director=CampaignDirector.Instance;director.enabled=false;
            Rushitadel.World.ForwardPressureController.Instance.enabled=false;
            foreach(var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))Destroy(enemy.gameObject);
            var player=PlayerHealth.Instance;
            var towers=player.GetComponentsInChildren<TowerController>();
            foreach(var tower in towers)tower.enabled=false;
            GameFrontend.Instance.ShowBattle();Time.timeScale=1;
            var canvas=GameFrontend.Instance.GetComponentInChildren<Canvas>();canvas.enabled=false;
            yield return null;
            var art=player.GetComponent<ModularPlayerArt>();var fx=art.Flight;
            Check(art.VisibleTurrets==5,"five modular turrets");
            var hull=Resources.Load<Texture2D>("PlayerForms/Hull");
            Check(hull.width==80 && hull.height==80,"40px logical hull at exact 2x");
            for(int form=0;form<=16;form++)
            {
                string name=form==0?"Default":"ROT-"+form.ToString("00");
                var texture=Resources.Load<Texture2D>("PlayerForms/"+name);
                Check(texture!=null && texture.width==40 && texture.height==40 && texture.filterMode==FilterMode.Point,name+" 20px logical point sprite at exact 2x");
                director.State.turretForms[0]=form;director.ApplyBuild();
                Check(towers.First(t=>t.SlotId==1).GetComponent<TurretVisual>().DisplayedForm==form,"bound form "+form);
                int before=fx.ShotsShown;
                typeof(TowerController).GetMethod("Fire",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(towers.First(t=>t.SlotId==1),null);
                Check(fx.ShotsShown==before+1,"firing effect form "+form);
            }
            foreach(var shot in FindObjectsByType<ProjectileBehaviour>(FindObjectsSortMode.None))Destroy(shot.gameObject);
            director.State.turretForms=new[]{1,3,4,13,16};director.ApplyBuild();
            var rig=player.GetComponent<PlayerTurretRig>();
            var position=player.transform.position;float angle=rig.RotatingBody.eulerAngles.z;
            var offset=fx.VisualOffsetWorld;
            yield return new WaitForSeconds(.2f);
            Check(Mathf.Abs(Mathf.DeltaAngle(angle,rig.RotatingBody.eulerAngles.z))>1,"continuous rig rotation");
            Check((fx.VisualOffsetWorld-offset).sqrMagnitude>.00000001f,"idle animation progresses");
            Check(Vector3.Distance(player.transform.position,position)<.001f,"idle leaves physical player unchanged");
            var attack=player.GetComponent<PlayerAttackController>();
            var power=player.GetComponent<PlayerPowerShotController>();
            Vector2 aim=Camera.main.WorldToScreenPoint(player.transform.position+Vector3.up*3);
            int shots=fx.ShotsShown;
            Check(attack.TryFire(aim)&&fx.ShotsShown==shots+1,"successful basic attack triggers effect");
            Check(!attack.TryFire(aim)&&fx.ShotsShown==shots+1,"cooldown produces no false effect");
            var powerResource=player.GetComponent<PlayerPower>();powerResource.RestorePower(100,false);
            shots=fx.ShotsShown;
            Check(power.TryFire(aim)&&fx.ShotsShown==shots+1,"successful power attack triggers effect");
            Check(!power.TryFire(aim)&&fx.ShotsShown==shots+1,"insufficient power produces no effect");
            Time.timeScale=0;yield return null;
            offset=fx.VisualOffsetWorld;angle=rig.RotatingBody.eulerAngles.z;float pulse=fx.CorePulse;
            yield return new WaitForSecondsRealtime(.15f);
            Check(fx.VisualOffsetWorld==offset&&Mathf.Approximately(fx.CorePulse,pulse)&&Mathf.Approximately(angle,rig.RotatingBody.eulerAngles.z),"pause freezes flight animation");
            Check(!attack.TryFire(aim)&&!power.TryFire(aim),"paused attacks blocked");
            Time.timeScale=1;yield return new WaitForSeconds(.5f);
            var camera=Camera.main;
            if (Environment.GetEnvironmentVariable("RUSHITADEL_ART_REVIEW") == "1")
            {
                foreach(var shot in FindObjectsByType<ProjectileBehaviour>(FindObjectsSortMode.None))Destroy(shot.gameObject);
                foreach(string name in new[]{"Ice/01-ring-boomerang","Ice/02-crescent-boomerang","Butterfly/08-laser-charge"})
                    Check(BossArt.Load(name)!=null, "Boss replacement sprite " + name);
                foreach(string name in new[]{"Butterfly/06-boss-laser","Butterfly/07-drone-laser","Lava/07-palm-laser","Ice/06-ring-laser","Twins/06-hydro-laser","Twins/07-lightning-tether"})
                    Check(BossArt.Beam(name)!=null, "Boss replacement beam " + name);
                for(int theme=0;theme<4;theme++)
                {
                    Rushitadel.World.ForwardPressureController.Instance.SetTheme((Rushitadel.World.PressureTheme)theme);
                    for(int index=0;index<7;index++)
                    {
                        var sprite=EnemyProjectileArt.Load(EnemyProjectileArt.PathFor((Rushitadel.World.PressureTheme)theme,index));
                        Check(sprite!=null && sprite.texture.width==56 && sprite.texture.height==56 && sprite.texture.filterMode==FilterMode.Point,
                            "28px logical enemy sprite at exact 2x " + theme + "/" + index);
                        var bullet = new GameObject("Review bullet").AddComponent<EnemyProjectileController>();
                        bullet.transform.position=player.transform.position+new Vector3((index-3)*1.1f,3,0);
                        bullet.ConfigureBoss(30,0,.1f,100,0);bullet.Launch(Vector2.down);
                        EnemyProjectileArt.Apply(bullet,(Rushitadel.World.PressureTheme)theme,index);
                        Check(Vector2.Distance(bullet.VisualCollisionSize,Vector2.one*(index>=5?.56f:.46f))<.001f,
                            "Visible enemy hitbox " + theme + "/" + index);
                    }
                    yield return null;yield return null;
                    Capture(camera,"theme-"+theme+".png",1920,1080);
                    foreach(var shot in FindObjectsByType<EnemyProjectileController>(FindObjectsSortMode.None))Destroy(shot.gameObject);
                    yield return null;
                }
            }
            foreach(var behaviour in camera.GetComponents<MonoBehaviour>())behaviour.enabled=false;
            camera.orthographicSize=1.55f;
            camera.transform.position=new Vector3(player.transform.position.x,player.transform.position.y,camera.transform.position.z);
            Time.captureFramerate=20;
            for(int frame=0;frame<80;frame++)
            {
                if(frame==20 || frame==26 || frame==32)attack.TryFire(camera.WorldToScreenPoint(player.transform.position+Vector3.up*3));
                if(frame==45){powerResource.RestorePower(100,false);power.TryFire(camera.WorldToScreenPoint(player.transform.position+Vector3.up*3));}
                if(frame>=54 && frame%6==0)
                    foreach(var tower in towers)typeof(TowerController).GetMethod("Fire",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(tower,null);
                yield return null;
                Capture(camera,"frame-"+frame.ToString("000")+".png");
            }
            Time.captureFramerate=0;
            Debug.Log("RUSHITADEL_FLIGHT_VALIDATION_SUCCESS");
            EditorApplication.Exit(0);
        }
        private void Capture(Camera camera,string filename,int width=640,int height=640)
        {
            var target=new RenderTexture(width,height,24);
            var previous=RenderTexture.active;
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(output,filename),image.EncodeToPNG());
            RenderTexture.active=previous;Destroy(image);Destroy(target);
        }
    }
}
#endif
