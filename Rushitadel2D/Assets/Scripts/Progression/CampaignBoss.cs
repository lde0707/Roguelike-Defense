using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rushitadel.Combat;
using Rushitadel.Player;
using Rushitadel.World;

namespace Rushitadel.Progression
{
    public enum BossKind { Butterfly, Lava, Ice, Twins }
    public sealed partial class CampaignBoss : MonoBehaviour
    {
        public EnemyController Health {get;private set;}
        public BossKind Kind {get;private set;}
        public string DisplayName=>Kind switch {BossKind.Butterfly=>"적월 나비",BossKind.Lava=>"용광로 거신",BossKind.Ice=>"빙환 수호자",_=>"수압·전격 쌍성"};
        public string PatternLabel {get;private set;}="등장";
        public int ShotsFired {get;private set;}
        public int LasersFired {get;private set;}
        public bool IsAttacking {get;private set;}
        public int Phase=>Health.CurrentHealth/Health.MaximumHealth>.7f?0:Health.CurrentHealth/Health.MaximumHealth>.35f?1:2;
        public string OrbAsset=>Kind switch {BossKind.Butterfly=>"Butterfly/01-pollen-pearl",BossKind.Lava=>"Lava/01-ember-orb",_=>"Ice/03-ricochet-orb"};
        private EnemyProjectileController prefab;
        private readonly List<EnemyProjectileController> bullets=new();
        private readonly List<BossLaser> lasers=new();
        private readonly List<Transform> sockets=new(),satellites=new();
        private readonly List<Vector2> homes=new();
        private readonly List<float> respawnAt=new();
        private int stage;
        private Color Accent=>Kind==BossKind.Butterfly||Kind==BossKind.Lava?new Color(1,.3f,.08f):new Color(.15f,.8f,1);
        public Vector3 DamagePosition(Vector3 query)
        {
            if(Kind!=BossKind.Twins)return transform.position;
            Vector3 left=transform.TransformPoint(new Vector3(-2,0,0));
            Vector3 right=transform.TransformPoint(new Vector3(2,0,0));
            return (left-query).sqrMagnitude<=(right-query).sqrMagnitude?left:right;
        }
        public void Configure(EnemyProjectileController template,int level)
        {
            prefab=template;stage=level;Health=GetComponent<EnemyController>();
            var theme=ForwardPressureController.Instance!=null?ForwardPressureController.Instance.Theme:PressureTheme.Grass;
            Kind=theme==PressureTheme.Grass?BossKind.Butterfly:theme==PressureTheme.Fire?BossKind.Lava:theme==PressureTheme.Ice?BossKind.Ice:BossKind.Twins;
            BuildArt();StartCoroutine(RunPatterns());
        }
        private Transform Socket(Vector2 offset)
        {var go=new GameObject("Emitter "+sockets.Count);go.transform.SetParent(transform,false);go.transform.localPosition=offset;sockets.Add(go.transform);return go.transform;}
        private void BuildArt()
        {
            transform.localScale=Vector3.one;transform.rotation=Quaternion.identity;
            GetComponent<SpriteRenderer>().enabled=false;
            foreach(var col in GetComponents<Collider2D>())col.enabled=false;
            if(Kind==BossKind.Twins)
            {
                for(int i=0;i<2;i++)
                {
                    var art=BossArt.Create(transform,"Twins/Body",new Vector2(i==0?-2:2,0),2.7f,i);
                    string variant=i==0?"a":"b";
                    art.gameObject.AddComponent<ThemeBossAnimation>().Configure(this,art,"D","boss_Type_D_"+variant+"_idle_","boss_Type_D_"+variant+"_attack_",2.7f);
                    var box=art.gameObject.AddComponent<BoxCollider2D>();box.isTrigger=true;box.size=art.sprite.bounds.size*.75f;
                    Socket(new Vector2(i==0?-2:2,-.2f));
                }
            }
            else
            {
                float width=Kind==BossKind.Butterfly?7:Kind==BossKind.Lava?8:2.7f;
                var bodyArt=BossArt.Create(transform,Kind+"/Body",Vector2.zero,width);
                string type=Kind==BossKind.Lava?"A":Kind==BossKind.Ice?"B":"C";
                string idle=Kind==BossKind.Lava?"boss_a_idle_":Kind==BossKind.Ice?"boss_Type_B_body_idle_":"boss_Type_C_boss_";
                bodyArt.gameObject.AddComponent<ThemeBossAnimation>().Configure(this,bodyArt,type,idle,Kind==BossKind.Lava?"boss_a_attack_":null,width);
                var box=gameObject.AddComponent<BoxCollider2D>();box.isTrigger=true;
                box.size=Kind==BossKind.Butterfly?new Vector2(5.6f,1.7f):Kind==BossKind.Lava?new Vector2(4,2.8f):new Vector2(2.3f,2.9f);
                if(Kind==BossKind.Butterfly)
                {Socket(new(-2,.15f));Socket(new(2,.15f));Socket(new(-1.55f,-1));Socket(new(1.55f,-1));Socket(new(0,-.65f));}
                else if(Kind==BossKind.Lava){Socket(new(-2.85f,-1.45f));Socket(new(2.85f,-1.45f));Socket(new(0,-.8f));}
                else Socket(new(0,-1.2f));
            }
            int count=Kind==BossKind.Butterfly?4:Kind==BossKind.Ice?2:0;
            for(int i=0;i<count;i++)
            {
                homes.Add(Kind==BossKind.Ice?new Vector2(i==0?-2.55f:2.55f,0):new Vector2(i%2==0?-3.8f:3.8f,i<2?0:-1.2f));
                satellites.Add(null);respawnAt.Add(0);Respawn(i);
            }
        }
        private void Respawn(int i)
        {
            var shot=Instantiate(prefab,transform.position+(Vector3)homes[i],Quaternion.identity);
            shot.gameObject.SetActive(true);shot.ConfigureBoss(Kind==BossKind.Ice?160:100,0,.28f,10000,0,0);shot.SetRewardOwner(Health);shot.Launch(Vector2.up);
            BossArt.Shot(shot.gameObject,Kind==BossKind.Ice?"Ice/01-ring-boomerang":"Butterfly/Drone",Kind==BossKind.Ice?1.35f:.65f,false);
            var partArt=shot.GetComponentsInChildren<SpriteRenderer>().Last(s=>s.enabled);
            partArt.gameObject.AddComponent<ThemeBossAnimation>().Configure(this,partArt,Kind==BossKind.Ice?"B":"C",
                Kind==BossKind.Ice?"boss_Type_B_weapon_idle_":"boss_Type_C_fly_",
                Kind==BossKind.Ice?"boss_Type_B_weapon_attack_":null,(Kind==BossKind.Ice?1.35f:.65f)/Mathf.Abs(shot.transform.lossyScale.x));
            if(Kind==BossKind.Ice)
            {
                shot.GetComponent<CircleCollider2D>().enabled=false;
                for(int n=0;n<12;n++)
                {
                    var col=shot.gameObject.AddComponent<CircleCollider2D>();col.isTrigger=true;
                    float a=n*Mathf.PI/6;col.offset=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.55f/shot.transform.localScale.x;
                    col.radius=.13f/shot.transform.localScale.x;
                }
            }
            satellites[i]=shot.transform;respawnAt[i]=float.PositiveInfinity;
        }
        private void Update()
        {
            if(Time.timeScale<=0 || Health==null || !Health.IsAlive)return;
            bullets.RemoveAll(x=>x==null);lasers.RemoveAll(x=>x==null);
            for(int i=0;i<satellites.Count;i++)if(satellites[i]==null)
            {
                if(float.IsPositiveInfinity(respawnAt[i]))respawnAt[i]=Time.time+3;
                if(Time.time>=respawnAt[i])Respawn(i);
            }
        }
        private Vector2 Aim(Vector2 from)=>PlayerHealth.Instance==null?Vector2.down:((Vector2)PlayerHealth.Instance.transform.position-from).normalized;
        public EnemyProjectileController FireAt(Vector2 from,Vector2 direction,string asset,float hp=24,float speed=3,float radius=.14f,int bounces=0,float turn=0,float returnAfter=0,float splitAfter=0,string child=null)
        {
            if(prefab==null||Health==null||!Health.IsAlive||bullets.Count(x=>x!=null)>=110)return null;
            var shot=Instantiate(prefab,from,Quaternion.identity);shot.gameObject.SetActive(true);
            shot.ConfigureBoss(hp*(1.35f+.15f*(stage-1)),speed*(1+.06f*(stage-1)),radius,7,bounces);shot.SetRewardOwner(Health);shot.Launch(direction);
            BossArt.Shot(shot.gameObject,asset,radius*2.5f,true,true);
            var motion=shot.gameObject.AddComponent<BossShotMotion>();motion.Owner=this;motion.Turn=turn;motion.ReturnAfter=returnAfter;motion.SplitAfter=splitAfter;motion.ChildArt=child;
            bullets.Add(shot);ShotsFired++;return shot;
        }
        private void Fan(Vector2 from,string asset,int count,float angle,float spread,float hp=24,float speed=3,int bounces=0)
        {for(int i=0;i<count;i++)FireAt(from,Quaternion.Euler(0,0,angle+(i-(count-1)*.5f)*spread)*Vector2.down,asset,hp,speed,.14f,bounces);}
        private BossLaser Laser(Transform socket,string asset,Vector2 heading,float width=.28f,float hold=1.4f,float sweep=0,Color? tint=null)
        {
            if(socket==null||lasers.Count(x=>x!=null)>=2)return null;
            var laser=new GameObject("Boss laser").AddComponent<BossLaser>();
            CuePart(socket,BossLaser.ChargeSeconds+BossLaser.PilotSeconds,hold,BossLaser.FadeSeconds);
            laser.Configure(this,socket,heading,asset,tint??Accent,width,hold,sweep);lasers.Add(laser);LasersFired++;return laser;
        }
        private IEnumerator RunPatterns()
        {
            yield return new WaitForSeconds(1.5f);int step=0,lastPhase=-1;
            while(Health!=null&&Health.IsAlive)
            {
                int phase=Phase;
                int[] rotation=phase==0?new[]{0,5,1}:phase==1?new[]{5,2,6,1,3}:new[]{5,4,6,2,3,0};
                int p=phase!=lastPhase?(phase==2?6:phase==1?5:0):rotation[step++%rotation.Length];
                lastPhase=phase;
                IsAttacking=true;
                if(p>=5)yield return AnimatedPattern(p);
                else if(Kind==BossKind.Butterfly)yield return Butterfly(p);
                else if(Kind==BossKind.Lava)yield return Lava(p);
                else if(Kind==BossKind.Ice)yield return Ice(p);
                else yield return Twins(p);
                IsAttacking=false;
                PatternLabel="재충전 · 공격 기회";yield return new WaitForSeconds(Phase==2?2.5f:1.8f);
            }
        }
        private void OnDestroy()
        {
            StopAllCoroutines();
            foreach(var shot in bullets)if(shot!=null)Destroy(shot.gameObject);
            foreach(var laser in lasers)if(laser!=null)Destroy(laser.gameObject);
            foreach(var part in satellites)if(part!=null)Destroy(part.gameObject);
        }
    }
}
