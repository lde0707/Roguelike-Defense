using UnityEngine;
using Rushitadel.Progression;

namespace Rushitadel.Combat
{
    public sealed class EnemyController : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float moveSpeed = 100f;
        [SerializeField, Min(0f)] private float stoppingDistance = 45f;
        [SerializeField] private ExperiencePickup experiencePickupPrefab;

        private float currentHealth;

        public bool IsAlive => currentHealth > 0f;
        public float CurrentHealth => currentHealth;

        private void Awake() => currentHealth = maxHealth;

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
            offset.y = 0f;
            if (offset.sqrMagnitude <= stoppingDistance * stoppingDistance)
            {
                return;
            }

            transform.position += offset.normalized * (moveSpeed * Time.deltaTime);
            transform.forward = offset.normalized;
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
                SpawnExperience();
                Destroy(gameObject);
            }
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
