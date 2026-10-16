#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;
using Rushitadel.Progression;
using Rushitadel.Combat;
using Rushitadel.Player;
using Rushitadel.World;
namespace Rushitadel.Presentation
{
 public sealed class BossObjectsProbe:MonoBehaviour
 {
  private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
  private void Awake(){DontDestroyOnLoad(gameObject);Application.logMessageReceived+=(m,s,t)=>{if((t==LogType.Error||t==LogType.Exception)&&!s.Contains("UnityEditor.Search")&&!m.StartsWith("Curl error"))EditorApplication.delayCall+=()=>EditorApplication.Exit(1);};}
  private static void Check(bool ok,string text){if(!ok)throw new Exception("OBJECT FAIL: "+text);Debug.Log("OBJECT PASS: "+text);}
  private IEnumerator Start()
  {
   RunStore.TestDirectory=System.IO.Path.GetFullPath("../output/boss-objects-validation/saves");
   RunSession.Pending=new RunData{slot=1,seed=44,runId=Guid.NewGuid().ToString()};SceneManager.LoadScene("inGame");yield return null;yield return null;
   var director=CampaignDirector.Instance;director.enabled=false;
   var player=PlayerHealth.Instance;player.SetMaximumHealth(100000);player.RestoreHealth(100000);
   foreach(var tower in player.GetComponentsInChildren<TowerController>())tower.enabled=false;
   foreach(var enemy in EnemyRegistry.ActiveEnemies.ToArray())if(enemy!=null)Destroy(enemy.gameObject);
   yield return null;
   for(int theme=0;theme<4;theme++)
   {
    ForwardPressureController.Instance.SetTheme((PressureTheme)theme);
    typeof(CampaignDirector).GetMethod("BeginBoss",Flags).Invoke(director,null);
    var boss=director.Boss;boss.StopAllCoroutines();GameFrontend.Instance.ShowBattle();Time.timeScale=8;
    if(theme==3)
    {
     float hp=boss.Health.CurrentHealth;boss.TwinHealth(0).TakeDamage(123);
     Check(boss.TwinHealth(1).CurrentHealth==hp*.5f&&boss.TwinHealth(0).CurrentHealth==hp*.5f-123&&boss.Health.CurrentHealth==hp-123,"independent twin damage and aggregate HP");
     Check(!EnemyRegistry.ActiveEnemies.Contains(boss.Health)&&EnemyRegistry.ActiveEnemies.Contains(boss.TwinHealth(0)),"no duplicate root damage target");
     yield return null;
     var images=FindObjectsByType<Image>(FindObjectsSortMode.None);
     Check(images.Any(i=>i.name=="Water Boss HP"&&i.gameObject.activeInHierarchy)&&images.Any(i=>i.name=="Electric Boss HP"&&i.gameObject.activeInHierarchy),"two independent visible health bars");
    }
    for(int pattern=0;pattern<3;pattern++)
    {
     int lasers=boss.LasersFired,shots=boss.ShotsFired;
     var routine=(IEnumerator)typeof(CampaignBoss).GetMethod("ObjectPattern",Flags).Invoke(boss,new object[]{pattern});
     bool done=false;boss.StartCoroutine(Complete(routine,()=>done=true));
     yield return new WaitForSeconds(1.4f);
     Check(boss.ActiveAttackParts==(pattern==2?6:4),boss.Kind+" clone count "+pattern);
     if(pattern==0)
     {
      Capture(boss.Kind.ToString());
      var clone=FindObjectsByType<EnemyProjectileController>(FindObjectsSortMode.None).First(s=>s.name.StartsWith("Attack part clone"));
      clone.TakeDamage(10000);yield return null;
      Check(boss.ActiveAttackParts==3,boss.Kind+" clone can be intercepted");
     }
     float deadline=Time.realtimeSinceStartup+12;
     while(!done&&Time.realtimeSinceStartup<deadline)yield return null;
     Check(done&&boss.ActiveAttackParts==0,boss.Kind+" pattern completes and clears clones "+pattern);
     Check(boss.ShotsFired>shots&&(pattern==1||boss.LasersFired>lasers),boss.Kind+" emits attacks "+pattern);
     CampaignDirector.ClearProjectiles();yield return null;
    }
    if(theme==3)
    {
     boss.StartCoroutine((IEnumerator)typeof(CampaignBoss).GetMethod("ObjectPattern",Flags).Invoke(boss,new object[]{0}));yield return new WaitForSeconds(.2f);
     boss.TwinHealth(0).Eliminate();yield return null;yield return null;
     Check(boss!=null&&boss.Health.IsAlive&&boss.TwinHealth(1).IsAlive&&boss.ActiveAttackParts<=2,"one twin death stops its clones, survivor remains");
     Check(boss.FireAt(boss.transform.position+Vector3.left*2,Vector2.down,"Twins/01-pressure-orb")==null,"defeated twin cannot fire");
     boss.TwinHealth(1).Eliminate();yield return null;yield return null;
     Check(boss==null,"both twins defeated ends boss");
    }
    else {Destroy(boss.gameObject);yield return null;}
    Check(!FindObjectsByType<BossLaser>(FindObjectsSortMode.None).Any(),"laser cleanup "+theme);
   }
   Debug.Log("BOSS OBJECTS VALIDATION SUCCESS");EditorApplication.Exit(0);
  }
  private IEnumerator Complete(IEnumerator routine,Action done){yield return routine;done();}
  private void Capture(string name)
  {
   string folder=System.IO.Path.GetFullPath("../output/boss-objects-validation");System.IO.Directory.CreateDirectory(folder);
   var camera=Camera.main;var canvas=GameFrontend.Instance.GetComponentInChildren<Canvas>();
   canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();
   var rt=new RenderTexture(1920,1080,24);var previous=RenderTexture.active;
   UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt});
   RenderTexture.active=rt;var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();
   System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,name+".png"),tex.EncodeToPNG());RenderTexture.active=previous;canvas.renderMode=RenderMode.ScreenSpaceOverlay;Destroy(tex);Destroy(rt);
  }
 }
}
#endif
