using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Rushitadel.Progression;
using Rushitadel.Presentation;
namespace Rushitadel.Editor
{
    [InitializeOnLoad]
    public static class LavaPalmValidation
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static int step;static double next;static CampaignBoss boss;static BossLaser laser;
        static LavaPalmValidation(){EditorApplication.playModeStateChanged+=s=>{
            if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("LavaPalm.Check",false))return;
            SessionState.SetBool("LavaPalm.Check",false);step=0;next=EditorApplication.timeSinceStartup+4;EditorApplication.update+=Tick;
        };}
        public static void Run(){EditorSceneManager.playModeStartScene=null;EditorSceneManager.OpenScene(BossTestSceneSetup.Path);SessionState.SetBool("LavaPalm.Check",true);EditorApplication.EnterPlaymode();}
        static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup<next)return;
            try
            {
                var test=UnityEngine.Object.FindFirstObjectByType<BossTestController>();
                Check(test!=null&&test.Ready,"Test setup failed");
                if(step==0){test.SelectBoss(1,1,0,5);step=1;next=EditorApplication.timeSinceStartup+4;return;}
                boss=test.Boss;
                var sockets=(List<Transform>)typeof(CampaignBoss).GetField("sockets",Flags).GetValue(boss);
                if(step==1)
                {
                    Check(boss.Kind==BossKind.Lava,"Expected Lava");boss.StopAllCoroutines();
                    var animation=boss.GetComponentInChildren<ThemeBossAnimation>();
                    var art=animation.GetComponent<SpriteRenderer>();
                    var clip=typeof(ThemeBossAnimation).GetField("clip",Flags).GetValue(animation);
                    var frames=((Sprite[])clip.GetType().GetField("idle").GetValue(clip)).Concat((Sprite[])clip.GetType().GetField("attack").GetValue(clip)).ToArray();
                    var originals=Resources.LoadAll<Texture2D>("ThemeContent/A/Boss");
                    int left=384,bottom=216;
                    foreach(var t in originals)for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++)if(t.GetPixel(x,y).a>.4f){left=Math.Min(left,x);bottom=Math.Min(bottom,y);}
                    foreach(float scale in new[]{1f,.85f})
                    {
                        boss.transform.localScale=Vector3.one*scale;
                        foreach(var frame in frames)
                        {
                            typeof(ThemeBossAnimation).GetMethod("SetFrame",Flags).Invoke(animation,new object[]{frame});
                            var source=originals.First(t=>t.name==frame.texture.name);
                            for(int side=0;side<2;side++)
                            {
                                Vector3 local=art.transform.InverseTransformPoint(sockets[side].position);
                                int x=Mathf.RoundToInt(local.x*100+frame.rect.width*.5f+left-.5f);
                                int y=Mathf.RoundToInt(local.y*100+frame.rect.height*.5f+bottom-.5f);
                                Color c=source.GetPixel(x,y);
                                Check(c.r>.86f&&c.g>.5f&&c.a>.5f,"Emitter misses palm core: "+source.name+" side "+side);
                                Check(side==0?x<140:x>240,"Emitter on wrong body part");
                            }
                        }
                    }
                    Debug.Log("LAVA PALM PASS: all 33 frames, both palms, two boss scales");
                    laser=(BossLaser)typeof(CampaignBoss).GetMethod("Laser",Flags).Invoke(boss,new object[]{sockets[0],"Lava/07-palm-laser",Vector2.down,.3f,1.1f,0f,null,1.25f});
                    var shot=boss.FireAt(sockets[1].position,Vector2.down,boss.OrbAsset);
                    Check(shot!=null&&Vector3.Distance(shot.transform.position,sockets[1].position)<.001f,"Orb origin mismatch");
                    step=2;next=EditorApplication.timeSinceStartup+.6;return;
                }
                Check(laser!=null,"Missing laser");
                var charge=(SpriteRenderer)typeof(BossLaser).GetField("charge",Flags).GetValue(laser);
                Check(Vector3.Distance(charge.transform.position,sockets[0].position)<.001f,"Charge does not follow palm");
                var warning=(LineRenderer)typeof(BossLaser).GetField("warning",Flags).GetValue(laser);
                Check(Vector3.Distance(warning.GetPosition(0),sockets[0].position)<.001f,"Laser warning does not start at palm");
                Debug.Log("LAVA PALM VALIDATION SUCCESS");EditorApplication.update-=Tick;EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.update-=Tick;EditorApplication.Exit(1);}
        }
    }
}
