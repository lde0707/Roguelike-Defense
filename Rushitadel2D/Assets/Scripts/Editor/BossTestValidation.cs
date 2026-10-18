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
                var hp=PlayerHealth.Instance;float before=hp.CurrentHealth;hp.TakeEnvironmentalDamage(999999);
                if(!hp.IsAlive||hp.CurrentHealth!=before)throw new Exception("Invulnerability failed");
                if(test.Boss.ShotsFired==0 && test.Boss.LasersFired==0)throw new Exception("No boss attacks");
                Debug.Log("BOSS TEST PASS selection "+index+" "+test.Boss.Kind);
                index++;
                if(index==8){Debug.Log("BOSS TEST VALIDATION SUCCESS");EditorApplication.update-=Tick;EditorApplication.Exit(0);return;}
                test.SelectBoss(index%4,1+index%3,index%3,index<4?-1:10);
                next=EditorApplication.timeSinceStartup+9;
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.update-=Tick;EditorApplication.Exit(1);}
        }
    }
}
