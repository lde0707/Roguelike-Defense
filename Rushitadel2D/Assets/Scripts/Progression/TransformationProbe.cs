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
namespace Rushitadel.Progression
{
    public sealed class TransformationProbe : MonoBehaviour
    {
        private string output;
        private static readonly Vector2 Origin=new(100,100);
        private static void Check(bool ok,string label){if(!ok)throw new Exception("FORM FAIL: "+label);Debug.Log("FORM PASS: "+label);}
        private static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
        private static void Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(obj,args);
        private static EnemyProjectileController Bullet(Vector2 p,float hp=1000)
        {
            var b=new GameObject("Test enemy projectile").AddComponent<EnemyProjectileController>();b.transform.position=p;
            Set(b,"maximumHealth",hp);Set(b,"health",hp);return b;
        }
        private static TransformationShot Fire(int form,Vector2 p,int slot=1)
        {
            TransformationShot.Fire(form,p,Vector2.up,1,slot,Color.magenta);
            return FindObjectsByType<TransformationShot>(FindObjectsSortMode.None).Last(s=>s.Form==form);
        }
        private static void Clear()
        {
            foreach(var s in FindObjectsByType<ProjectileBehaviour>(FindObjectsSortMode.None))DestroyImmediate(s.gameObject);
        }
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../output/transformation-20261007"));Directory.CreateDirectory(output);
            RunStore.TestDirectory=Path.Combine(output,"test-saves");
            Application.logMessageReceived+=(msg,stack,type)=>{if((type==LogType.Error||type==LogType.Exception)&&!msg.StartsWith("Curl error"))EditorApplication.delayCall+=()=>EditorApplication.Exit(1);};
        }
        private IEnumerator Start()
        {
            yield return null;
            RunSession.Pending=new RunData{slot=1,seed=173,runId=Guid.NewGuid().ToString()};SceneManager.LoadScene("inGame");yield return null;yield return null;
            var d=CampaignDirector.Instance;d.enabled=false;
            Rushitadel.World.ForwardPressureController.Instance.enabled=false;
            foreach(var t in FindObjectsByType<TowerController>(FindObjectsSortMode.None))t.enabled=false;
            foreach(var e in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))Destroy(e.gameObject);
            GameFrontend.Instance.ShowBattle();Time.timeScale=1;yield return null;Clear();
            Check(PlayerHealth.Instance.GetComponent<ModularPlayerArt>().VisibleTurrets==5,"five modular sockets");
            for(int f=1;f<=16;f++)Check(ModularPlayerArt.LoadSprite("ROT-"+f.ToString("00"),true)!=null,"sprite "+f);
            foreach(int f in new[]{1,4,7,12,16})
            {
                var b=Bullet(Origin+Vector2.up*.5f);var shot=Fire(f,Origin);shot.Hit(b);
                float damage=f==1?8:f==4?22:f==7?4:20;
                Check(Mathf.Abs(b.CurrentHealth-(1000-damage))<.01f,"direct HP form "+f);
                if(f==7)Check(Mathf.Approximately(b.DamageMultiplier,1.3f),"corrosion 30 percent");
                Clear();
            }
            var pelletTarget=Bullet(Origin+Vector2.up*.5f);Fire(2,Origin);
            foreach(var s in FindObjectsByType<TransformationShot>(FindObjectsSortMode.None))s.Hit(pelletTarget);
            Check(pelletTarget.CurrentHealth==984,"shotgun seven pellets capped at two hits");Clear();
            var drillTarget=Bullet(Origin+Vector2.up*.5f);Fire(3,Origin).Hit(drillTarget);
            yield return new WaitForSeconds(.66f);Check(drillTarget.CurrentHealth==850,"drill six real timed ticks");Clear();
            var disc=Fire(4,Origin);var discTargets=Enumerable.Range(0,5).Select(i=>Bullet(Origin+new Vector2(i*.1f,.5f))).ToArray();
            foreach(var b in discTargets)disc.Hit(b);Check(discTargets.Count(b=>b.CurrentHealth==978)==4,"disc four target cap");Clear();
            var orbTargets=Enumerable.Range(0,9).Select(i=>Bullet(Origin+new Vector2(i*.02f,.1f))).ToArray();Fire(5,Origin).Hit(orbTargets[0]);
            Check(orbTargets.Count(b=>b.CurrentHealth==968)==8,"explosion eight target cap");Clear();
            var cold=Bullet(Origin+Vector2.up,.0f+50);Fire(6,Origin);yield return null;yield return null;
            Check(cold.CurrentHealth==47&&Mathf.Approximately(cold.SpeedMultiplier,.6f),"cold real cone tick and slow");Clear();
            var returnTarget=Bullet(Origin+Vector2.up*.8f);Fire(9,Origin);yield return new WaitForSeconds(.5f);
            Check(returnTarget.CurrentHealth==964,"boomerang outward and return hits");Clear();
            var sawTarget=Bullet(Origin+Vector2.up*1.4f);Fire(10,Origin);yield return new WaitForSeconds(.72f);
            Check(sawTarget.CurrentHealth==964,"saw four timed ticks");Clear();
            var pushTarget=Bullet(Origin+Vector2.up,50);Fire(11,Origin);yield return null;yield return null;
            Check(pushTarget.CurrentHealth==38&&Mathf.Abs(pushTarget.transform.position.y-101.35f)<.01,"shock damage and push");
            pushTarget.Push(Vector2.up);Check(Mathf.Abs(pushTarget.transform.position.y-101.35f)<.01,"push global immunity");Clear();
            var splitTarget=Bullet(Origin+Vector2.up*.5f);Fire(12,Origin).Hit(splitTarget);yield return null;
            Check(FindObjectsByType<TransformationShot>(FindObjectsSortMode.None).Length==2,"split creates exactly two fragments");Clear();
            var netTarget=Bullet(Origin+Vector2.up*1.6f,50);Fire(13,Origin);yield return new WaitForSeconds(.2f);
            Check(netTarget.CurrentHealth==42&&Mathf.Approximately(netTarget.SpeedMultiplier,.7f),"net once only and slow");Clear();
            var foamTarget=Bullet(Origin+Vector2.up*1.2f);Fire(15,Origin);yield return new WaitForSeconds(.4f);
            Check(foamTarget.CurrentHealth==955,"foam actual HP budget 45");Clear();
            var chainTargets=new[]{Bullet(Origin+Vector2.up*.5f),Bullet(Origin+new Vector2(.5f,.5f)),Bullet(Origin+new Vector2(1,.5f)),Bullet(Origin+new Vector2(1.5f,.5f))};
            Fire(16,Origin).Hit(chainTargets[0]);Check(chainTargets[0].CurrentHealth==980&&chainTargets[1].CurrentHealth==988&&chainTargets[2].CurrentHealth==993&&chainTargets[3].CurrentHealth==1000,"chain 20 12 7 with hard cap");Clear();
            foreach(int gate in new[]{8,14})
            {
                Fire(gate,Origin,2);var s=Fire(1,Origin);Call(s,"TryGates",Origin+Vector2.up,Origin+Vector2.up*1.3f);
                var shots=FindObjectsByType<TransformationShot>(FindObjectsSortMode.None).Where(x=>x.Form==1).ToArray();
                if(gate==8){Check(shots.Length==2,"copy gate duplicates once");foreach(var copy in shots)Call(copy,"TryGates",Origin+Vector2.up,Origin+Vector2.up*1.3f);Check(FindObjectsByType<TransformationShot>(FindObjectsSortMode.None).Count(x=>x.Form==1)==2,"no recursive copying");}
                else {var b=Bullet(Origin+Vector2.up*1.5f);s.Hit(b);Check(b.CurrentHealth==990,"overcharge real damage 125 percent");}
                Clear();
            }
            foreach(int f in new[]{8,10,13,14,15}){for(int i=0;i<4;i++)Fire(f,Origin);yield return null;Check(FindObjectsByType<TransformationShot>(FindObjectsSortMode.None).Count(x=>x.Form==f)==(f==8||f==10?2:1),"stationary cap "+f);Clear();}
            Fire(1,Origin);yield return new WaitForSeconds(.5f);Check(FindObjectsByType<TransformationShot>(FindObjectsSortMode.None).Length==0,"moving shot ends at range three");Clear();
            d.Experience.AddExperience(2000);Time.timeScale=1;
            for(int f=1;f<=16;f++){Check(d.ChooseTransformation(1,f),"choose form "+f);Check(PlayerHealth.Instance.GetComponentsInChildren<TurretVisual>().First(v=>v.GetComponent<TowerController>().SlotId==1).DisplayedForm==f,"visual linked "+f);}
            Check(d.ChooseTransformation(2,8)&&!d.CanTransform(3,14),"support limited to one slot");
            d.State.element=1;d.ApplyBuild();Check(PlayerHealth.Instance.GetComponent<ModularPlayerArt>().EmissionColor==TurretForms.ElementColor(1),"fire emission");
            d.State.element=2;d.ApplyBuild();Check(PlayerHealth.Instance.GetComponent<ModularPlayerArt>().EmissionColor==TurretForms.ElementColor(2),"ice emission");
            var data=new RunData{slot=1,experience=2000,runId=Guid.NewGuid().ToString(),campaign=d.State};
            Check(RunStore.Write(data,out var error),"save transformed build "+error);Check(RunStore.Read(1,out var read,out _)&&read.campaign.turretForms[0]==16&&read.campaign.element==2,"restore form and element");
            var firingTower=PlayerHealth.Instance.GetComponentsInChildren<TowerController>().First(t=>t.SlotId==1);
            for(int f=1;f<=16;f++)
            {
                d.State.turretForms[0]=f;d.ApplyBuild();Clear();Call(firingTower,"Fire");
                Check(FindObjectsByType<TransformationShot>(FindObjectsSortMode.None).Any(s=>s.Form==f),"live turret firing "+f);
            }
            Clear();Check(d.OfferedForms(1).SequenceEqual(d.OfferedForms(1))&&d.OfferedForms(1).Length==3,"stable three-choice offer");
            d.State.turretForms=new[]{1,3,4,13,16};d.State.element=0;d.ApplyBuild();GameFrontend.Instance.ShowBattle();Time.timeScale=0;
            Capture("assembled-default.png");
            d.State.element=1;d.ApplyBuild();Capture("assembled-fire.png");d.State.element=2;d.ApplyBuild();Capture("assembled-ice.png");
            GameFrontend.Instance.ShowTransformations();Capture("upgrade-menu.png");
            GameFrontend.Instance.ShowBattle();Time.timeScale=1;
            var camera=Camera.main;float oldSize=camera.orthographicSize;var oldPosition=camera.transform.position;
            var canvas=GameFrontend.Instance.GetComponentInChildren<Canvas>();canvas.enabled=false;
            for(int page=0;page<4;page++)
            {
                for(int i=0;i<5;i++)d.State.turretForms[i]=1+(page*4+i)%16;
                d.State.element=page==1?1:page==2?2:0;d.ApplyBuild();
                PlayerHealth.Instance.GetComponent<PlayerTurretRig>().RotatingBody.rotation=Quaternion.Euler(0,0,page*23);
                foreach(var tower in PlayerHealth.Instance.GetComponentsInChildren<TowerController>())Call(tower,"Fire");
                yield return null;
                camera.orthographicSize=1.7f;camera.transform.position=new Vector3(PlayerHealth.Instance.transform.position.x,PlayerHealth.Instance.transform.position.y,oldPosition.z);
                Capture("detail-"+page+".png");Clear();
            }
            canvas.enabled=true;camera.orthographicSize=oldSize;camera.transform.position=oldPosition;
            Debug.Log("RUSHITADEL_TRANSFORMATION_VALIDATION_SUCCESS");EditorApplication.Exit(0);
        }
        private void Capture(string name)
        {
            var camera=Camera.main;var canvas=GameFrontend.Instance.GetComponentInChildren<Canvas>();
            var target=new RenderTexture(1920,1080,24);var old=RenderTexture.active;var oldTarget=camera.targetTexture;
            camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(output,name),tex.EncodeToPNG());
            RenderTexture.active=old;camera.targetTexture=oldTarget;canvas.renderMode=RenderMode.ScreenSpaceOverlay;Destroy(tex);Destroy(target);
        }
    }
}
#endif
