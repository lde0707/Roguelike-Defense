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
        private int interceptionXp;
        private float interceptionCredit;
        public int InterceptionXpPaid => interceptionXp;
        public int InterceptionXpLimit => Mathf.FloorToInt(dropValue * .4f);
        // One finite budget per enemy; destroying its bullets never creates extra XP.
        public void RewardInterception(float bulletHealth, Vector3 position)
        {
            if (!IsAlive || Time.timeScale <= 0 || interceptionXp >= InterceptionXpLimit) return;
            interceptionCredit += Mathf.Clamp(bulletHealth / 24f, .5f, 3f);
            int grant = Mathf.Min(Mathf.FloorToInt(interceptionCredit), InterceptionXpLimit - interceptionXp);
            if (grant < 2 && grant < InterceptionXpLimit - interceptionXp) return;
            if (grant <= 0) return;
            interceptionCredit -= grant;
            interceptionXp += grant;
            SpawnExperience(grant, position);
        }
        public void ConfigureRun(float hp, int xp, bool stationary = false)
        {
            maxHealth = currentHealth = hp; dropValue = Mathf.Max(0, xp);
            interceptionXp = 0; interceptionCredit = 0;
            if (stationary) { moveSpeed = 0; contactDamage = 0; }
        }

        public bool IsAlive => currentHealth > 0f;
        public float CurrentHealth => currentHealth;
        public float MaximumHealth => maxHealth;
        public event System.Action<float> Damaged;

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
            if(moveSpeed > 0) transform.up = offset.normalized;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || !float.IsFinite(amount) || amount <= 0f)
            {
                return;
            }

            float applied=Mathf.Min(currentHealth,amount);
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            Damaged?.Invoke(applied);
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

            TakeDamage(currentHealth);
        }

        private void Die()
        {
            if(TryGetComponent<WaveLab.Waves.SpawnedUnit>(out var waveUnit))waveUnit.ReportDefeated();
            SpawnExperience(dropValue - interceptionXp, transform.position);
            Destroy(gameObject);
        }

        private void SpawnExperience(int value, Vector3 position)
        {
            if (value <= 0) return;
            if (experiencePickupPrefab != null)
            {
                var pickup = Instantiate(experiencePickupPrefab, position, Quaternion.identity);
                pickup.gameObject.SetActive(true); pickup.SetValue(value);
            }
            else new GameObject("Blue XP", typeof(CircleCollider2D), typeof(SpriteRenderer), typeof(ExperiencePickup))
                { transform = { position = position } }.GetComponent<ExperiencePickup>().SetValue(value);
        }


    }
}
