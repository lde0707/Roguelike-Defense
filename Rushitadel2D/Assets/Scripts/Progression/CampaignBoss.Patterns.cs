using System.Collections;
using UnityEngine;
using Rushitadel.Player;

namespace Rushitadel.Progression
{
    public sealed partial class CampaignBoss
    {
        private IEnumerator Butterfly(int p)
        {
            PatternLabel=new[]{"개화 나선","나비 레이저 릴레이","날개 가위질","비늘 장막","붉은 정원"}[p];
            if(p==0 || p==4)
            {
                for(int wave=0;wave<6;wave++)
                {
                    for(int side=0;side<2;side++)Fan(sockets[side].position,OrbAsset,5,(wave*7-18)*(side==0?1:-1),15,12,2.4f);
                    if(wave%2==0)for(int side=2;side<4;side++)FireAt(sockets[side].position,Aim(sockets[side].position),"Butterfly/02-thorn-needle",12,4);
                    if(wave==1 || wave==4)FlyOrbs(wave==1?0:1);
                    yield return new WaitForSeconds(.4f);
                }
                if(p==0)yield break;yield return new WaitForSeconds(1);
            }
            if(p==1 || p==4)
            {
                for(int pair=0;pair<2;pair++)
                {
                    foreach(int index in pair==0?new[]{0,3}:new[]{1,2})if(satellites[index]!=null)
                        Laser(satellites[index],"Butterfly/07-drone-laser",Aim(satellites[index].position),.16f,.9f);
                    yield return new WaitForSeconds(2.9f);
                    FlyOrbs(pair);
                    for(int side=0;side<2;side++)FireAt(sockets[side].position,Vector2.down,"Butterfly/03-petal-blade",45,2.5f,.22f,0,side==0?12:-12);
                }
                // The body follows the fly relay after its beams have faded.
                yield return new WaitForSeconds(.4f);
                Laser(sockets[4],"Butterfly/06-boss-laser",Aim(sockets[4].position),.28f,1.1f,0,null,1.05f);
                Fan(sockets[0].position,OrbAsset,3,-18,20,20,2.2f);
                Fan(sockets[1].position,OrbAsset,3,18,20,20,2.2f);
                yield return new WaitForSeconds(3.2f);
            }
            if(p==2)
            {
                // Parallel lanes retain the middle route; no simultaneous centre-crossing beams.
                for(int side=0;side<2;side++)Laser(sockets[side],"Butterfly/06-boss-laser",Vector2.down,.28f,1.5f);
                for(int i=0;i<2;i++)FireAt(sockets[4].position,new Vector2(i==0?-.15f:.15f,-1),"Butterfly/04-seed-pod",60,1.5f,.25f,0,0,0,1.7f,"Butterfly/02-thorn-needle");
                yield return new WaitForSeconds(3.4f);
            }
            if(p==3)
            {
                for(int wave=0;wave<3;wave++)
                {
                    int gap=2+wave;
                    for(int i=0;i<8;i++)if(i!=gap&&i!=gap+1)FireAt((Vector2)transform.position+new Vector2(-4.2f+i*1.2f,-.8f),Vector2.down,"Butterfly/05-scale-kite",36,2.3f,.26f);
                    yield return new WaitForSeconds(1.2f);
                }
            }
        }
        private void FlyOrbs(int pair)
        {
            foreach(int index in pair==0?new[]{0,3}:new[]{1,2})
            {
                if(satellites[index]==null)continue;
                Vector2 from=(Vector2)satellites[index].position+Vector2.down*.12f;
                Vector2 aim=Aim(from);
                for(int n=-1;n<=1;n++)FireAt(from,Quaternion.Euler(0,0,n*18)*aim,OrbAsset,18,2.2f,.14f);
            }
        }
        private IEnumerator Lava(int p)
        {
            PatternLabel=new[]{"쌍나선 용광로","절벽 반사 협공","분열하는 탄벽","손 레이저 가위질","용광로 압착"}[p];
            if(p==0)
            {
                for(int wave=0;wave<6;wave++)
                {for(int side=0;side<2;side++)Fan(sockets[side].position,OrbAsset,7,(wave*8-20)*(side==0?1:-1),13,12,2.7f);yield return new WaitForSeconds(.35f);}
            }
            if(p==1)
            {
                for(int side=0;side<2;side++)for(int i=0;i<3;i++)FireAt(sockets[side].position,new Vector2(side==0?-1:1,-.45f-i*.2f),"Lava/03-armored-magma",90,2.5f,.28f,1);
                yield return new WaitForSeconds(.7f);Fan(sockets[2].position,OrbAsset,7,0,16,12,2.1f);yield return new WaitForSeconds(1.5f);
            }
            if(p==2)
            {
                for(int i=0;i<5;i++)FireAt((Vector2)transform.position+new Vector2((i-2)*1.5f,-.8f),Vector2.down,"Lava/04-cluster-bomb",54,1.6f,.24f,0,0,0,1.7f,"Lava/02-slag-dart");
                yield return new WaitForSeconds(2.5f);
            }
            if(p>=3)
            {
                for(int side=0;side<2;side++)
                {
                    Laser(sockets[side],"Lava/07-palm-laser",Quaternion.Euler(0,0,side==0?-15:15)*Vector2.down,.4f,1.4f,side==0?20:-20);
                    if(p==4)
                    {
                        FireAt(sockets[1-side].position,Vector2.down,"Lava/05-heat-crescent",54,2,.24f,0,side==0?15:-15);
                        FireAt(sockets[2].position,new Vector2(side==0?-.4f:.4f,-1),"Lava/06-furnace-mine",72,.9f,.3f,0,0,0,2,"Lava/01-ember-orb");
                    }
                    yield return new WaitForSeconds(3.4f);
                }
            }
        }
        private IEnumerator FlyRing(int index)
        {
            var ring=satellites[index];if(ring==null)yield break;
            Vector2 start=ring.position,target=PlayerHealth.Instance.transform.position;
            target.x=Mathf.Clamp(target.x+(index==0?-1.3f:1.3f),-4,4);
            var warning=Laser(ring,"Ice/06-ring-laser",(target-start).normalized,.05f,0);
            yield return new WaitForSeconds(.65f);if(warning!=null)Destroy(warning.gameObject);
            float t=0;
            while(t<2.8f && ring!=null)
            {
                t+=Time.deltaTime;float u=Mathf.Clamp01(t/2.8f);
                ring.position=Vector2.Lerp(start,target,Mathf.Sin(u*Mathf.PI))+Vector2.right*Mathf.Sin(u*2*Mathf.PI)*(index==0?-.65f:.65f);
                ring.GetChild(ring.childCount-1).Rotate(0,0,Time.deltaTime*180);
                var player=PlayerHealth.Instance;
                if(player!=null && Time.timeScale>0)
                {
                    float distance=Vector2.Distance(player.transform.position,ring.position),r=player.GetComponent<PlayerController>().BodyRadius;
                    if(distance+r>.45f && distance-r<.67f)player.TakeDamage(16);
                }
                yield return null;
            }
            if(ring!=null)
            {
                ring.position=start;ring.rotation=Quaternion.identity;
                if(ring.childCount>0)ring.GetChild(ring.childCount-1).localRotation=Quaternion.identity;
            }
        }
        private IEnumerator Ice(int p)
        {
            PatternLabel=new[]{"엇갈리는 왕복 고리","부메랑 포대","반사 회랑","레이저 릴레이","빙환 봉쇄"}[p];
            if(p==0 || p==4)
            {
                StartCoroutine(FlyRing(0));yield return new WaitForSeconds(.65f);StartCoroutine(FlyRing(1));
                Fan(sockets[0].position,"Ice/05-frost-dart",5,0,22,12,3.2f);
                yield return new WaitForSeconds(3.6f);if(p==0)yield break;
            }
            if(p==1||p==3||p==4)
            {
                for(int side=0;side<2;side++)
                {
                    if(satellites[side]!=null)Laser(satellites[side],"Ice/06-ring-laser",Vector2.down,.28f,1.6f,p==3?(side==0?15:-15):0);
                    for(int i=0;i<2;i++)FireAt((Vector2)transform.position+homes[1-side],new Vector2(i==0?-.3f:.3f,-1),"Ice/02-crescent-boomerang",54,2.7f,.24f,0,0,1.15f);
                    yield return new WaitForSeconds(3.6f);
                }
            }
            if(p==2)
            {
                for(int side=0;side<2;side++)if(satellites[side]!=null)Laser(satellites[side],"Ice/06-ring-laser",Vector2.down,.24f,1.6f);
                for(int side=-1;side<=1;side+=2)for(int i=0;i<2;i++)FireAt(sockets[0].position,new Vector2(side,-.6f-i*.4f),"Ice/03-ricochet-orb",36,2.7f,.17f,2);
                yield return new WaitForSeconds(3.4f);
            }
            if(p==4)for(int side=-1;side<=1;side+=2)FireAt(sockets[0].position,new Vector2(side*.4f,-1),"Ice/04-heavy-prism",90,2,.28f,1);
        }
        private IEnumerator Twins(int p)
        {
            PatternLabel=new[]{"수압 부채 · 전격 조준","수압 레이저 릴레이","도탄 결정 협공","프리즘 기뢰","과부하 연계"}[p];
            if(p==0)
            {
                for(int wave=0;wave<3;wave++)
                {Fan(sockets[0].position,"Twins/01-pressure-orb",11,wave*6-6,12,24,2.5f);
                    FireAt(sockets[1].position,Aim(sockets[1].position),"Twins/02-voltaic-dart",12,4.5f);yield return new WaitForSeconds(.65f);}
            }
            if(p==1 || p==4)
            {
                for(int side=0;side<2;side++)
                {
                    Laser(sockets[side],side==0?"Twins/06-hydro-laser":"Twins/07-lightning-tether",Aim(sockets[side].position),.32f,1.4f,0,side==0?Color.cyan:Color.yellow);
                    FireAt(sockets[1-side].position,Vector2.down,"Twins/04-tidal-crescent",60,2,.35f,0,side==0?8:-8);
                    yield return new WaitForSeconds(3.4f);
                }
            }
            if(p==2)
            {
                for(int side=0;side<2;side++)for(int i=0;i<3;i++)FireAt(sockets[side].position,new Vector2(side==0?-1:1,-.5f-i*.25f),"Twins/03-ricochet-crystal",90,3,.25f,2);
                yield return new WaitForSeconds(2);
            }
            if(p>=3)
            {
                for(int side=0;side<2;side++)FireAt(sockets[side].position,Vector2.down,p==4?"Twins/08-overload-core":"Twins/05-prism-mine",p==4?150:40,1,.3f,0,0,0,1.8f,"Twins/02-voltaic-dart");
                if(p==3)
                {
                    var a=FireAt((Vector2)transform.position+new Vector2(-2,-1.6f),Vector2.down,"Twins/05-prism-mine",50,0,.24f);
                    var b=FireAt((Vector2)transform.position+new Vector2(2,-1.6f),Vector2.down,"Twins/05-prism-mine",50,0,.24f);
                    if(a!=null&&b!=null)
                    {
                        var tether=Laser(a.transform,"Twins/07-lightning-tether",Vector2.right,.18f,1.4f,0,Color.yellow);
                        if(tether!=null){tether.EndSocket=b.transform;tether.RequireEnd=true;}
                    }
                }
                yield return new WaitForSeconds(2.4f);
            }
        }
    }
}
