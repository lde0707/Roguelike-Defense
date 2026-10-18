using System.Collections;
using System.Linq;
using UnityEngine;

namespace Rushitadel.Progression
{
    public sealed partial class CampaignBoss
    {
        private void CuePart(Transform source,float charge,float hold,float recover)
        {
            var part=source.GetComponentInChildren<ThemeBossAnimation>();
            if(part==null)
            {
                // Twins have independent bodies; choose the body belonging to this emitter.
                part=GetComponentsInChildren<ThemeBossAnimation>()
                    .OrderBy(a=>(a.transform.position-source.position).sqrMagnitude).FirstOrDefault();
            }
            if(part!=null)part.Cue(charge,hold,recover,Kind==BossKind.Ice?0:.5f,Kind==BossKind.Ice?.5f:1);
        }
        private IEnumerator AnimatedPattern(int pattern)
        {
            if(Kind==BossKind.Lava)yield return PalmCadence(pattern==6);
            else if(Kind==BossKind.Ice)yield return RingDeployment(pattern==6);
            else if(Kind==BossKind.Butterfly)yield return WingPulse(pattern==6);
            else yield return CrystalRelay(pattern==6);
        }

        // A: palms unfold during the warning, remain open throughout emission, then close.
        private IEnumerator PalmCadence(bool ricochet)
        {
            PatternLabel=ricochet?"개방 용광로 · 반사 박동":"손바닥 충전 · 교대 포격";
            if(ricochet)
            {
                CuePart(sockets[0],.9f,1.2f,.65f);
                yield return new WaitForSeconds(.9f);
                for(int pulse=0;pulse<3;pulse++)
                {
                    for(int side=0;side<2;side++)
                        FireAt(sockets[side].position,new Vector2(side==0?-1:1,-.7f-pulse*.25f),
                            "Lava/03-armored-magma",65,2.4f,.23f,1);
                    // Sparse centre fan leaves a choice between dodging and breaking the heavy side orbs.
                    Fan(sockets[2].position,OrbAsset,3,pulse*12-12,28,16,2.2f);
                    yield return new WaitForSeconds(.4f);
                }
                yield return new WaitForSeconds(.9f);
                yield break;
            }
            for(int side=0;side<2;side++)
            {
                // Snapshot aim: the telegraphed line never tracks the player after charging begins.
                Laser(sockets[side],"Lava/07-palm-laser",Aim(sockets[side].position),.3f,1.1f);
                yield return new WaitForSeconds(BossLaser.ChargeSeconds+BossLaser.PilotSeconds);
                Fan(sockets[1-side].position,OrbAsset,5,side==0?-18:18,18,18,2.2f);
                yield return new WaitForSeconds(1.1f+BossLaser.FadeSeconds+.55f);
            }
        }

        // B: deploy, hold the upright attack frame while charging/firing, then return home.
        private IEnumerator RingDeployment(bool boomerang)
        {
            PatternLabel=boomerang?"빙환 왕복 · 귀환 포대":"전개 고리 · 순차 소각선";
            for(int side=0;side<2;side++)
            {
                var ring=satellites[side];if(ring==null)continue;
                if(boomerang)yield return FlyRing(side);
                else
                {
                    Vector3 target=transform.position+new Vector3(side==0?-3.6f:3.6f,-1.1f,0);
                    yield return MoveRing(ring,target,.65f);
                }
                if(ring==null)continue;
                Laser(ring,"Ice/06-ring-laser",Vector2.down,.25f,1.2f);
                yield return new WaitForSeconds(BossLaser.ChargeSeconds+BossLaser.PilotSeconds);
                for(int sign=-1;sign<=1;sign+=2)
                    FireAt(sockets[0].position,new Vector2(sign*.55f,-1),"Ice/02-crescent-boomerang",42,2.5f,.18f,0,0,1.25f);
                yield return new WaitForSeconds(1.2f+BossLaser.FadeSeconds+.25f);
                if(ring!=null)yield return MoveRing(ring,transform.position+(Vector3)homes[side],.6f);
            }
        }
        private IEnumerator MoveRing(Transform ring,Vector3 target,float duration)
        {
            if(ring==null)yield break;
            Vector3 from=ring.position;
            for(float elapsed=0;elapsed<duration;elapsed+=Time.deltaTime)
            {if(ring==null)yield break;ring.position=Vector3.Lerp(from,target,Mathf.SmoothStep(0,1,elapsed/duration));yield return null;}
            if(ring!=null)ring.position=target;
        }

        // C has no separate attack clip: the flap cycle itself is the windup and firing cue.
        private IEnumerator WingPulse(bool relay)
        {
            PatternLabel=relay?"접힌 날개 · 호위 교차 릴레이":"날갯짓 · 삼중 꽃가루 파동";
            for(int pulse=0;pulse<(relay?2:3);pulse++)
            {
                CuePart(sockets[4],.8f,.2f,.65f);
                yield return new WaitForSeconds(.8f);
                for(int side=0;side<2;side++)
                    Fan(sockets[side].position,"Butterfly/01-pollen-pearl",5,(pulse-1)*12*(side==0?1:-1),17,16,2.3f);
                FlyOrbs(pulse%2);
                if(relay)
                {
                    // Only one diagonal pair is active; next pair waits for fade-out.
                    foreach(int index in pulse==0?new[]{0,3}:new[]{1,2})
                        if(satellites[index]!=null)Laser(satellites[index],"Butterfly/07-drone-laser",Vector2.down,.16f,.85f);
                    yield return new WaitForSeconds(BossLaser.ChargeSeconds+BossLaser.PilotSeconds+.85f+BossLaser.FadeSeconds+.4f);
                }
                else yield return new WaitForSeconds(1.05f);
            }
        }

        // D: each crystal unit charges independently; a released pair returns rather than tracking.
        private IEnumerator CrystalRelay(bool prisms)
        {
            PatternLabel=prisms?"결정 방출 · 왕복 협공":"수압·전격 · 공명 교대";
            for(int side=0;side<2;side++)
            {
                var socket=sockets[side];
                if(prisms)
                {
                    CuePart(socket,.85f,.45f,.6f);yield return new WaitForSeconds(.85f);
                    for(int sign=-1;sign<=1;sign+=2)
                        FireAt((Vector2)socket.position+Vector2.right*sign*.95f,new Vector2(sign*.4f,-1),
                            "Twins/03-ricochet-crystal",60,2.5f,.2f,1,0,1.35f);
                    Fan(socket.position,side==0?"Twins/01-pressure-orb":"Twins/02-voltaic-dart",5,0,23,18,2.4f);
                    yield return new WaitForSeconds(1.65f);
                }
                else
                {
                    Laser(socket,side==0?"Twins/06-hydro-laser":"Twins/07-lightning-tether",
                        Vector2.down,.28f,1.1f,0,side==0?Color.cyan:Color.yellow);
                    yield return new WaitForSeconds(BossLaser.ChargeSeconds+BossLaser.PilotSeconds);
                    CuePart(sockets[1-side],.5f,.15f,.5f);yield return new WaitForSeconds(.5f);
                    Fan(sockets[1-side].position,"Twins/01-pressure-orb",5,side==0?12:-12,20,20,2.4f);
                    yield return new WaitForSeconds(.6f+BossLaser.FadeSeconds+.55f);
                }
            }
        }
    }
}
