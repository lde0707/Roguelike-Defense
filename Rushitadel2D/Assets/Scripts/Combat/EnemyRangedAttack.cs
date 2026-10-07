using Rushitadel.Player;
using UnityEngine;

namespace Rushitadel.Combat
{
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyRangedAttack : MonoBehaviour
    {
        [SerializeField] private EnemyProjectileController projectilePrefab;
        [SerializeField, Min(0.1f)] private float shotInterval = 2f;
        [SerializeField, Min(0f)] private float initialDelay = 0.8f;
        private EnemyController owner;
        private float nextShotTime;
        private bool charging;
        private Vector2 lockedAim;
        private int volley;
        private SpriteRenderer art;
        public EnemyProjectileController ProjectilePrefab => projectilePrefab;
        private void Awake() => owner = GetComponent<EnemyController>();
        private void OnEnable() => nextShotTime = Time.time + initialDelay;
        private void Update()
        {
            var target = PlayerHealth.Instance;
            if (Time.timeScale <= 0f || !owner.IsAlive || target == null || !target.IsAlive ||
                projectilePrefab == null) return;
            var archetype=GetComponent<EnemyArchetype>();
            Vector2 aim=((Vector2)target.transform.position-(Vector2)transform.position).normalized;
            if(archetype!=null && !charging)transform.up=-aim;
            if(Time.time<nextShotTime)return;
            if(!charging)
            {
                lockedAim=aim;charging=true;
                nextShotTime=Time.time+(archetype!=null&&(archetype.Elite||archetype.Variant==3)?.85f:.4f);
                art=GetComponent<SpriteRenderer>();art.color=new Color(1,.7f,.5f);return;
            }
            charging=false;if(art!=null)art.color=Color.white;
            if(archetype==null){Emit(lockedAim,null,0,0,4);nextShotTime=Time.time+shotInterval;return;}
            int role=archetype.Variant;
            int count=role==1||role==2?3:role>=5?5:role==4?2:1;
            float spread=role==1?15:role==2?12:role>=5?12:20;
            for(int i=0;i<count;i++)
            {
                Vector2 direction=Quaternion.Euler(0,0,(i-(count-1)*.5f)*spread+(role>=5?(volley%2==0?5:-5):0))*lockedAim;
                Emit(direction,archetype,role>=5?role==5?1:2:0,role==4?(i==0?-9:9):0,role==3?5.2f:role==2?2.2f:role>=5?3:3.6f);
            }
            volley++;
            nextShotTime=Time.time+(role==0?.95f:role==3?2.4f:role>=5?2.8f:1.8f);
        }
        private void Emit(Vector2 direction,EnemyArchetype archetype,int bounces,float turn,float speed)
        {
            if(FindObjectsByType<EnemyProjectileController>(FindObjectsSortMode.None).Length>=110)return;
            var shot = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            shot.gameObject.SetActive(true);
            shot.ConfigureBoss(archetype!=null?archetype.BulletHealth:24,speed,.12f,7,bounces,archetype!=null&&archetype.Elite?16:10);
            shot.Launch(direction);
            var pressure=Rushitadel.World.ForwardPressureController.Instance;
            var theme=pressure!=null?pressure.Theme:Rushitadel.World.PressureTheme.Grass;
            EnemyProjectileArt.Apply(shot,theme,archetype!=null?archetype.Variant:0);
            if(turn!=0)shot.gameObject.AddComponent<EnemyShotCurve>().Turn=turn;
        }
    }
    public sealed class EnemyShotCurve:MonoBehaviour
    {
        public float Turn;
        private EnemyProjectileController shot;
        private void Awake()=>shot=GetComponent<EnemyProjectileController>();
        private void Update(){if(Time.timeScale>0&&shot.IsAlive&&shot.BounceCount==0)shot.SetHeading(Quaternion.Euler(0,0,Turn*Time.deltaTime)*shot.Direction);}
    }
}
