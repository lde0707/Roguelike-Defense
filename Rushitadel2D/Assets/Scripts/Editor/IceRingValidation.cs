using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rushitadel.Progression;
using Rushitadel.Presentation;
namespace Rushitadel.Editor
{
    [InitializeOnLoad]
    public static class IceRingValidation
    {
        static int step;static double next;
        static IceRingValidation(){EditorApplication.playModeStateChanged+=s=>{
            if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("IceRing.Check",false))return;
            SessionState.SetBool("IceRing.Check",false);step=0;next=EditorApplication.timeSinceStartup+4;EditorApplication.update+=Tick;
        };}
        public static void Run(){EditorSceneManager.playModeStartScene=null;EditorSceneManager.OpenScene(BossTestSceneSetup.Path);SessionState.SetBool("IceRing.Check",true);EditorApplication.EnterPlaymode();}
        static void Check(bool ok,string text){if(!ok)throw new Exception(text);Debug.Log("ICE RING PASS: "+text);}
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup<next)return;
            try
            {
                var test=UnityEngine.Object.FindFirstObjectByType<BossTestController>();
                Check(test!=null&&test.Ready,"test ready");
                if(step==0){test.SelectBoss(2,1,0,0);step++;next=EditorApplication.timeSinceStartup+3.6;return;}
                var animations=UnityEngine.Object.FindObjectsByType<ThemeBossAnimation>(FindObjectsSortMode.None);
                var trails=UnityEngine.Object.FindObjectsByType<IceRingDashTrail>(FindObjectsSortMode.None);
                if(step==1||step==3)
                {
                    Check(animations.Any(a=>a.IsContactSpinning&&a.GetComponent<SpriteRenderer>().sprite.texture.name.StartsWith("boss_Type_B_weapon_idle_")),"contact attack uses fast idle frames");
                    Check(trails.Any(t=>t.Emitting),"dash trail emitting");
                    Check(UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Any(s=>s.name.StartsWith("Ice echo")&&s.enabled&&s.color.a<=.22f),"subtle world-space echoes visible");
                }
                if(step==1){test.SelectBoss(2,1,0,5);step++;next=EditorApplication.timeSinceStartup+4;return;}
                if(step==2)
                {
                    Check(!animations.Any(a=>a.IsContactSpinning),"ranged attack has no contact spin");
                    Check(animations.Any(a=>a.IsActionPlaying&&a.GetComponent<SpriteRenderer>().sprite.texture.name.StartsWith("boss_Type_B_weapon_attack_")),"laser uses attack animation");
                    Check(test.Boss.LasersFired>0&&test.Boss.ShotsFired>0,"ranged attack emits laser and orbs");
                    Check(!trails.Any(t=>t.Emitting),"ranged attack has no dash trail");
                    test.SelectBoss(2,1,0,11);step++;next=EditorApplication.timeSinceStartup+4.1;return;
                }
                test.SelectBoss(0);step++;next=EditorApplication.timeSinceStartup+3;
                if(step==5)
                {
                    Check(!UnityEngine.Object.FindObjectsByType<IceRingDashTrail>(FindObjectsSortMode.None).Any(),"trail cleanup after boss switch");
                    Debug.Log("ICE RING VALIDATION SUCCESS");EditorApplication.update-=Tick;EditorApplication.Exit(0);
                }
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.update-=Tick;EditorApplication.Exit(1);}
        }
    }
}
