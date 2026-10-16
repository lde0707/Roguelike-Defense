#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rushitadel.Presentation;
using Rushitadel.World;
using Rushitadel.Combat;
using Rushitadel.Spawning;
using Rushitadel.Player;
namespace Rushitadel.Progression
{
    public sealed class ThemeContentProbe:MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            RunStore.TestDirectory=Path.GetFullPath(Path.Combine(Application.dataPath,"../../.work/theme-test-saves"));
            Application.logMessageReceived+=(message,stack,type)=>{if(type==LogType.Exception||type==LogType.Error)EditorApplication.delayCall+=()=>EditorApplication.Exit(1);};
        }
        private IEnumerator Start()
        {
            yield return null;RunSession.Pending=new RunData{seed=17,runId=Guid.NewGuid().ToString()};
            SceneManager.LoadScene("inGame");yield return null;yield return null;
            CampaignDirector.Instance.enabled=false;
            var pressure=ForwardPressureController.Instance;pressure.enabled=false;pressure.HoldAdvance=true;
            GameFrontend.Instance.ShowBattle();Time.timeScale=1;
            var player=PlayerHealth.Instance;player.SetMaximumHealth(100000);player.RestoreHealth(100000);
            foreach(var tower in player.GetComponentsInChildren<TowerController>())tower.enabled=false;
            foreach(var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))Destroy(enemy.gameObject);
            yield return null;
            var loop=FindFirstObjectByType<LoopingBattlefield>();
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../output/theme-content-20261016"));Directory.CreateDirectory(folder);
            for(int theme=0;theme<5;theme++)
            {
                pressure.SetTheme((PressureTheme)theme);yield return null;yield return null;
                var map=ThemeContent.Map((PressureTheme)theme);
                if(loop.Panels.Any(p=>p.sprite!=map)||Math.Abs(loop.PanelHeight-12.8f)>.001f)throw new Exception("Theme map mismatch "+theme);
                var boss=FindFirstObjectByType<EnemySpawner>().SpawnCampaignEnemy(1,true).GetComponent<CampaignBoss>();
                if((int)boss.Kind!=Math.Min(theme,3))throw new Exception("Theme boss mismatch "+theme);
                var clips=FindObjectsByType<ThemeBossAnimation>(FindObjectsSortMode.None);
                if(clips.Length==0||clips.Any(c=>c.IdleFrameCount==0))throw new Exception("Missing animation "+theme);
                var sr=clips[0].GetComponent<SpriteRenderer>();var before=sr.sprite;
                yield return new WaitForSeconds(.21f);
                if(sr.sprite==before)throw new Exception("Animation frozen "+theme);
                Capture(pressure.GameplayCamera,Path.Combine(folder,"idle-"+theme+".png"));
                yield return new WaitForSeconds(1.7f);
                foreach(var anim in clips){var visual=anim.GetComponent<SpriteRenderer>();Debug.Log("VISUAL "+theme+" "+visual.sprite.texture.name+" rect="+visual.sprite.rect+" pos="+visual.transform.position+" scale="+visual.transform.lossyScale+" enabled="+visual.enabled+" color="+visual.color);}
                Capture(pressure.GameplayCamera,Path.Combine(folder,"theme-"+ThemeContent.Type((PressureTheme)theme)+".png"));
                Debug.Log("THEME PASS "+theme+" map="+map.name+" boss="+boss.Kind+" animated parts="+clips.Length);
                Destroy(boss.gameObject);yield return null;CampaignDirector.ClearProjectiles();
            }
            Debug.Log("THEME_CONTENT_VALIDATION_OK");EditorApplication.Exit(0);
        }
        private static void Capture(Camera camera,string path)
        {
            var rt=RenderTexture.GetTemporary(1920,1080,24);var previous=camera.targetTexture;var active=RenderTexture.active;
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Destroy(image);
        }
    }
}
#endif
