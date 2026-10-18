using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rushitadel.Presentation;
using Rushitadel.Player;
using Rushitadel.Progression;
namespace Rushitadel.Editor
{
    [InitializeOnLoad]
    public static class BossTestValidation
    {
        private static int index;
        private static double next,deadline;
        static BossTestValidation(){EditorApplication.playModeStateChanged+=s=>{
            if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("BossTest.Check",false))return;
            SessionState.SetBool("BossTest.Check",false);index=0;next=EditorApplication.timeSinceStartup+5;deadline=next+100;EditorApplication.update+=Tick;
        };}
        public static void Run(){EditorSceneManager.playModeStartScene=null;EditorSceneManager.OpenScene(BossTestSceneSetup.Path);SessionState.SetBool("BossTest.Check",true);EditorApplication.EnterPlaymode();}
        private static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Boss Test timeout");
                if(EditorApplication.timeSinceStartup<next)return;
                var test=UnityEngine.Object.FindFirstObjectByType<BossTestController>();
                if(test==null||!test.Ready)throw new Exception("Boss Test not ready");
                if((int)test.Boss.Kind!=index%4)throw new Exception("Wrong selected boss");
                if(CampaignDirector.Instance.enabled)throw new Exception("Campaign progression enabled");
                foreach(var rt in UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None))
                {
                    if(rt.name!="Battle HUD Left"&&rt.name!="Battle HUD Right")continue;
                    var corners=new Vector3[4];rt.GetWorldCorners(corners);
                    float min=RectTransformUtility.WorldToScreenPoint(null,corners[0]).x;
                    float max=RectTransformUtility.WorldToScreenPoint(null,corners[2]).x;
                    if(rt.name.EndsWith("Left")&&max>BattleSideSpace.LeftPixels+1 || rt.name.EndsWith("Right")&&min<Screen.width-BattleSideSpace.RightPixels-1)
                        throw new Exception("HUD overlaps playable road: "+rt.name);
                }
                var hp=PlayerHealth.Instance;float before=hp.CurrentHealth;hp.TakeEnvironmentalDamage(999999);
                if(!hp.IsAlive||hp.CurrentHealth!=before)throw new Exception("Invulnerability failed");
                if(test.Boss.ShotsFired==0 && test.Boss.LasersFired==0)throw new Exception("No boss attacks");
                if(index==0)
                {
                    int shots=test.Boss.ShotsFired;
                    typeof(CampaignBoss).GetMethod("FlyOrbs",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(test.Boss,new object[]{0});
                    if(test.Boss.ShotsFired!=shots+6)throw new Exception("Fly orb volley missing");
                }
                if(index==4&&test.Boss.LasersFired<3)throw new Exception("Fly and body laser relay missing");
                Debug.Log("BOSS TEST PASS selection "+index+" "+test.Boss.Kind);
                index++;
                if(index==8){Debug.Log("BOSS TEST VALIDATION SUCCESS");EditorApplication.update-=Tick;EditorApplication.Exit(0);return;}
                test.SelectBoss(index%4,1+index%3,index%3,index<4?-1:index==4?1:10);
                next=EditorApplication.timeSinceStartup+(index==4?12:9);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.update-=Tick;EditorApplication.Exit(1);}
        }
    }
}
