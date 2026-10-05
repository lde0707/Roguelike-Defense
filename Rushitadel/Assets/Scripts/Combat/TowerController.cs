using UnityEngine;

namespace Rushitadel.Combat
{
    public sealed class TowerController : MonoBehaviour
    {
        [Header("Attack Settings")]
        [SerializeField, Min(0f)] private float attackRange = 300f;
        [SerializeField, Min(0.01f)] private float shotInterval = 0.5f;
        [SerializeField, Min(0f)] private float damage = 25f;

        [Header("Projectile References")]
        [SerializeField] private Transform shotPos;
        [SerializeField] private GameObject projectilePrefab;
        private float nextShotTime;

        public void Configure(Transform newShotPos, GameObject newProjectilePrefab)
        {
            shotPos = newShotPos != null ? newShotPos : transform;
            projectilePrefab = newProjectilePrefab;
        }

        private void OnEnable() => TowerRegistry.Register(this);

        private void OnDisable() => TowerRegistry.Unregister(this);

        private void Update()
        {
            if (Time.time < nextShotTime || projectilePrefab == null)
            {
                return;
            }

            EnemyController target = FindClosestEnemyInRange();
            if (target == null)
            {
                return;
            }

            Fire(target);
            nextShotTime = Time.time + shotInterval;
        }

        private EnemyController FindClosestEnemyInRange()
        {
            return EnemyRegistry.FindClosest(transform.position, attackRange);
        }

        private void Fire(EnemyController target)
        {
            Transform origin = shotPos != null ? shotPos : transform;
            GameObject projectileObject = Instantiate(projectilePrefab, origin.position, Quaternion.identity);
            projectileObject.SetActive(true);

            ProjectileController projectile = projectileObject.GetComponent<ProjectileController>();
            if (projectile == null)
            {
                projectile = projectileObject.AddComponent<ProjectileController>();
            }

            projectile.Launch(target, damage, transform.position, attackRange);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.25f, 0.1f, 0.75f);
            DrawRangeCircle(transform.position, attackRange, 64);
        }

        private static void DrawRangeCircle(Vector3 center, float radius, int segments)
        {
            Vector3 previous = center + Vector3.forward * radius;
            for (int index = 1; index <= segments; index++)
            {
                float angle = index * Mathf.PI * 2f / segments;
                Vector3 next = center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
