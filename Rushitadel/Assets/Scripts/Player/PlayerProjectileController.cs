using Rushitadel.Combat;
using System.Collections.Generic;
using UnityEngine;

namespace Rushitadel.Player
{
    public sealed class PlayerProjectileController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float speed = 1200f;
        [SerializeField] private MouseWorldInteractionSettings interactionSettings;
        [SerializeField, Min(0.1f)] private float lifetime = 3f;

        private Vector3 direction;
        private Vector3 destination;
        private float damage;
        private float impactRadius = -1f;
        private bool eliminateOnImpact;
        private float remainingLifetime;

        public void Launch(Vector3 newDirection, Vector3 newDestination, float newDamage)
        {
            ConfigureFlight(newDirection, newDestination, newDamage, -1f, false);
        }

        public void LaunchEliminating(
            Vector3 newDirection,
            Vector3 newDestination,
            float newImpactRadius)
        {
            ConfigureFlight(newDirection, newDestination, 0f, newImpactRadius, true);
        }

        private void ConfigureFlight(
            Vector3 newDirection,
            Vector3 newDestination,
            float newDamage,
            float newImpactRadius,
            bool shouldEliminateOnImpact)
        {
            direction = newDirection.sqrMagnitude > 0f ? newDirection.normalized : Vector3.forward;
            destination = newDestination;
            damage = newDamage;
            impactRadius = newImpactRadius;
            eliminateOnImpact = shouldEliminateOnImpact;
            remainingLifetime = lifetime;

            Rigidbody projectileBody = GetComponent<Rigidbody>();
            if (projectileBody != null)
            {
                projectileBody.useGravity = false;
                projectileBody.isKinematic = true;
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

            Vector3 start = transform.position;
            float remainingDistance = Vector3.Distance(start, destination);
            float step = Mathf.Min(speed * Time.deltaTime, remainingDistance);
            Vector3 end = start + direction * step;

            transform.position = end;
            if (remainingDistance <= step + Mathf.Epsilon)
            {
                ApplyAreaDamage();
                Destroy(gameObject);
            }
        }

        private void ApplyAreaDamage()
        {
            float radius = impactRadius >= 0f
                ? impactRadius
                : interactionSettings != null ? interactionSettings.Radius : 0f;
            List<EnemyController> enemies = EnemyRegistry.FindAllWithinRadius(destination, radius);
            foreach (EnemyController enemy in enemies)
            {
                if (eliminateOnImpact)
                {
                    enemy.Eliminate();
                }
                else
                {
                    enemy.TakeDamage(damage);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.75f);
            float radius = impactRadius >= 0f
                ? impactRadius
                : interactionSettings != null ? interactionSettings.Radius : 0f;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
