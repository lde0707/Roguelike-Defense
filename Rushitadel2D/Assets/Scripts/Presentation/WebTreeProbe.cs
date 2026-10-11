#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rushitadel.Progression;

namespace Rushitadel.Presentation
{
    public sealed class WebTreeProbe:MonoBehaviour
    {
        private string output;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../output/web-tree-validation"));Directory.CreateDirectory(output);
            RunStore.TestDirectory=Path.Combine(output,"saves-"+DateTime.Now.ToString("HHmmss"));
            Application.logMessageReceived+=(m,s,t)=>{if((t==LogType.Error||t==LogType.Exception)&&!s.Contains("UnityEditor.Search.SearchDatabase")&&!m.StartsWith("Curl error"))EditorApplication.delayCall+=()=>EditorApplication.Exit(1);};
        }
        private static void Check(bool ok,string label) {if(!ok)throw new Exception("WEB FAIL: "+label);Debug.Log("WEB PASS: "+label);}
        private static Button Find(string name)=>FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name==name&&b.gameObject.activeInHierarchy);
        private IEnumerator Start()
        {
            yield return null;
            for(int seed=1;seed<=1000;seed++)
            {
                var g=RandomPlayerWeb.Generate(seed);
                if(!RandomPlayerWeb.Valid(g))throw new Exception("Invalid graph "+seed);
                if(JsonUtility.ToJson(g)!=JsonUtility.ToJson(RandomPlayerWeb.Generate(seed)))throw new Exception("Unstable seed");
                foreach(var a in g.nodes)foreach(var b in g.nodes)if(!ReferenceEquals(a,b)&&Vector2.Distance(new Vector2(a.x,a.y),new Vector2(b.x,b.y))<57)throw new Exception("Overlapping nodes "+seed);
            }
            Check(true,"1000 valid deterministic non-overlapping graphs");
            Check(RandomPlayerWeb.Titles.Length==40&&RandomPlayerWeb.Effects.Length==40,"forty upgrade definitions");
            Check(Enumerable.Range(1,1000).SelectMany(seed=>RandomPlayerWeb.Generate(seed).nodes.GroupBy(n=>n.upgrade)).Any(g=>RandomPlayerWeb.Category(g.Key)==0&&g.Count()>=3),"basic upgrades can appear three or four times");
            var stacks=new CampaignState{playerWeb=new PlayerWebGraph{nodes=Enumerable.Range(0,4).Select(i=>new PlayerWebNode{upgrade=0,owned=true}).ToArray()}};
            Check(RandomPlayerWeb.MaximumHealth(stacks)==160,"four health nodes stack additively");
            Check(JsonUtility.ToJson(RandomPlayerWeb.Generate(1))!=JsonUtility.ToJson(RandomPlayerWeb.Generate(2)),"different seed different web");
            var data=new RunData{slot=1,seed=432,theme=0,experience=500,runId=Guid.NewGuid().ToString(),campaign=new CampaignState{phase=RunPhase.Maintenance,recovered=true,augmentsChosen=5,transformationsChosen=5,turretForms=new[]{1,2,3,4,5}}};
            RandomPlayerWeb.Ensure(data.campaign,data.seed);
            Check(RunStore.Write(data,out _),"isolated maintenance save");RunSession.Pending=data;
            SceneManager.LoadScene("inGame");yield return null;yield return null;
            var d=CampaignDirector.Instance;var ui=GameFrontend.Instance;
            Check(ui.CurrentPage=="maintenance"&&Time.timeScale==0,"web pauses combat");
            var nodes=FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.name.StartsWith("Web Node ")).ToArray();
            Check(nodes.Length==d.State.playerWeb.nodes.Length,"generated nodes rendered");
            int wallet=d.Wallet;
            foreach(var b in nodes)
            {
                Check(b.transform.Find("Icon").GetComponent<Image>().sprite!=null,"icon "+b.name);
            }
            nodes[0].onClick.Invoke();yield return null;
            Check(d.Wallet==wallet,"inspection never spends XP");
            Check(!d.CanBuyNode(2,out _),"locked child rejected");
            Check(d.BuyNode(0),"root purchase");
            Check(d.Wallet==wallet-d.State.playerWeb.nodes[0].cost,"exact XP deduction");
            Check(!d.BuyNode(0),"duplicate purchase rejected");
            Check(d.CanBuyNode(2,out _),"child unlocked");
            Check(d.BuyNode(2),"child purchase");
            Check(d.SaveCheckpoint("Web test"),"purchase saved");
            Check(RunStore.Read(1,out var loaded,out var error),"reload: "+error);
            Check(JsonUtility.ToJson(loaded.campaign.playerWeb)==JsonUtility.ToJson(d.State.playerWeb),"shape and purchases preserved");
            Check(!d.CanBuy(-1,out _)&&!d.SetElement(1),"legacy purchases and elements disabled");
            ui.ShowMaintenance();yield return null;
            Capture("random-web.png");
            // Isolated effect fixtures, never persisted to the player's saves.
            var savedGraph=d.State.playerWeb;
            d.State.playerWeb=new PlayerWebGraph{nodes=Enumerable.Range(0,RandomPlayerWeb.Titles.Length).Select(id=>new PlayerWebNode{upgrade=id,owned=true}).ToArray()};
            d.ApplyBuild();
            Check(Mathf.Abs(Rushitadel.Player.PlayerHealth.Instance.MaximumHealth-145.8f)<.01f,"flat and percentage health bonuses applied");
            Check(Rushitadel.Player.PlayerHealth.Instance.GetComponent<Rushitadel.Power.PlayerPower>().MaximumPower==125,"power stat and fitting applied");
            Check(Mathf.Abs(RandomPlayerWeb.MoveMultiplier-1.2f)<.001f,"movement bonuses combined");
            Check(Mathf.Abs(RandomPlayerWeb.DamageMultiplier-1.22f)<.001f,"basic damage bonuses combined");
            Check(Mathf.Abs(RandomPlayerWeb.CadenceMultiplier-1.18f)<.001f,"cadence bonuses combined");
            Check(Mathf.Abs(Rushitadel.Player.PlayerHealth.Instance.GetComponent<Rushitadel.Player.PlayerPowerShotController>().CostFraction-.66f)<.001f,"power-shot cost reduction");
            Time.timeScale=1;
            var h=Rushitadel.Player.PlayerHealth.Instance;h.RestoreHealth(135);h.TakeDamage(10);
            Check(Mathf.Abs(h.CurrentHealth-125.9f)<.01f,"normal hit reduced by armor");
            h.TakeEnvironmentalDamage(10);Check(Mathf.Abs(h.CurrentHealth-115.9f)<.01f,"rear hazard bypasses armor");
            var target=new GameObject("Web damage fixture");target.transform.position=new Vector3(100,100,0);
            var bullet=target.AddComponent<Rushitadel.Combat.EnemyProjectileController>();bullet.ConfigureBoss(1000,0,.1f,10,0);bullet.Launch(Vector2.down);
            var secondary=new GameObject("Web secondary fixture");secondary.transform.position=target.transform.position+Vector3.right*.3f;
            var other=secondary.AddComponent<Rushitadel.Combat.EnemyProjectileController>();other.ConfigureBoss(1000,0,.1f,10,0);other.Launch(Vector2.down);
            var shotGo=new GameObject("Web shot fixture");shotGo.transform.position=target.transform.position;
            var shot=shotGo.AddComponent<Rushitadel.Combat.ProjectileController>();shot.Launch(Vector2.up,100,8);shot.EnablePlayerWeb();shot.Hit(bullet);
            Check(Mathf.Abs(bullet.CurrentHealth-871.2f)<.01f,"interception and heavy-projectile bonuses applied");
            Check(Mathf.Abs(other.CurrentHealth-940)<.01f,"splash and chain reach a secondary hostile bullet");
            var runtime=h.GetComponent<PlayerWebRuntime>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(PlayerWebRuntime).GetField("shieldTime",flags).SetValue(runtime,12f);
            typeof(PlayerWebRuntime).GetField("pulseTime",flags).SetValue(runtime,8f);
            target.transform.position=h.transform.position+Vector3.right;
            d.State.phase=RunPhase.Boss;
            Time.timeScale=0;typeof(PlayerWebRuntime).GetMethod("Update",flags).Invoke(runtime,null);
            Check(runtime.Shield==0&&Mathf.Abs(bullet.CurrentHealth-871.2f)<.01f,"paused menu does not recharge or pulse");
            Time.timeScale=1;typeof(PlayerWebRuntime).GetMethod("Update",flags).Invoke(runtime,null);
            Check(runtime.Shield==20,"shield recharges to twenty");
            Check(Mathf.Abs(runtime.Absorb(30)-10)<.01f&&runtime.Shield==0,"shield absorbs only its remaining capacity");
            Check(Mathf.Abs(bullet.CurrentHealth-841.2f)<.01f,"pulse hits hostile bullet for thirty");
            Check(Mathf.Abs(h.Heal(10)-11.5f)<.01f,"healing amplification applied");
            var power=h.GetComponent<Rushitadel.Power.PlayerPower>();power.RestorePower(50,false);runtime.Reclaim();
            Check(Mathf.Abs(power.CurrentPower-51)<.01f,"bullet salvage restores power");
            runtime.Reclaim();Check(Mathf.Abs(power.CurrentPower-51)<.01f,"salvage cooldown prevents multi-hit stacking");
            Destroy(target);Destroy(secondary);Destroy(shotGo);d.State.phase=RunPhase.Maintenance;d.State.playerWeb=savedGraph;d.ApplyBuild();Time.timeScale=0;
            Debug.Log("RUSHITADEL_WEB_TREE_VALIDATION_SUCCESS");EditorApplication.Exit(0);
        }
        private void Capture(string name)
        {
            var canvas=GameFrontend.Instance.GetComponentInChildren<Canvas>();var camera=Camera.main;
            var rt=new RenderTexture(1920,1080,24);var old=camera.targetTexture;camera.targetTexture=rt;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();var previous=RenderTexture.active;
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            RenderTexture.active=rt;var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(output,name),tex.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=old;canvas.renderMode=RenderMode.ScreenSpaceOverlay;Destroy(tex);Destroy(rt);
        }
    }
}
#endif
