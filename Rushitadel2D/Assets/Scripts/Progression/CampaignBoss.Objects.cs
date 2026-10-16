using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rushitadel.Combat;
using Rushitadel.Player;

namespace Rushitadel.Progression
{
    public sealed partial class CampaignBoss
    {
        private sealed class AttackPart
        {
            public EnemyProjectileController shot;
            public int side;
            public int copy;
            public Vector2 origin;
        }
        private readonly List<AttackPart> attackParts=new();
        private readonly List<UnityEngine.Object> partResources=new();
        public int ActiveAttackParts=>attackParts.FindAll(p=>p.shot!=null).Count;
        private string PartAsset=>Kind switch {BossKind.Butterfly=>"Butterfly/Drone",BossKind.Lava=>"Lava/03-armored-magma",BossKind.Ice=>"Ice/01-ring-boomerang",_=>"Twins/03-ricochet-crystal"};
        private string PartBeam(int side)=>Kind switch {BossKind.Butterfly=>"Butterfly/07-drone-laser",BossKind.Lava=>"Lava/07-palm-laser",BossKind.Ice=>"Ice/06-ring-laser",_=>side==0?"Twins/06-hydro-laser":"Twins/07-lightning-tether"};
        private string PartOrb(int side)=>Kind==BossKind.Twins?(side==0?"Twins/01-pressure-orb":"Twins/02-voltaic-dart"):OrbAsset;
        private bool PartAlive(AttackPart part)=>part.shot!=null&&part.shot.IsAlive&&TwinAlive(part.side);
        private void PartVolley(AttackPart part,int wave)
        {
            if(!PartAlive(part))return;
            Vector2 from=part.shot.transform.position;
            int sign=part.side==0?-1:1;
            if(Kind==BossKind.Butterfly)
                for(int i=-2;i<=2;i++)FireAt(from,Quaternion.Euler(0,0,i*22+sign*wave*8)*Vector2.down,"Butterfly/03-petal-blade",25,2.2f,.14f,0,sign*10);
            else if(Kind==BossKind.Lava)
                for(int i=-1;i<=1;i++)FireAt(from,new Vector2(i*.55f+sign*.2f,-1),"Lava/04-cluster-bomb",65,1.7f,.2f,0,0,0,1.8f,"Lava/02-slag-dart");
            else if(Kind==BossKind.Ice)
                for(int i=-1;i<=1;i++)FireAt(from,new Vector2(i*.4f+sign*.2f,-1),"Ice/02-crescent-boomerang",38,2.5f,.18f,1,0,1.1f);
            else Fan(from,PartOrb(part.side),part.side==0?5:3,sign*(20+wave*8),part.side==0?18:26,part.side==0?35:25,part.side==0?2.1f:3.1f,part.side==0?0:1);
        }
        private AttackPart MakePart(int side,int copy)
        {
            if(!TwinAlive(side))return null;
            Vector2 origin=Kind==BossKind.Ice&&satellites[side]!=null?(Vector2)satellites[side].position:sockets[Mathf.Min(side,sockets.Count-1)].position;
            if(Kind==BossKind.Twins)origin+=Vector2.right*(copy%2==0?-.8f:.8f)*ScreenFitScale;
            var shot=Instantiate(prefab,origin,Quaternion.identity);shot.gameObject.SetActive(true);
            shot.name="Attack part clone "+side+"-"+copy;
            float diameter=Kind==BossKind.Ice?.85f:Kind==BossKind.Lava?.7f:.48f;
            shot.ConfigureBoss(80+stage*20,0,diameter*.45f,24,0,0);shot.SetRewardOwner(Health);shot.Launch(Vector2.down);
            BossArt.Shot(shot.gameObject,PartAsset,diameter,false);
            if(Kind==BossKind.Twins)MakeGemMesh(shot.transform,side);
            var part=new AttackPart{shot=shot,side=side,copy=copy,origin=origin};attackParts.Add(part);bullets.Add(shot);
            ShotsFired++;return part;
        }
        private void MakeGemMesh(Transform root,int side)
        {
            foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>())renderer.enabled=false;
            var go=new GameObject(side==0?"Water prism clone":"Electric prism clone");go.transform.SetParent(root,false);
            go.transform.localScale=Vector3.one/Mathf.Abs(root.lossyScale.x);
            var mesh=new Mesh();var vertices=new List<Vector3>();var colors=new List<Color>();var indices=new List<int>();
            Color tint=side==0?new Color(.05f,.65f,1):new Color(1,.65f,.05f);
            for(int layer=0;layer<2;layer++)
            {
                float x=layer==0?.19f:.15f,y=layer==0?.26f:.22f;
                Vector3[] corners={new(0,y,0),new(x,0,0),new(0,-y,0),new(-x,0,0)};
                for(int face=0;face<4;face++)
                {
                    int start=vertices.Count;vertices.Add(Vector3.zero);vertices.Add(corners[face]);vertices.Add(corners[(face+1)%4]);
                    Color color=layer==0?new Color(.08f,.12f,.2f):face==0?Color.Lerp(tint,Color.white,.65f):face==1?tint:face==2?tint*.55f:Color.Lerp(tint,Color.white,.25f);color.a=1;
                    colors.Add(color);colors.Add(color);colors.Add(color);indices.Add(start);indices.Add(start+1);indices.Add(start+2);
                }
            }
            mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer2D=go.AddComponent<MeshRenderer>();var material=new Material(Shader.Find("Sprites/Default"));renderer2D.sharedMaterial=material;renderer2D.sortingOrder=112;
            partResources.Add(mesh);partResources.Add(material);
        }
        private IEnumerator MovePart(AttackPart part,Vector2 target,float seconds,bool contact=false)
        {
            if(!PartAlive(part))yield break;
            Vector2 start=part.shot.transform.position;bool hit=false;
            for(float t=0;t<seconds&&PartAlive(part);t+=Time.deltaTime)
            {
                Vector2 previous=part.shot.transform.position;
                float u=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/seconds));
                Vector2 next=Vector2.Lerp(start,target,u);
                part.shot.transform.position=next;
                if(contact&&!hit&&PlayerHealth.Instance!=null)
                {
                    var player=PlayerHealth.Instance;Vector2 delta=next-previous;
                    float along=delta.sqrMagnitude>.00001f?Mathf.Clamp01(Vector2.Dot((Vector2)player.transform.position-previous,delta)/delta.sqrMagnitude):0;
                    float radius=player.GetComponent<PlayerController>().BodyRadius+(Kind==BossKind.Ice?.42f:Kind==BossKind.Lava?.34f:.23f);
                    if(Vector2.Distance(player.transform.position,previous+delta*along)<radius){player.TakeDamage(18);hit=true;}
                }
                yield return null;
            }
            if(PartAlive(part))part.shot.transform.position=target;
        }
        private IEnumerator DashPart(AttackPart part,Vector2 target)
        {
            if(!PartAlive(part))yield break;
            var go=new GameObject("Part dash telegraph");go.transform.SetParent(part.shot.transform,false);
            var line=go.AddComponent<LineRenderer>();var mat=new Material(Shader.Find("Sprites/Default"));line.sharedMaterial=mat;
            partResources.Add(mat);
            line.positionCount=2;line.useWorldSpace=true;line.widthMultiplier=.035f;line.sortingOrder=114;
            line.startColor=line.endColor=new Color(1,.65f,.35f,.6f);
            line.SetPosition(0,part.shot.transform.position);line.SetPosition(1,target);
            // Lock the destination before launch; no tracking through the dodge window.
            yield return new WaitForSeconds(.9f);
            if(go!=null)Destroy(go);Destroy(mat);
            yield return MovePart(part,target,.8f,true);
            yield return new WaitForSeconds(.3f);
            yield return MovePart(part,part.origin,1.2f,true);
        }
        private IEnumerator ObjectPattern(int pattern)
        {
            string prefix=Kind switch{BossKind.Butterfly=>"나비 분신",BossKind.Lava=>"분리 용암 포대",BossKind.Ice=>"복제 빙환",_=>"쌍성 보석 분신"};
            PatternLabel=prefix+new[]{" · 레이저 포위"," · 왕복 돌진"," · 이동 포대 릴레이"}[pattern];
            var parts=new List<AttackPart>();
            int copies=pattern==2?3:2;
            for(int side=0;side<2;side++)for(int copy=0;copy<copies;copy++)
            {
                var part=MakePart(side,copy);if(part==null)continue;parts.Add(part);
                float x=(side==0?-1:1)*(1.65f+copy*.85f);
                float y=transform.position.y-.8f-copy*.55f;
                StartCoroutine(MovePart(part,new Vector2(x,y),1f));
            }
            yield return new WaitForSeconds(1.25f);
            if(pattern==1)
            {
                foreach(var part in parts)
                {
                    if(!PartAlive(part))continue;
                    var target=PlayerHealth.Instance!=null?(Vector2)PlayerHealth.Instance.transform.position:part.origin+Vector2.down*4;
                    target.x=Mathf.Clamp(target.x+(part.side==0?-.65f:.65f),-4.2f,4.2f);
                    yield return DashPart(part,target);
                    PartVolley(part,0);
                    yield return new WaitForSeconds(.35f);
                }
            }
            else
            {
                for(int copy=0;copy<copies;copy++)
                {
                    // Only one pair fires lasers at a time. Parallel beams preserve a central lane.
                    for(int side=0;side<2;side++)
                    {
                        var part=parts.Find(p=>p.side==side&&p.copy==copy);
                        if(part==null||!PartAlive(part))continue;
                        Laser(part.shot.transform,PartBeam(side),Vector2.down,.18f,1.05f,0,side==0?Color.cyan:Color.yellow,1.05f);
                    }
                    yield return new WaitForSeconds(2.55f);
                    foreach(var part in parts)PartVolley(part,copy);
                    if(pattern==2)
                    {
                        foreach(var part in parts)if(PartAlive(part))
                        {
                            Vector2 target=(Vector2)part.shot.transform.position+Vector2.down*.4f;
                            StartCoroutine(MovePart(part,target,.55f));
                        }
                    }
                    yield return new WaitForSeconds(.65f);
                }
            }
            foreach(var part in parts)if(part.shot!=null)Destroy(part.shot.gameObject);
            attackParts.RemoveAll(p=>parts.Contains(p));
            foreach(var resource in partResources)if(resource!=null)Destroy(resource);partResources.Clear();
        }
        private void CullDefeatedParts()
        {
            foreach(var part in attackParts)if(part.shot!=null&&!TwinAlive(part.side))Destroy(part.shot.gameObject);
        }
    }
}
