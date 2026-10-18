using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rushitadel.Combat;
using Rushitadel.Presentation;
using Rushitadel.Progression;
namespace Rushitadel.Editor
{
    [InitializeOnLoad]
    public static class CombatTempoValidation
    {
        static double next;
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static CombatTempoValidation(){EditorApplication.playModeStateChanged+=s=>{
            if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("Tempo.Check",false))return;
            SessionState.SetBool("Tempo.Check",false);next=EditorApplication.timeSinceStartup+5;EditorApplication.update+=Tick;
        };}
        public static void Run(){EditorSceneManager.playModeStartScene=null;EditorSceneManager.OpenScene(BossTestSceneSetup.Path);SessionState.SetBool("Tempo.Check",true);EditorApplication.EnterPlaymode();}
        static void Equal(float actual,float expected,string name){if(Mathf.Abs(actual-expected)>.001f)throw new Exception(name+": "+actual+" != "+expected);Debug.Log("TEMPO PASS: "+name);}
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup<next)return;
            EditorApplication.update-=Tick;
            try
            {
                var boss=UnityEngine.Object.FindFirstObjectByType<BossTestController>().Boss;
                boss.StopAllCoroutines();CampaignDirector.ClearProjectiles();
                float dt=Time.deltaTime;
                var allied=new GameObject("Tempo test allied").AddComponent<ProjectileController>();
                allied.transform.position=new Vector3(0,-3,0);allied.Launch(Vector2.right,1,3);
                typeof(ProjectileController).GetMethod("Update",Flags).Invoke(allied,null);
                Equal(allied.DistanceTravelled,10*dt*1.7f,"allied speed x1.7");
                var enemy=boss.FireAt(new Vector2(0,1),Vector2.down,boss.OrbAsset,20,2);
                var from=enemy.transform.position;
                typeof(EnemyProjectileController).GetMethod("Update",Flags).Invoke(enemy,null);
                Equal(Vector3.Distance(from,enemy.transform.position),2*dt*1.7f,"enemy speed x1.7");
                var socket=new GameObject("Tempo laser socket").transform;socket.position=new Vector3(2,2,0);
                var laser=new GameObject("Tempo laser").AddComponent<BossLaser>();
                laser.Configure(boss,socket,Vector2.down,"Butterfly/06-boss-laser",Color.red,.2f,1);
                typeof(BossLaser).GetMethod("Update",Flags).Invoke(laser,null);
                Equal(laser.Age,dt*1.7f,"laser clock x1.7");
                Equal(Time.timeScale,1,"global timescale unchanged");
                if(laser.WindupSeconds/CombatTempo.SpeedMultiplier<.65f)throw new Exception("Unsafe telegraph");
                Debug.Log("COMBAT TEMPO VALIDATION SUCCESS");EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
