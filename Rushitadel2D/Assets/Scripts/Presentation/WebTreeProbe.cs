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
            var data=new RunData{slot=1,seed=432,theme=0,experience=50,runId=Guid.NewGuid().ToString(),campaign=new CampaignState{phase=RunPhase.Maintenance,recovered=true}};
            Check(RunStore.Write(data,out _),"isolated maintenance save");RunSession.Pending=data;
            SceneManager.LoadScene("inGame");yield return null;yield return null;
            var d=CampaignDirector.Instance;var ui=GameFrontend.Instance;
            Check(ui.CurrentPage=="maintenance"&&Time.timeScale==0,"web pauses combat");
            var nodes=FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.name.StartsWith("Web Icon ")).ToArray();
            Check(nodes.Length==24,"all 24 spreadsheet nodes");
            Check(ui.GetComponentsInChildren<Image>().Count(i=>i.name=="Web connection")==24,"all upward tree connections");
            int wallet=d.Wallet;
            foreach(var b in nodes)
            {
                var icon=b.transform.Find("Icon").GetComponent<Image>();
                Check(icon.sprite!=null&&icon.sprite.texture.filterMode==FilterMode.Point,"pixel icon "+b.name);
                b.onClick.Invoke();yield return null;
                Check(d.Wallet==wallet,"inspection never spends XP");
            }
            Find("Web Icon Pierce").onClick.Invoke();yield return null;
            var detail=ui.GetComponentsInChildren<RectTransform>().First(r=>r.name=="Web details");
            Check(detail.anchoredPosition.x>=1400&&detail.anchoredPosition.y<=-490,"fixed lower-right details");
            Check(detail.GetComponentsInChildren<Text>().Any(t=>t.text=="연속 관통"),"clicked description changes");
            Capture("web-tree.png");
            Find("현재 강화 6종 Button").onClick.Invoke();yield return null;
            Check(d.BuyWeb(0)&&d.Wallet==wallet-40,"existing purchase preserved");
            Find("아이콘 Web 보기 Button").onClick.Invoke();yield return null;
            Check(Find("Web Icon Pierce")!=null&&d.State.web[0],"return preserves purchase");
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
