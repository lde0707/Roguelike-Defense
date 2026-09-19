using Rushitadel.Data;
using UnityEngine;

namespace Rushitadel.Combat
{
    public sealed class TowerController : MonoBehaviour
    {
        [SerializeField] private TowerDefinition definition;
        public TowerDefinition Definition => definition;

        private void Awake() => DefinitionGuard.Require(this, definition);


        private float attackRange => definition.AttackRange;

        private float shotInterval => definition.ShotInterval;

        private float damage => definition.Damage;

        [Header("Projectile References")]
        [SerializeField] private Transform shotPos;
        [SerializeField] private ProjectileController projectilePrefab;
        private float nextShotTime;

        public void Configure(Transform newShotPos, ProjectileController newProjectilePrefab)
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
            ProjectileController projectile = Instantiate(projectilePrefab, origin.position, Quaternion.identity);
            projectile.gameObject.SetActive(true);
            projectile.Launch(target, damage, transform.position, attackRange);
        }

        private void OnDrawGizmos()
        {
            if (definition == null) return;
            Gizmos.color = new Color(1f, 0.25f, 0.1f, 0.75f);
            DrawRangeCircle(transform.position, attackRange, 64);
        }

        private static void DrawRangeCircle(Vector3 center, float radius, int segments)
        {
            Vector3 previous = center + Vector3.up * radius;
            for (int index = 1; index <= segments; index++)
            {
                float angle = index * Mathf.PI * 2f / segments;
                Vector3 next = center + new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f) * radius;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
