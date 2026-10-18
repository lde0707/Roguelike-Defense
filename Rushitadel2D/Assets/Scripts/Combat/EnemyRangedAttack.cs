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
        // Shared scaled-time clock gives each encounter an actual, overlapping attack opening.
        public const float FiringSeconds = 2.8f, ReloadSeconds = 1.4f;
        public static bool IsReloadWindow(float time) => Mathf.Repeat(time, FiringSeconds + ReloadSeconds) >= FiringSeconds;
        public bool IsReloading => IsReloadWindow(Time.time);
        public EnemyProjectileController ProjectilePrefab => projectilePrefab;
        private void Awake() => owner = GetComponent<EnemyController>();
        private void OnEnable() => nextShotTime = Time.time + initialDelay;
        private void Update()
        {
            var target = PlayerHealth.Instance;
            if (Time.timeScale <= 0f || !owner.IsAlive || target == null || !target.IsAlive ||
                projectilePrefab == null) return;
            var archetype=GetComponent<EnemyArchetype>();
            if (art == null) art = GetComponent<SpriteRenderer>();
            if (IsReloading)
            {
                charging = false;
                if (art != null) art.color = new Color(.55f, .65f, .75f);
                nextShotTime = Mathf.Max(nextShotTime, Time.time);
                return;
            }
            if (!charging && art != null) art.color = Color.white;
            Vector2 aim=((Vector2)target.transform.position-(Vector2)transform.position).normalized;
            if(archetype!=null && !charging)transform.up=-aim;
            if(Time.time<nextShotTime)return;
            if(!charging)
            {
                float windup = archetype!=null&&(archetype.Elite||archetype.Variant==3)?.85f:.4f;
                // Never display a charge that would fire inside the shared reload window.
                if (Mathf.Repeat(Time.time, FiringSeconds + ReloadSeconds) + windup >= FiringSeconds) return;
                lockedAim=aim;charging=true;
                nextShotTime=Time.time+windup;
                if(art!=null)art.color=new Color(1,.7f,.5f);return;
            }
            charging=false;if(art!=null)art.color=Color.white;
            if(archetype==null){Emit(lockedAim,null,0,0,4);nextShotTime=Time.time+shotInterval;return;}
            int role=archetype.Variant;
            int pattern = (role + volley / 2) % WavePattern.Names.Length;
            int count = WavePattern.Count(pattern, archetype.Elite);
            if (FindObjectsByType<EnemyProjectileController>(FindObjectsSortMode.None).Length + count > 80)
            { nextShotTime = Time.time + .5f; return; }
            // Circuit volleys are deliberately intermittent, not a permanent screen-clear loop.
            var circuit = volley % 3 == 0 ? new WaveVolley() : null;
            for(int i=0;i<count;i++)
            {
                Vector2 direction = WavePattern.Direction(lockedAim, pattern, i, count, volley);
                var shot = Emit(direction, archetype, archetype.Elite ? 1 : 0,
                    pattern == 2 ? (i % 2 == 0 ? -6 : 6) : 0, WavePattern.Speed(pattern, i, archetype.Elite));
                if (shot != null && circuit != null) circuit.Add(shot, i == count / 2);
            }
            volley++;
            nextShotTime=Time.time+(role==0?.95f:role==3?2.4f:role>=5?2.8f:1.8f);
        }
        private EnemyProjectileController Emit(Vector2 direction,EnemyArchetype archetype,int bounces,float turn,float speed)
        {
            if(FindObjectsByType<EnemyProjectileController>(FindObjectsSortMode.None).Length>=80)return null;
            var shot = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            shot.gameObject.SetActive(true);
            shot.ConfigureBoss(archetype!=null?archetype.BulletHealth:24,speed,.12f,7,bounces,archetype!=null&&archetype.Elite?16:10);
            shot.SetRewardOwner(owner);
            shot.Launch(direction);
            var pressure=Rushitadel.World.ForwardPressureController.Instance;
            var theme=pressure!=null?pressure.Theme:Rushitadel.World.PressureTheme.Grass;
            EnemyProjectileArt.Apply(shot,theme,archetype!=null?archetype.Variant:0);
            if(turn!=0)shot.gameObject.AddComponent<EnemyShotCurve>().Turn=turn;
            return shot;
        }
    }
    public sealed class EnemyShotCurve:MonoBehaviour
    {
        public float Turn;
        private EnemyProjectileController shot;
        private void Awake()=>shot=GetComponent<EnemyProjectileController>();
        private void Update(){if(Time.timeScale>0&&shot.IsAlive&&shot.BounceCount==0)shot.SetHeading(Quaternion.Euler(0,0,Turn*CombatTempo.DeltaTime)*shot.Direction);}
    }
}
