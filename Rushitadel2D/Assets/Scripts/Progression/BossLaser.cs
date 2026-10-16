using UnityEngine;
using Rushitadel.Combat;
using Rushitadel.Player;

namespace Rushitadel.Progression
{
    // Preview and fade are harmless. The visible extending core is the only damaging segment.
    public sealed class BossLaser : ProjectileBehaviour
    {
        public const float ChargeSeconds=1.25f, PilotSeconds=.10f, FadeSeconds=.30f;
        public CampaignBoss Owner;
        public Transform Socket;
        public Transform EndSocket;
        public bool RequireEnd;
        public bool IsDamaging {get;private set;}
        public float Age {get;private set;}
        public float Length {get;private set;}
        public float Width {get;private set;}
        private Vector2 heading;
        private float hold,sweep;
        private SpriteRenderer beam,charge;
        private LineRenderer warning;
        private Color color;
        private bool horizontal;
        private float visibleWidth;
        private readonly Transform[] motes=new Transform[8];
        private Transform ringArt;
        private Vector3 ringScale;
        public void Configure(CampaignBoss owner,Transform socket,Vector2 direction,string asset,Color tint,float width,float duration,float sweepDegrees=0)
        {
            Owner=owner;Socket=socket;heading=direction.normalized;color=tint;Width=width;hold=duration;sweep=sweepDegrees;
            beam=BossArt.Create(transform,asset,Vector2.zero,1);beam.sortingOrder=115;
            beam.sharedMaterial=BossArt.HostileMaterial;
            beam.sprite=BossArt.Beam(asset);horizontal=beam.sprite.bounds.size.x>beam.sprite.bounds.size.y;
            beam.drawMode=SpriteDrawMode.Tiled;
            beam.tileMode=SpriteTileMode.Continuous;
            charge=BossArt.Create(transform,owner.OrbAsset,Vector2.zero,.2f);charge.sortingOrder=116;
            warning=gameObject.AddComponent<LineRenderer>();warning.sharedMaterial=new Material(Shader.Find("Sprites/Default"));
            warning.positionCount=2;warning.useWorldSpace=true;warning.sortingOrder=114;warning.widthMultiplier=.035f;
            for(int i=0;i<motes.Length;i++)
            {
                var go=new GameObject("Charging pixel");go.transform.SetParent(transform,false);
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=TransformationShot.Glyph;sr.color=tint;sr.sortingOrder=117;
                sr.sharedMaterial=warning.sharedMaterial;
                go.transform.localScale=Vector3.one*.045f;motes[i]=go.transform;
            }
            if(owner.Kind==BossKind.Ice && socket.childCount>0 && socket.GetComponentInChildren<ThemeBossAnimation>()==null)
            {ringArt=socket.GetChild(socket.childCount-1);ringScale=ringArt.localScale;}
        }
        private void Update()
        {
            if(Time.timeScale<=0)return;
            if(Owner==null || !Owner.Health.IsAlive || Socket==null || !Socket.gameObject.activeInHierarchy || (RequireEnd&&EndSocket==null)){Destroy(gameObject);return;}
            Age+=Time.deltaTime;transform.position=Socket.position;
            float activeAge=Age-ChargeSeconds-PilotSeconds;
            IsDamaging=activeAge>=0 && activeAge<hold;
            if(Age>ChargeSeconds+PilotSeconds+hold+FadeSeconds){Destroy(gameObject);return;}
            Vector2 d=Quaternion.Euler(0,0,sweep*Mathf.Clamp01(activeAge/Mathf.Max(.001f,hold)))*heading;
            Vector2 start=Socket.position;
            if(Owner.Kind==BossKind.Butterfly && Socket.GetComponent<EnemyProjectileController>()!=null)start+=Vector2.down*.12f;
            float limit=18;
            if(EndSocket!=null){Vector2 delta=(Vector2)EndSocket.position-start;limit=delta.magnitude;d=delta.normalized;}
            if(PlayfieldBoundary.Instance!=null && PlayfieldBoundary.Instance.TryGetExitHit(start,d,0,limit,out var wall,out _))limit=wall;
            var camera=Camera.main;
            if(camera!=null && d.y<-.001f)limit=Mathf.Min(limit,Mathf.Max(0,(camera.transform.position.y-camera.orthographicSize-start.y)/d.y));
            Length=activeAge>=0?limit*Mathf.Clamp01(activeAge/.15f):limit;
            warning.enabled=Age<ChargeSeconds;
            warning.startColor=warning.endColor=new Color(color.r,color.g,color.b,.35f+.2f*Mathf.Sin(Age*18));
            warning.SetPosition(0,start);warning.SetPosition(1,start+d*limit);
            beam.enabled=Age>=ChargeSeconds;
            if(beam.sprite!=null)
            {
                beam.transform.position=start+d*Length*.5f;
                beam.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg+(horizontal?0:90));
                float w=activeAge<0?Width*.15f:Width*Mathf.Lerp(.3f,1,Mathf.Clamp01(activeAge/.15f));
                // Boss telegraphs retain their length, but no longer inherit doubled common-shot width.
                w*=.85f;
                visibleWidth=w;
                float tileScale=w/(horizontal?beam.sprite.bounds.size.y:beam.sprite.bounds.size.x);
                beam.transform.localScale=new Vector3(tileScale,tileScale,1);
                beam.size=horizontal?new Vector2(Length/tileScale,beam.sprite.bounds.size.y):new Vector2(beam.sprite.bounds.size.x,Length/tileScale);
                beam.color=new Color(1,1,1,activeAge>hold?1-(activeAge-hold)/FadeSeconds:1);
            }
            charge.transform.position=start;
            charge.transform.localScale=Vector3.one*(Mathf.Lerp(.12f,.45f,Mathf.Clamp01(Age/ChargeSeconds))/charge.sprite.bounds.size.x);
            charge.color=new Color(1,1,1,activeAge>hold?Mathf.Clamp01(1-(activeAge-hold)/FadeSeconds):1);
            for(int i=0;i<motes.Length;i++)
            {
                motes[i].gameObject.SetActive(Age<ChargeSeconds);
                float a=i*Mathf.PI*.25f+Age*1.5f,r=.7f*(1-Mathf.Repeat(Age*1.5f+i*.13f,1));
                motes[i].localPosition=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0);
            }
            if(ringArt!=null)ringArt.localScale=new Vector3(ringScale.x,ringScale.y*Mathf.Lerp(1,.45f,Mathf.Clamp01(Age/.35f)),ringScale.z);
            var player=PlayerHealth.Instance;
            if(IsDamaging && player!=null && player.IsAlive)
            {
                Vector2 p=player.transform.position;
                float along=Mathf.Clamp(Vector2.Dot(p-start,d),0,Length);
                float radius=player.GetComponent<PlayerController>().BodyRadius;
                if(Vector2.Distance(p,start+d*along)<visibleWidth*.5f+radius)player.TakeDamage(18);
            }
        }
        private void OnDestroy(){if(warning!=null)Destroy(warning.sharedMaterial);if(ringArt!=null)ringArt.localScale=ringScale;}
    }
}
