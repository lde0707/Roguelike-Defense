using Rushitadel.Data;
using UnityEngine;

namespace Rushitadel.Combat
{
    public sealed class ProjectileController : ProjectileBehaviour
    {
        [SerializeField] private HomingProjectileDefinition definition;
        public HomingProjectileDefinition Definition => definition;

        private void Awake() => DefinitionGuard.Require(this, definition);


        private float speed => definition.Speed;

        private float hitDistance => definition.HitDistance;

        private float lifetime => definition.Lifetime;

        private EnemyController target;
        private float damage;
        private float remainingLifetime;
        private Vector3 rangeCenter;
        private float attackRange;
        private Vector3 lastTargetPosition;
        private bool isFlyingToLastPosition;

        public void Launch(
            EnemyController newTarget,
            float newDamage,
            Vector3 newRangeCenter,
            float newAttackRange)
        {
            target = newTarget;
            damage = newDamage;
            rangeCenter = newRangeCenter;
            attackRange = newAttackRange;
            lastTargetPosition = newTarget != null ? newTarget.transform.position : transform.position;
            isFlyingToLastPosition = false;
            remainingLifetime = lifetime;

            PrepareBody();
        }

        private void Update()
        {
            remainingLifetime -= Time.deltaTime;
            if (remainingLifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (!IsValidTarget(target))
            {
                target = EnemyRegistry.FindClosest(rangeCenter, attackRange);
                if (target == null)
                {
                    isFlyingToLastPosition = true;
                }
                else
                {
                    isFlyingToLastPosition = false;
                }
            }

            if (!isFlyingToLastPosition)
            {
                lastTargetPosition = target.transform.position;
            }

            Vector3 destination = isFlyingToLastPosition ? lastTargetPosition : target.transform.position;
            Vector3 offset = destination - transform.position;
            float step = speed * Time.deltaTime;
            if (offset.sqrMagnitude <= Mathf.Max(hitDistance * hitDistance, step * step))
            {
                if (!isFlyingToLastPosition && target != null && target.IsAlive)
                {
                    ApplyDamage(target, damage);
                }

                Destroy(gameObject);
                return;
            }

            transform.position += offset.normalized * step;
            FaceDirection(offset);
        }

        private bool IsValidTarget(EnemyController candidate)
        {
            if (candidate == null || !candidate.IsAlive)
            {
                return false;
            }

            float rangeSquared = attackRange * attackRange;
            return (candidate.transform.position - rangeCenter).sqrMagnitude <= rangeSquared;
        }
    }
}
