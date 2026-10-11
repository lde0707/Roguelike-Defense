using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rushitadel.Progression;
namespace Rushitadel.Combat
{
    // World-space attacks. No projectile follows the rotating muzzle after launch.
    public sealed class TransformationShot : ProjectileBehaviour
    {
        private static readonly List<TransformationShot> live=new();
        private static int serial;
        private static Sprite pixel;
        private readonly HashSet<Component> hitIds=new();
        private readonly HashSet<int> gateIds=new();
        private Dictionary<Component,int> pelletHits;
        private Vector2 origin,heading;
        private float factor=1,life=2,travel,nextTick,budget=45;
        private int form,slot,id,hitCount,ticks;
        private bool finished,returning,derived,processedGate,fragment,wasAttached;
        private Vector2 gateOrigin;
        private bool hasGateOrigin;
        private EnemyProjectileController attached;
        private Color tint;
        private Collider2D hitbox;
        private int Upgrade=>TurretAugments.UniqueRank(slot);
        private void Damage(EnemyProjectileController target,float amount)
        {
            if(target==null||!target.IsAlive)return;
            float heavy=target.MaximumHealth>=100?1+.2f*TurretAugments.Rank(slot,5):1;
            target.TakeDamage(amount*heavy*(form==1?1+.12f*Upgrade:1));
            if(TurretAugments.Rank(slot,3)>0&&target.IsAlive)target.Slow(.2f,.5f);
        }
        public override void FitVisualCollision(Vector2 size)
        {
            if(Stationary)return; // Area attacks use TickArea and gate checks instead of moving hitboxes.
            base.FitVisualCollision(size);hitbox=visualHitbox;
        }
        public int Form => form;
        public bool CanHit(EnemyProjectileController b)=>!finished && !Stationary && !wasAttached && (form!=9||hitCount<3+Upgrade) && b!=null && b.IsAlive && !hitIds.Contains(b) && (pelletHits==null || !pelletHits.TryGetValue(b,out int n) || n<(form==2?2+Upgrade:1));
        private bool Stationary=>form==6||form==8||form==10||form==11||form==13||form==14||form==15;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void Reset(){live.Clear();serial=0;pixel=null;}
        public static void Fire(int form,Vector2 position,Vector2 direction,float factor,int slot,Color tint)
        {
            int id=++serial, count=form==2?7:form==7?3:1;
            var shared=form==2||form==7?new Dictionary<Component,int>():null;
            for(int i=0;i<count;i++)
            {
                float angle=count==1?0:Mathf.Lerp(form==2?-30:-20,form==2?30:20,i/(float)(count-1));
                Create(form,position,Quaternion.Euler(0,0,angle)*direction,factor,slot,tint,id,shared);
            }
        }
        private static TransformationShot Create(int f,Vector2 p,Vector2 d,float power,int slot,Color c,int id,Dictionary<Component,int> pellets=null)
        {
            var go=new GameObject("ROT-"+f.ToString("00")+" shot");go.transform.position=p;
            var shot=go.AddComponent<TransformationShot>();shot.form=f;shot.origin=p;shot.heading=d.normalized;shot.factor=power;shot.slot=slot;shot.tint=c;shot.id=id;shot.pelletHits=pellets;shot.budget=45*power;
            shot.transform.localScale=f==4?new Vector3(.4f,.4f,1):f==3?new Vector3(.18f,.28f,1):new Vector3(.13f,.22f,1);
            shot.FaceDirection(d);
            if(shot.Stationary)
            {
                float reach=f==10?1.4f:f==13?1.6f:1.2f;
                if(f!=6 && f!=11)go.transform.position=p+d*reach;
                shot.life=f==8?1.2f:f==14?1:f==10?.8f:f==13||f==15?.7f:.08f;
                shot.life*=1+.2f*TurretAugments.Rank(slot,4);
                if(f==10)shot.life+=.2f*shot.Upgrade;
                if(f==15)shot.budget+=15*shot.Upgrade*power;
                shot.transform.localScale=f==8||f==14?new Vector3(.8f,.08f,1):f==10?new Vector3(.6f,.6f,1):f==13?new Vector3(1,.10f,1):f==15?new Vector3(.7f,.18f,1):new Vector3(.8f,.16f,1);
                int cap=f==8||f==10?2:1;
                var old=live.Where(x=>x!=null&&!x.finished&&x.form==f&&x.slot==slot).ToArray();
                for(int i=0;i<=old.Length-cap;i++)old[i].Finish();
            }
            else {var circle=go.AddComponent<CircleCollider2D>();circle.isTrigger=true;circle.radius=.5f;shot.hitbox=circle;}
            Rushitadel.Presentation.ProjectileArt.Transformation(go,f,c);
            live.Add(shot);return shot;
        }
        public static Sprite Glyph
        {
            get{if(pixel!=null)return pixel;var t=new Texture2D(5,5,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};var p=new Color[25];for(int y=0;y<5;y++)for(int x=0;x<5;x++)p[y*5+x]=(x==0||x==4)&&(y==0||y==4)?Color.clear:Color.white;t.SetPixels(p);t.Apply();return pixel=Sprite.Create(t,new Rect(0,0,5,5),new Vector2(.5f,.5f),5);}
        }
        private bool InRange(Vector2 p)=>Vector2.Distance(p,origin)<=3.001f && (!hasGateOrigin||Vector2.Distance(p,gateOrigin)<=3.001f);
        private EnemyProjectileController[] Nearby(Vector2 center,float radius)=>FindObjectsByType<EnemyProjectileController>(FindObjectsSortMode.None).Where(b=>b.IsAlive&&InRange(b.transform.position)&&Vector2.Distance(b.transform.position,center)<=radius).OrderBy(b=>Vector2.SqrMagnitude((Vector2)b.transform.position-center)).ThenBy(b=>b.GetEntityId().GetHashCode()).ToArray();
        private void Update()
        {
            if(finished||Time.timeScale<=0)return;
            life-=Time.deltaTime;
            if(attached!=null)
            {
                if(!attached.IsAlive||!InRange(attached.transform.position)){Finish();return;}
                transform.position=attached.transform.position;
                while(ticks<6+Upgrade && Time.time>=nextTick){Damage(attached,25*factor);ticks++;nextTick+=.1f;}
                if(ticks>=6+Upgrade)Finish();return;
            }
            if(wasAttached){Finish();return;}
            if(Stationary){TickArea();if(life<=0)Finish();return;}
            if(life<=0){EndFlight();return;}
            float distance=Mathf.Min(8*Time.deltaTime,3-travel);
            if(form==9&&!returning)distance=Mathf.Min(distance,1.5f-travel);
            Vector2 start=transform.position, dir=returning?-heading:heading;
            float previousTravel=travel;
            TryGates(start,start+dir*distance);
            Physics2D.SyncTransforms();
            foreach(var h in Physics2D.CapsuleCastAll(start,CastSize(form==4?.2f:.075f),CastDirection,transform.eulerAngles.z,dir,distance).OrderBy(h=>h.distance))
            {
                var b=h.collider.GetComponentInParent<EnemyProjectileController>();
                if(b!=null && CanHit(b) && InRange(b.transform.position)){transform.position=start+dir*h.distance;travel=previousTravel+h.distance;Hit(b);if(finished||attached!=null)return;}
                var e=h.collider.GetComponentInParent<EnemyController>();
                if(e!=null && e.IsAlive && !hitIds.Contains(e) && InRange(e.transform.position)){hitIds.Add(e);e.TakeDamage(BaseDamage()*.25f*factor);if(form!=4&&form!=9){Finish();return;}}
            }
            transform.position=start+dir*distance;travel=previousTravel+distance;
            if(form==9&&!returning&&travel>=1.4999f){returning=true;hitIds.Clear();hitCount=0;hitbox.enabled=true;}
            if(travel>=2.9999f || !InRange(transform.position))EndFlight();
            if(form==4||form==9)transform.Rotate(0,0,540*Time.deltaTime);
        }
        private float BaseDamage()=>fragment?10:form switch {1=>8,2=>8,3=>25,4=>22,5=>32,7=>4,9=>18,12=>20,16=>20,_=>3};
        public void Hit(EnemyProjectileController target)
        {
            if(!CanHit(target)||!InRange(target.transform.position))return;
            Component key=target;hitIds.Add(key);
            if(pelletHits!=null){pelletHits.TryGetValue(key,out int n);pelletHits[key]=n+1;}
            if(form==3&&!fragment)
            {
                if(!target.AttachDrill(this)){Finish();return;}
                attached=target;wasAttached=true;nextTick=Time.time+.1f;life=.7f;ticks=0;hitbox.enabled=false;return;
            }
            if(form==5){Explode();return;}
            Damage(target,BaseDamage()*factor*(fragment&&form==12?1+.25f*Upgrade:1));
            if(form==7)target.Corrode((derived?.75f:1.5f)+.5f*Upgrade);
            if(form==16&&!fragment)
            {
                Vector2 center=target.transform.position;
                for(int i=0;i<2+Upgrade;i++) {var next=Nearby(center,.65f).FirstOrDefault(b=>!hitIds.Contains(b));if(next==null)break;hitIds.Add(next);FlashLine(center,next.transform.position,tint);Damage(next,(i==0?12:7)*factor);center=next.transform.position;}
            }
            if(form==12&&!fragment)
            {
                for(int sign=-1;sign<=1;sign+=2){var child=Create(12,transform.position,Quaternion.Euler(0,0,90*sign)*heading,factor,slot,tint,id);child.origin=origin;child.travel=travel;child.fragment=true;child.derived=true;child.hasGateOrigin=hasGateOrigin;child.gateOrigin=gateOrigin;child.hitIds.Add(key);Rushitadel.Presentation.ProjectileArt.Transformation(child.gameObject,12,tint,true);}
            }
            hitCount++;int max=form==4?4+Upgrade:form==9?3+Upgrade:1;if(hitCount>=max) {if(form==9){hitbox.enabled=false;}else Finish();}
        }
        private void TickArea()
        {
            if(form==8||form==14)return;
            if(Time.time<nextTick)return;nextTick=Time.time+(form==10?.2f:.1f);
            int max=form==6?6:form==11?5+2*Upgrade:form==13?4+Upgrade:form==10?3:99;
            float radius=form==6?1.8f:form==11?2:form==10?.3f:form==13?.55f:.4f;
            if(form==10)radius=GetComponent<Rushitadel.Presentation.ProjectileVisual>().DisplaySize.x*.5f;
            int count=0;
            foreach(var b in Nearby(transform.position,radius))
            {
                Component key=b;
                if((form==13||form==11)&&hitIds.Contains(key))continue;
                if((form==6||form==11)&&Vector2.Angle(heading,(Vector2)b.transform.position-origin)>(form==6?22.5f:27.5f))continue;
                if(form==13&&hitIds.Count>=4+Upgrade)break;
                if(form==6){Damage(b,3*factor);b.Slow(.4f,1.2f+.3f*Upgrade);}
                if(form==11){Damage(b,12*factor);b.Push(heading);hitIds.Add(key);}
                if(form==10)Damage(b,9*factor);
                if(form==13){Damage(b,8*factor);b.Slow(.3f,.8f);hitIds.Add(key);}
                if(form==15){float before=b.CurrentHealth;b.TakeDamage(Mathf.Min(budget/b.DamageMultiplier,15*factor));budget-=Mathf.Min(before-b.CurrentHealth,budget);if(budget<=0){Finish();break;}}
                if(++count>=max)break;
            }
            if(form==10 && ++ticks>=4+Upgrade+TurretAugments.Rank(slot,4))Finish();
        }
        private void TryGates(Vector2 start,Vector2 end)
        {
            if(derived||processedGate||!TurretForms.Copyable(form))return;
            foreach(var g in live.ToArray())
            {
                if(g==null||g.finished||!TurretForms.Support(g.form)||g.slot==slot)continue;
                Vector2 point=g.transform.position, segment=end-start;
                float t=segment.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(point-start,segment)/segment.sqrMagnitude):0;
                if(Vector2.Distance(start+segment*t,point)>.4f || !g.InRange(end))continue;
                int max=g.form==8?3+g.Upgrade:1;if(!g.gateIds.Contains(id)&&g.gateIds.Count>=max)continue;
                g.gateIds.Add(id);processedGate=true;
                if(g.form==14){factor*=1.25f+.1f*g.Upgrade;GetComponent<Rushitadel.Presentation.ProjectileVisual>()?.Overcharge();}
                else
                {
                    var copy=Create(form,end+new Vector2(-heading.y,heading.x)*.07f,heading,factor*.5f,slot,tint,id,pelletHits);
                    copy.origin=origin;copy.travel=travel+Vector2.Distance(start,end);copy.hitCount=hitCount;copy.returning=returning;copy.derived=true;copy.hasGateOrigin=true;copy.gateOrigin=g.origin;
                    copy.GetComponent<Rushitadel.Presentation.ProjectileVisual>()?.SmallerCopy();
                    foreach(Component key in hitIds)copy.hitIds.Add(key);
                }
                break;
            }
        }
        private void Explode(){foreach(var b in Nearby(transform.position,.65f).Take(8+2*Upgrade))Damage(b,32*factor);Flash(transform.position,tint,1.3f);Finish();}
        private void EndFlight(){if(form==5)Explode();else Finish();}
        private void Finish(){if(finished)return;finished=true;if(hitbox!=null)hitbox.enabled=false;live.Remove(this);Destroy(gameObject);}
        private void OnDestroy()=>live.Remove(this);
        public static void Flash(Vector2 p,Color color,float size)=>Rushitadel.Presentation.ProjectileArt.Burst(p,"BlastRing",Vector2.one*size,color,.18f);
        private static void FlashLine(Vector2 a,Vector2 b,Color color)=>Rushitadel.Presentation.ProjectileArt.Arc(a,b,color);
    }
}
