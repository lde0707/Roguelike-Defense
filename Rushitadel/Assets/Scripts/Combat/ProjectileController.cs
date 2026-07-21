using UnityEngine;

namespace Rushitadel.Combat
{
    public sealed class ProjectileController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float speed = 1000f;
        [SerializeField, Min(0f)] private float hitDistance = 20f;
        [SerializeField, Min(0.1f)] private float lifetime = 5f;

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

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.useGravity = false;
                body.isKinematic = true;
            }
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
                    target.TakeDamage(damage);
                }

                Destroy(gameObject);
                return;
            }

            transform.position += offset.normalized * step;
            transform.forward = offset.normalized;
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
