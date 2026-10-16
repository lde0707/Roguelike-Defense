#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Rushitadel.Player;
using Rushitadel.Combat;
using Rushitadel.Progression;
using Rushitadel.World;
namespace Rushitadel.Presentation
{
 public sealed class OrbitPressureProbe:MonoBehaviour
 {
  private static void Check(bool ok,string label){if(!ok)throw new Exception("ORBIT FAIL: "+label);Debug.Log("ORBIT PASS: "+label);}
  private void Awake(){DontDestroyOnLoad(gameObject);Application.logMessageReceived+=(m,s,t)=>{if((t==LogType.Exception||t==LogType.Error)&&!m.StartsWith("Curl error")&&!s.Contains("UnityEditor.Search"))EditorApplication.delayCall+=()=>EditorApplication.Exit(1);};}
  private IEnumerator Start()
  {
   RunStore.TestDirectory=System.IO.Path.GetFullPath("../output/orbit-validation/saves");
   var data=new RunData{slot=1,seed=432,theme=1,runId=Guid.NewGuid().ToString(),experience=600,campaign=new CampaignState{phase=RunPhase.Maintenance,recovered=true,augmentsChosen=5,transformationsChosen=5,turretForms=new[]{1,2,3,4,5},augments=new int[15],turretUpgrades=new int[30]}};
   RandomPlayerWeb.Ensure(data.campaign,data.seed);
   Check(RunStore.Write(data,out var error),"legacy save migration "+error);
   Check(data.campaign.turretForms.Length==6&&data.campaign.turretForms[4]==5&&data.campaign.turretForms[5]==0,"preserve forms; sixth starts empty");
   RunSession.Pending=data;SceneManager.LoadScene("inGame");yield return null;yield return null;
   var d=CampaignDirector.Instance;var p=PlayerHealth.Instance;var rig=p.GetComponent<PlayerTurretRig>();
   var towers=rig.GetComponentsInChildren<TowerController>();
   Check(towers.Length==6&&towers.Select(t=>t.SlotId).Distinct().Count()==6,"six distinct orbit slots");
   Check(towers.All(t=>Mathf.Abs(t.transform.localPosition.magnitude-PlayerTurretRig.OrbitRadius)<.001f),"detached circular orbit");
   Check(p.GetComponentsInChildren<Collider2D>().Count(c=>c.enabled)==1&&Mathf.Approximately(p.GetComponent<CircleCollider2D>().radius,PlayerTurretRig.CoreRadius),"only central circle collides");
   Check(d.ChooseTransformation(6,9)&&CampaignDirector.Form(6)==9,"sixth slot transformation");
   Check(RunStore.Write(data,out error)&&RunStore.Read(1,out var loaded,out error)&&loaded.campaign.turretForms[5]==9,"sixth slot save roundtrip "+error);
   typeof(CampaignDirector).GetMethod("BeginBoss",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(d,null);
   GameFrontend.Instance.ShowBattle();yield return null;yield return null;
   var pressure=ForwardPressureController.Instance;var rear=pressure.RearHazard.GetComponent<ThemePressureVisual>();
   Check(!pressure.RearHazard.enabled&&!pressure.InDanger&&pressure.ExposureSeconds==0,"boss rear hazard disabled");
   Check(!pressure.RearHazard.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&r.gameObject.activeInHierarchy),"boss rear visuals hidden");
   var start=rig.RotatingBody.rotation;yield return new WaitForSeconds(.2f);
   Check(Quaternion.Angle(start,rig.RotatingBody.rotation)>1,"turrets orbit during play");
   Time.timeScale=0;yield return null;Capture("boss");
   d.State.phase=RunPhase.Round;Time.timeScale=1;yield return null;yield return null;
   Check(pressure.RearHazard.enabled,"rear restored outside boss");
   Time.timeScale=0;yield return null;Capture("round");
   Debug.Log("ORBIT VALIDATION SUCCESS");EditorApplication.Exit(0);
  }
  private void Capture(string name)
  {
   var camera=Camera.main;var rt=new RenderTexture(1920,1080,24);var previous=RenderTexture.active;
   UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt});
   RenderTexture.active=rt;var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();
   System.IO.File.WriteAllBytes("../output/orbit-validation/"+name+".png",tex.EncodeToPNG());RenderTexture.active=previous;Destroy(tex);Destroy(rt);
  }
 }
}
#endif
