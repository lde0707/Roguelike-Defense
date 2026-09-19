using Rushitadel.Data;
using UnityEngine;
using Rushitadel.Progression;

namespace Rushitadel.Combat
{
    public sealed class EnemyController : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyDefinition definition;
        public EnemyDefinition Definition => definition;


        private float maxHealth => definition.MaxHealth;

        private float moveSpeed => definition.MoveSpeed;

        private float stoppingDistance => definition.StoppingDistance;
        [SerializeField] private ExperiencePickup experiencePickupPrefab;

        private float currentHealth;

        public bool IsAlive => currentHealth > 0f;
        public float CurrentHealth => currentHealth;

        private void Awake()
        {
            if (!DefinitionGuard.Require(this, definition)) return;
            currentHealth = maxHealth;
        }

        private void OnEnable() => EnemyRegistry.Register(this);

        private void OnDisable() => EnemyRegistry.Unregister(this);

        private void Update()
        {
            TowerController target = FindClosestTower();
            if (target == null)
            {
                return;
            }

            Vector3 offset = target.transform.position - transform.position;
            offset.z = 0f;
            if (offset.sqrMagnitude <= stoppingDistance * stoppingDistance)
            {
                return;
            }

            transform.position += offset.normalized * (moveSpeed * Time.deltaTime);
            transform.up = offset.normalized;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            if (!IsAlive)
            {
                Die();
            }
        }

        public void Eliminate()
        {
            if (!IsAlive)
            {
                return;
            }

            currentHealth = 0f;
            Die();
        }

        private void Die()
        {
            SpawnExperience();
            Destroy(gameObject);
        }

        private void SpawnExperience()
        {
            if (experiencePickupPrefab != null)
            {
                Instantiate(experiencePickupPrefab, transform.position, Quaternion.identity);
            }
        }

        private TowerController FindClosestTower()
        {
            TowerController closest = null;
            float closestDistance = float.PositiveInfinity;

            foreach (TowerController tower in TowerRegistry.ActiveTowers)
            {
                if (tower == null || !tower.isActiveAndEnabled)
                {
                    continue;
                }

                float distance = (tower.transform.position - transform.position).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = tower;
                }
            }

            return closest;
        }
    }
}
