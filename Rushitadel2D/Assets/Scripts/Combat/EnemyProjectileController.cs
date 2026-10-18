using System.Collections.Generic;
using Rushitadel.Player;
using UnityEngine;

namespace Rushitadel.Combat
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class EnemyProjectileController : ProjectileBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maximumHealth = 25f;
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0f)] private float speed = 4f;
        [SerializeField, Min(0.01f)] private float collisionRadius = 0.12f;
        [SerializeField, Min(0.1f)] private float lifetime = 12f;
        private readonly List<RaycastHit2D> hits = new();
        private CircleCollider2D hitbox;
        private Vector2 direction;
        private float health;
        private float remainingLifetime;
        private bool flying;
        private EnemyController rewardOwner;
        private WaveVolley volleyGroup;
        private bool volleyCore, counterArmed;
        private SpriteRenderer volleyMark;
        public bool CounterArmed => counterArmed;
        public bool IsVolleyCore => volleyCore;
        public void LinkVolley(WaveVolley group, bool core)
        {
            volleyGroup = group; volleyCore = core;
            if (!core) return;
            var mark = new GameObject("Breakable volley core marker");
            mark.transform.SetParent(transform, false);
            volleyMark = mark.AddComponent<SpriteRenderer>();
            volleyMark.sprite = Rushitadel.Presentation.ProjectileArt.Load("TargetMark");
            volleyMark.sortingOrder = 115;
            volleyMark.color = new Color(1, .94f, .63f);
            if (volleyMark.sprite != null)
                mark.transform.localScale = Vector3.one * .72f / (volleyMark.sprite.bounds.size.x * Mathf.Abs(transform.lossyScale.x));
        }
        public void ArmCounterRicochet()
        {
            if (!IsAlive || counterArmed) return;
            counterArmed = true;
            health = Mathf.Max(1, health * .5f);
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>()) sr.color = new Color(1, .68f, .85f);
            // The next wall contact can now reflect even an ordinary zero-bounce projectile.
            maximumBounces = Mathf.Max(maximumBounces, BounceCount + 1);
        }
        public void SetRewardOwner(EnemyController owner) => rewardOwner = owner;
        public bool IsAlive => health > 0f;
        public float MaximumHealth => maximumHealth;
        public float CurrentHealth => health;
        public Vector2 Direction => direction;
        public int BounceCount { get; private set; }
        private float slowUntil, slowAmount, vulnerableUntil, pushedUntil;
        private Object attachedDrill;
        private int maximumBounces = int.MaxValue;
        public int MaximumBounces=>maximumBounces;
        public void ConfigureBoss(float hp, float velocity, float radius, float seconds, int bounces, float hitDamage=12)
        {
            maximumHealth=health=hp; speed=velocity; collisionRadius=radius;
            lifetime=seconds; maximumBounces=bounces; damage=hitDamage;
            hitbox.radius=radius/Mathf.Max(.0001f,Mathf.Abs(transform.lossyScale.x));
        }
        public void SetHeading(Vector2 value) { direction=value.normalized; FaceDirection(direction); }
        public void Despawn() => Finish();
        public float SpeedMultiplier => Time.time<slowUntil ? 1-slowAmount : 1;
        public float DamageMultiplier => Time.time<vulnerableUntil?1.3f:1;
        public void Slow(float amount,float duration)
        {
            if(maximumHealth>=80)amount*=.5f;
            if(Time.time>=slowUntil || amount>=slowAmount)
            {
                slowAmount=amount; slowUntil=Time.time+duration;
                Rushitadel.Presentation.ProjectileArt.Status(gameObject,"IceShell",duration,collisionRadius*2.5f);
            }
        }
        public void Corrode(float duration)
        {
            vulnerableUntil=Time.time+duration;
            Rushitadel.Presentation.ProjectileArt.Status(gameObject,"PoisonStatus",duration,collisionRadius*2.5f);
        }
        public bool AttachDrill(Object drill){if(attachedDrill!=null)return false; attachedDrill=drill; return true;}
        public void Push(Vector2 heading)
        {
            if(Time.time<pushedUntil)return; pushedUntil=Time.time+1;
            var next=(Vector2)transform.position+heading*(maximumHealth>=80?.1f:.35f);
            if(PlayfieldBoundary.Instance!=null && PlayfieldBoundary.Instance.Contains(transform.position))next=PlayfieldBoundary.Instance.ClampPlayer(next,WallExtent(collisionRadius));
            transform.position=next;
        }

        private void Awake()
        {
            transform.localScale *= 1.2f;
            health = maximumHealth;
            hitbox = GetComponent<CircleCollider2D>();
            hitbox.isTrigger = true;
            hitbox.radius = collisionRadius / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        }

        public void Launch(Vector2 heading)
        {
            direction = heading.sqrMagnitude > 0f ? heading.normalized : Vector2.down;
            remainingLifetime = lifetime;
            flying = true;
            PrepareBody();
            FaceDirection(direction);
        }

        private void Update()
        {
            if (!flying || !IsAlive || Time.timeScale <= 0f) return;
            remainingLifetime -= speed>0?CombatTempo.DeltaTime:Time.deltaTime;
            if (remainingLifetime <= 0f) { Finish(); return; }
            Advance(speed * SpeedMultiplier * CombatTempo.DeltaTime);
        }

        // Bounded substeps handle entry + several ricochets in a single fast frame.
        private void Advance(float travel)
        {
            for (int iteration = 0; travel > 0f && flying && iteration < 64; iteration++)
            {
                Vector2 start = transform.position;
                var boundary = PlayfieldBoundary.Instance;
                float wallDistance = travel;
                Vector2 normal = Vector2.zero;
                bool wallHit = boundary != null && boundary.TryGetExitHit(start, direction,
                    WallExtent(collisionRadius), travel, out wallDistance, out normal);
                float segment = wallHit ? wallDistance : travel;
                Physics2D.SyncTransforms();
                hits.Clear();
                Physics2D.CapsuleCast(start, CastSize(collisionRadius), CastDirection, transform.eulerAngles.z, direction,
                    new ContactFilter2D { useTriggers = true }, hits, segment);
                PlayerHealth player = null;
                ProjectileController friendly = null;
                TransformationShot transformed = null;
                float closest = float.PositiveInfinity;
                foreach (RaycastHit2D hit in hits)
                {
                    var candidatePlayer = hit.collider.GetComponentInParent<PlayerHealth>();
                    var candidateShot = hit.collider.GetComponentInParent<ProjectileController>();
                    var candidateForm = hit.collider.GetComponentInParent<TransformationShot>();
                    if ((candidatePlayer == null || !candidatePlayer.IsAlive) &&
                        (candidateShot == null || !candidateShot.IsFlying || !candidateShot.CanHit(this)) &&
                        (candidateForm==null || !candidateForm.CanHit(this))) continue;
                    if (hit.distance >= closest) continue;
                    closest = hit.distance;
                    player = candidatePlayer;
                    friendly = candidateShot;
                    transformed = candidateForm;
                }
                if (player != null || friendly != null || transformed != null)
                {
                    transform.position = start + direction * closest;
                    travel -= closest;
                    if (transformed!=null) transformed.Hit(this);
                    else if (friendly != null) friendly.Hit(this);
                    else { player.TakeDamage(damage); Finish(); }
                    continue;
                }
                transform.position = start + direction * segment;
                travel -= segment;
                if (!wallHit) break;
                if (counterArmed && volleyGroup != null && volleyGroup.ClaimReturn())
                {
                    Vector2 reflected = Vector2.Reflect(direction, normal).normalized;
                    // Rebound locks onto the emitter's current position ONCE, then flies straight.
                    // Pure downward mirroring would miss cliff enemies above the player forever.
                    if (rewardOwner != null && rewardOwner.IsAlive)
                    {
                        Vector2 returnAim = ((Vector2)rewardOwner.transform.position - (Vector2)transform.position).normalized;
                        if (Vector2.Dot(returnAim, normal) > .05f) reflected = returnAim;
                    }
                    var go = new GameObject("Friendly counter ricochet");
                    go.transform.position = transform.position + (Vector3)(normal * .02f);
                    var counter = go.AddComponent<ProjectileController>();
                    counter.Launch(reflected, 65, 22, "PrismRound");
                    counter.SetModifiers(2, false, false);
                    Finish();
                    break;
                }
                if(BounceCount >= maximumBounces) { Finish(); break; }
                direction = Vector2.Reflect(direction, normal).normalized;
                // Separate from the surface to avoid a repeated zero-distance collision.
                transform.position += (Vector3)(normal * 0.0001f);
                BounceCount++;
                FaceDirection(direction);
            }
            // No unchecked movement is performed if an extreme speed exhausts the substep budget.
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || Time.timeScale <= 0f || !float.IsFinite(amount) || amount <= 0f) return;
            health = Mathf.Max(0f, health - amount*DamageMultiplier);
            if (!IsAlive)
            {
                if (volleyCore) volleyGroup?.BreakCore();
                if (rewardOwner != null) rewardOwner.RewardInterception(maximumHealth, transform.position);
                Finish();
            }
        }

        private void Finish()
        {
            health = 0f;
            flying = false;
            hitbox.enabled = false;
            if(visualHitbox!=null)visualHitbox.enabled=false;
            Destroy(gameObject); // Expiry, wall limits and player impact never grant interception XP.
        }
    }
}
