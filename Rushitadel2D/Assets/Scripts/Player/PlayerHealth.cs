using System;
using Rushitadel.Combat;
using UnityEngine;

namespace Rushitadel.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField, Min(0f)] private float invulnerabilitySeconds = 0.5f;
        private float currentHealth;
        private float invulnerableUntil;
        public static PlayerHealth Instance { get; private set; }
        public float CurrentHealth => currentHealth;
        public float MaximumHealth => maximumHealth;
        public float NormalizedHealth => currentHealth / maximumHealth;
        public bool IsAlive => currentHealth > 0f;
        public event Action<float, float> HealthChanged;
        public event Action Died;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() => Instance = null;

        private void Awake()
        {
            Instance = this;
            currentHealth = maximumHealth;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || Time.timeScale <= 0f || !float.IsFinite(amount) ||
                amount <= 0f || Time.time < invulnerableUntil) return;
            invulnerableUntil = Time.time + invulnerabilitySeconds;
            ApplyDamage(amount);
        }

        public void TakeEnvironmentalDamage(float amount)
        {
            if (!IsAlive || Time.timeScale <= 0f || !float.IsFinite(amount) || amount <= 0f) return;
            ApplyDamage(amount); // Periodic hazards neither consume nor grant normal hit invulnerability.
        }

        private void ApplyDamage(float amount)
        {
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            HealthChanged?.Invoke(currentHealth, maximumHealth);
            if (IsAlive) return;
            if (TryGetComponent(out Rigidbody2D body)) body.linearVelocity = Vector2.zero;
            Time.timeScale = 0f;
            Died?.Invoke();
        }

        public void RestoreHealth(float value)
        {
            currentHealth = Mathf.Clamp(value, 1f, maximumHealth);
            invulnerableUntil = 0f;
            HealthChanged?.Invoke(currentHealth, maximumHealth);
        }
        public void SetMaximumHealth(float value)
        {
            maximumHealth = Mathf.Max(1, value); currentHealth = Mathf.Min(currentHealth, maximumHealth);
            HealthChanged?.Invoke(currentHealth, maximumHealth);
        }

        public float Heal(float amount)
        {
            if (!IsAlive || !float.IsFinite(amount) || amount <= 0f) return 0f;
            float restored = Mathf.Min(amount, maximumHealth - currentHealth);
            currentHealth += restored;
            if (restored > 0f) HealthChanged?.Invoke(currentHealth, maximumHealth);
            return restored;
        }
    }
}
