#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Rushitadel.Progression;
using Rushitadel.Presentation;

namespace Rushitadel.Editor
{
    public sealed class RetroUiImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Resources/RetroUI/"))return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;
            importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;
        }
    }
    public static class RetroUiValidation
    {
        public static void Run()
        {
            foreach(string name in new[]{"Frames","Icons","Themes"})
                AssetDatabase.ImportAsset("Assets/Resources/RetroUI/"+name+".png",ImportAssetOptions.ForceUpdate);
            EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
            SessionState.SetBool("RetroUi.Validate",true);EditorApplication.isPlaying=true;
        }
        [InitializeOnLoadMethod] private static void Hook()
        {
            EditorApplication.playModeStateChanged+=state=>{
                if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("RetroUi.Validate",false))return;
                SessionState.SetBool("RetroUi.Validate",false);
                new GameObject("Retro UI validation").AddComponent<RetroUiProbe>();
            };
        }
    }
    public sealed class RetroUiProbe:MonoBehaviour
    {
        private string output;
        private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        private GameFrontend ui;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../output/retro-ui-20261031"));Directory.CreateDirectory(output);
            RunStore.TestDirectory=Path.Combine(output,"isolated-saves-"+DateTime.Now.ToString("HHmmss"));
            GameSettings.StorageKey="Rushitadel.RetroUiTest."+Guid.NewGuid();GameSettings.Load();
            Application.logMessageReceived+=OnLog;
        }
        private void OnLog(string message,string stack,LogType type)
        {
            if((type==LogType.Exception||type==LogType.Error)&&!message.StartsWith("Curl error")&&!stack.Contains("UnityEditor.Search"))
            {File.AppendAllText(Path.Combine(output,"errors.txt"),message+"\n"+stack+"\n");EditorApplication.Exit(1);}
        }
        private void Call(string name,params object[] args)=>typeof(GameFrontend).GetMethod(name,Flags).Invoke(ui,args);
        private void Set(string name,object value)=>typeof(GameFrontend).GetField(name,Flags).SetValue(ui,value);
        private static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("RETRO PASS: "+message);}
        private static void Click(string name)=>FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name==name&&b.gameObject.activeInHierarchy).onClick.Invoke();
        private IEnumerator Start()
        {
            yield return null;yield return null;ui=GameFrontend.Instance;
            ui.ShowTitle();yield return null;Capture("01-title");
            Check(GameObject.Find("StartButton")!=null,"Title navigation retained");
            Click("SettingsButton");yield return null;Capture("02-settings");
            Click("사운드 Button");yield return null;Capture("03-audio");
            var slider=FindObjectsByType<Slider>(FindObjectsSortMode.None).First(s=>s.name=="효과음 Slider");
            slider.value=0;
            Check(ui.GetComponent<AudioSource>().volume==0,"Effects mute applies to result source");
            Click("취소 Button");yield return null;
            ui.ShowSlots(true);yield return null;Capture("04-slots");
            Call("SelectNewSlot",1);yield return null;Capture("05-themes");
            Set("selectedWebIcon",6);Call("ShowWebTree",new object[]{null});yield return null;Capture("06-web");
            ui.ShowTitle();Call("SelectNewSlot",1);yield return null;
            var card=FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.transform.Find("Theme Illustration")!=null);
            card.onClick.Invoke();yield return null;Click("선택 확정 · 시작 Button");
            float deadline=Time.realtimeSinceStartup+40;
            while(GameFrontend.IsTransitioning||CampaignDirector.Instance==null||!CampaignDirector.Instance.Initialized)
            {Check(Time.realtimeSinceStartup<deadline,"Run starts within timeout");yield return null;}
            yield return null;ui.ShowBattle();yield return null;Capture("07-battle");
            Capture("07-battle-720p",1280,720);
            CampaignDirector.Instance.Experience.AddExperience(100);
            ui.ShowAugments();yield return null;Capture("11-transform");
            for(int i=0;i<6;i++)CampaignDirector.Instance.State.turretForms[i]=1;
            ui.ShowAugments();yield return null;Capture("12-augment");
            ui.ShowPause();yield return null;Capture("08-pause");
            ui.ShowRunResult(true);yield return new WaitForSecondsRealtime(.7f);Capture("09-clear");Capture("09-clear-720p",1280,720);
            var motion=FindFirstObjectByType<ResultUiMotion>();
            Check(Time.timeScale==0&&motion!=null&&motion.Elapsed>.5f,"Result animates with simulation paused");
            Check(ui.GetComponent<AudioSource>().isPlaying,"Clear cue plays while paused");
            yield return new WaitForSecondsRealtime(.6f);ui.ShowRunResult(true);yield return null;
            Check(!ui.GetComponent<AudioSource>().isPlaying,"Reopening result does not replay cue");
            Set("resultCuePlayed",false);ui.ShowRunResult(false);yield return new WaitForSecondsRealtime(.65f);Capture("10-game-over");
            Check(ui.GetComponent<AudioSource>().isPlaying,"Defeat cue plays while paused");
            ui.ShowTitle();yield return null;
            Check(FindFirstObjectByType<ResultUiMotion>()==null,"Result effects cleaned on navigation");
            PlayerPrefs.DeleteKey(GameSettings.StorageKey);PlayerPrefs.Save();
            File.WriteAllText(Path.Combine(output,"verification.txt"),"PASS: title, slots, themes, settings/audio, web, live HUD, pause, clear and defeat rendered. Effects mute, paused animation, one-shot result cue and cleanup checked. Saves isolated.\n");
            Debug.Log("RETRO_UI_VALIDATION_SUCCESS");EditorApplication.Exit(0);
        }
        private void Capture(string name,int width=1920,int height=1080)
        {
            var canvas=ui.GetComponentInChildren<Canvas>();var camera=Camera.main;
            Check(camera!=null,"Capture camera");
            var target=new RenderTexture(width,height,24);var old=camera.targetTexture;camera.targetTexture=target;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();if(ui.CurrentPage=="game")Call("LayoutBattleSides");var active=RenderTexture.active;
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());
            RenderTexture.active=active;camera.targetTexture=old;canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            Destroy(texture);Destroy(target);
        }
    }
}
#endif
