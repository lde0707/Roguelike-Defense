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
        public bool IsAlive => health > 0f;
        public float MaximumHealth => maximumHealth;
        public float CurrentHealth => health;
        public Vector2 Direction => direction;
        public int BounceCount { get; private set; }
        private float slowUntil, slowAmount, vulnerableUntil, pushedUntil;
        private Object attachedDrill;
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
            if(PlayfieldBoundary.Instance!=null && PlayfieldBoundary.Instance.Contains(transform.position))next=PlayfieldBoundary.Instance.ClampPlayer(next,collisionRadius);
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
            remainingLifetime -= Time.deltaTime;
            if (remainingLifetime <= 0f) { Finish(); return; }
            Advance(speed * SpeedMultiplier * Time.deltaTime);
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
                    collisionRadius, travel, out wallDistance, out normal);
                float segment = wallHit ? wallDistance : travel;
                Physics2D.SyncTransforms();
                hits.Clear();
                Physics2D.CircleCast(start, collisionRadius, direction,
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
            if (!IsAlive) Finish();
        }

        private void Finish()
        {
            health = 0f;
            flying = false;
            hitbox.enabled = false;
            Destroy(gameObject); // Projectiles never drop enemy XP.
        }
    }
}
