using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Rushitadel.Combat
{
    /// <summary>Non-homing shell. Hits the first enemy or destructible hostile projectile.</summary>
    public sealed class ProjectileController : ProjectileBehaviour
    {
        [SerializeField, Min(0f)] private float speed = 10f;
        [FormerlySerializedAs("hitDistance")]
        [SerializeField, Min(0f)] private float collisionRadius = 0.2f;
        [SerializeField, Min(0.1f)] private float lifetime = 5f;
        private readonly List<RaycastHit2D> hits = new();
        private Vector3 direction;
        private float damage;
        private float remainingDistance;
        private float remainingLifetime;
        private bool flying;
        private CircleCollider2D hitbox;
        private readonly HashSet<IDamageable> damaged = new();
        private int extraHits;
        private bool splash, poison;
        public bool CanHit(IDamageable target) => flying && !damaged.Contains(target);
        public void SetModifiers(int pierce, bool explosion, bool toxin)
        {
            extraHits = pierce; splash = explosion; poison = toxin;
            string special=toxin?"WebPoison":explosion?"WebBlast":pierce>0?"WebPierce":null;
            if(special!=null) Rushitadel.Presentation.ProjectileArt.Set(gameObject,special,new Vector2(.12f,.24f),Color.white);
        }
        public bool IsFlying => flying;
        public Vector3 Direction => direction;
        public float DistanceTravelled { get; private set; }

        private void Awake() => transform.localScale *= 1.2f;

        public void Launch(Vector3 newDirection, float newDamage, float maximumDistance, string visual="TowerBasic")
        {
            newDirection.z = 0f;
            direction = newDirection.sqrMagnitude > 0f ? newDirection.normalized : Vector3.up;
            damage = Mathf.Max(0f, newDamage);
            remainingDistance = Mathf.Max(0f, maximumDistance);
            remainingLifetime = lifetime;
            DistanceTravelled = 0f;
            flying = true;
            hitbox = GetComponent<CircleCollider2D>();
            if (hitbox == null) hitbox = gameObject.AddComponent<CircleCollider2D>();
            hitbox.isTrigger = true;
            hitbox.radius = collisionRadius / Mathf.Max(0.0001f,
                Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y)));
            PrepareBody();
            FaceDirection(direction);
            Rushitadel.Presentation.ProjectileArt.Set(gameObject,visual,new Vector2(.12f,.24f),Color.white);
        }

        private void Update()
        {
            if (!flying || Time.timeScale <= 0f) return;
            remainingLifetime -= Time.deltaTime;
            if (remainingLifetime <= 0f || remainingDistance <= 0f)
            {
                Finish();
                return;
            }

            float step = Mathf.Min(speed * Time.deltaTime, remainingDistance);
            Vector3 start = transform.position;
            // Enemy movement is transform-driven. Synchronize before the swept collision query.
            Physics2D.SyncTransforms();
            hits.Clear();
            Physics2D.CircleCast(start, collisionRadius, direction,
                new ContactFilter2D { useTriggers = true }, hits, step);
            IDamageable first = null;
            float closestDistance = float.PositiveInfinity;
            foreach (RaycastHit2D hit in hits)
            {
                var enemy = hit.collider.GetComponentInParent<EnemyController>();
                var bullet = hit.collider.GetComponentInParent<EnemyProjectileController>();
                IDamageable candidate = enemy != null && enemy.isActiveAndEnabled ? enemy :
                    bullet != null && bullet.isActiveAndEnabled ? bullet : null;
                if (candidate == null || !candidate.IsAlive || !CanHit(candidate) || hit.distance >= closestDistance) continue;
                first = candidate;
                closestDistance = hit.distance;
            }

            float travelled = first != null ? closestDistance : step;
            transform.position = start + direction * travelled;
            DistanceTravelled += travelled;
            remainingDistance = Mathf.Max(0f, remainingDistance - travelled);
            if (first != null)
            {
                Hit(first);
            }
            else if (remainingDistance <= 0f) Finish();
        }

        public void Hit(IDamageable target)
        {
            if (!flying || target == null || !target.IsAlive || !CanHit(target)) return;
            damaged.Add(target);
            ApplyDamage(target, damage);
            if (target is EnemyController enemy && enemy != null && enemy.IsAlive && poison)
            {
                var effect = enemy.GetComponent<Rushitadel.Progression.PoisonEffect>();
                if (effect == null) effect = enemy.gameObject.AddComponent<Rushitadel.Progression.PoisonEffect>();
                effect.Refresh();
            }
            if (splash)
            {
                Rushitadel.Presentation.ProjectileArt.Burst(transform.position,"BlastRing",Vector2.one*1.6f,new Color(1,.83f,.48f),.18f);
                foreach (var other in EnemyRegistry.FindAllWithinRadius(transform.position, .8f))
                    if (other != null && other.IsAlive && !ReferenceEquals(other, target) && Vector2.Distance(other.transform.position, transform.position) <= .8f)
                        other.TakeDamage(damage * .5f);
            }
            if (extraHits-- <= 0) Finish();
        }

        private void Finish()
        {
            flying = false;
            if (hitbox != null) hitbox.enabled = false;
            Destroy(gameObject);
        }
    }
}
