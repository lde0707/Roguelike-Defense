using Rushitadel.Combat;
using UnityEngine;

namespace Rushitadel.Player
{
    public sealed class PlayerProjectileController : ProjectileBehaviour
    {
        [SerializeField, Min(0f)] private float speed = 12f;
        [SerializeField, Min(0.1f)] private float lifetime = 3f;

        private Vector3 direction;
        private Vector3 destination;
        private float impactRadius;
        private float remainingLifetime;
        private bool flying;
        public float ImpactRadius => impactRadius;
        public const float ExecuteRadiusFraction = 0.3f;

        public void LaunchArea(Vector3 newDirection, Vector3 newDestination, float radius)
        {
            newDirection.z = 0f;
            direction = newDirection.sqrMagnitude > 0f ? newDirection.normalized : Vector3.up;
            destination = newDestination;
            destination.z = transform.position.z;
            impactRadius = Mathf.Max(0.01f, radius);
            // Allow any visible mouse destination to be reached before timeout.
            remainingLifetime = Mathf.Max(lifetime, Vector3.Distance(transform.position, destination) / Mathf.Max(0.01f, speed) + 0.5f);
            flying = true;
            transform.localScale = Vector3.one * (impactRadius * 2f * 1.2f);
            PrepareBody();
            Rushitadel.Presentation.ProjectileArt.Set(gameObject,"PlayerPower",Vector2.one*(impactRadius*2),Color.white,3);
        }

        private void Update()
        {
            if (!flying || Time.timeScale <= 0f) return;
            remainingLifetime -= Time.deltaTime;
            if (remainingLifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 start = transform.position;
            float remainingDistance = Vector3.Distance(start, destination);
            float step = Mathf.Min(speed * Time.deltaTime, remainingDistance);
            Vector3 end = start + direction * step;

            transform.position = end;
            if (remainingDistance <= step + Mathf.Epsilon)
            {
                ApplyAreaDamage();
                Rushitadel.Presentation.ProjectileArt.Burst(destination,"BlastRing",Vector2.one*(impactRadius*2),new Color(1,.83f,.48f),.18f);
                flying = false;
                transform.position = destination;
                transform.localScale = Vector3.one * (impactRadius * 2f);
                // Briefly hold the two visible circles at impact so the actual radius is readable.
                Destroy(gameObject, 0.12f);
            }
        }

        private void ApplyAreaDamage()
        {
            // Cursor screen/world conversion can shift an exact rim point by a few floating-point ULPs.
            foreach (EnemyController enemy in EnemyRegistry.FindAllWithinRadius(destination, impactRadius * 1.00001f))
            {
                DamageTarget(enemy, enemy.transform.position);
            }
            foreach (var bullet in FindObjectsByType<EnemyProjectileController>(FindObjectsSortMode.None))
                DamageTarget(bullet, bullet.transform.position);
        }

        private void DamageTarget(IDamageable target, Vector3 position)
        {
            float normalizedDistance = Vector2.Distance(position, destination) / impactRadius;
            if (!target.IsAlive || normalizedDistance > 1f + 0.00001f) return;
            if (normalizedDistance <= ExecuteRadiusFraction + 0.00001f && target is EnemyController enemy)
            {
                enemy.Eliminate();
                return;
            }
            ApplyDamage(target, target.MaximumHealth * DamageFraction(normalizedDistance));
        }

        public static float DamageFraction(float normalizedDistance)
        {
            if (normalizedDistance <= ExecuteRadiusFraction + 0.00001f) return 1f;
            return Mathf.Lerp(1f, 0.5f, Mathf.InverseLerp(ExecuteRadiusFraction, 1f, normalizedDistance));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.75f);
            Gizmos.DrawWireSphere(transform.position, impactRadius);
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, impactRadius * ExecuteRadiusFraction);
        }
    }
}
