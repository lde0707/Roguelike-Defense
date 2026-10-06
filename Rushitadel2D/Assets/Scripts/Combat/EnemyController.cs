using UnityEngine;
using Rushitadel.Progression;
using Rushitadel.Player;

namespace Rushitadel.Combat
{
    public sealed class EnemyController : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float moveSpeed = 1f;
        [SerializeField, Min(0f)] private float stoppingDistance = 1.05f;
        [SerializeField, Min(0f)] private float contactDamage = 10f;
        [SerializeField, Min(0.1f)] private float attackInterval = 1f;
        private float nextAttackTime;
        [SerializeField] private ExperiencePickup experiencePickupPrefab;

        private float currentHealth;
        private int dropValue = 20;
        public void ConfigureRun(float hp, int xp, bool stationary = false)
        {
            maxHealth = currentHealth = hp; dropValue = xp;
            if (stationary) { moveSpeed = 0; contactDamage = 0; }
        }

        public bool IsAlive => currentHealth > 0f;
        public float CurrentHealth => currentHealth;
        public float MaximumHealth => maxHealth;

        private void Awake() => currentHealth = maxHealth;

        private void OnEnable() => EnemyRegistry.Register(this);

        private void OnDisable() => EnemyRegistry.Unregister(this);

        private void Update()
        {
            PlayerHealth target = PlayerHealth.Instance;
            if (Time.timeScale <= 0f || target == null || !target.IsAlive || !IsAlive)
            {
                return;
            }

            Vector3 offset = target.transform.position - transform.position;
            offset.z = 0f;
            if (offset.sqrMagnitude <= stoppingDistance * stoppingDistance)
            {
                if (Time.time >= nextAttackTime)
                {
                    target.TakeDamage(contactDamage);
                    nextAttackTime = Time.time + attackInterval;
                }
                return;
            }

            transform.position += offset.normalized * Mathf.Min(moveSpeed * Time.deltaTime, offset.magnitude - stoppingDistance);
            transform.up = offset.normalized;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || !float.IsFinite(amount) || amount <= 0f)
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
                var pickup = Instantiate(experiencePickupPrefab, transform.position, Quaternion.identity);
                pickup.gameObject.SetActive(true); pickup.SetValue(dropValue);
            }
            else new GameObject("Blue XP", typeof(CircleCollider2D), typeof(SpriteRenderer), typeof(ExperiencePickup))
                { transform = { position = transform.position } }.GetComponent<ExperiencePickup>().SetValue(dropValue);
        }


    }
}
