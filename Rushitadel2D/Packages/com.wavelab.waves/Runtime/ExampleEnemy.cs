using UnityEngine;
using WaveLab.Core;
namespace WaveLab.Waves
{
    // Neutral test unit: no game-specific targeting, projectiles or automatic defeat.
    public sealed class ExampleEnemy : MonoBehaviour, IStatProvider
    {
        public float maxHealth = 40;
        public float attackDamage = 3;
        public float attackInterval = 2;
        public float moveSpeed = 1;
        public float attackRange = 3;
        public bool moveOnSpawn;
        public Vector3 moveDirection = Vector3.down;
        public float CurrentHealth { get; private set; }
        private bool defeated;
        private void Awake() { CurrentHealth = maxHealth; }
        private void Update()
        {
            if (moveOnSpawn && !defeated && GetComponent<ExampleCombat>() == null) transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
        }
        public void TakeDamage(float amount)
        {
            if (defeated || !WaveValidation.Finite(amount) || amount <= 0) return;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            if (CurrentHealth > 0) return;
            defeated = true;
            GetComponent<SpawnedUnit>()?.ReportDefeated();
            Destroy(gameObject);
        }
        [ContextMenu("테스트/피해 10 주기")]
        private void TestDamage() { if (Application.isPlaying) TakeDamage(10); }
        [ContextMenu("테스트/즉시 처치")]
        private void TestDefeat() { if (Application.isPlaying) TakeDamage(maxHealth); }
        public bool TryGetStat(string id, out float value)
        {
            switch(id)
            {
                case "health.max": value=maxHealth;return true;
                case "attack.damage": value=attackDamage;return true;
                case "attack.interval": value=attackInterval;return true;
                case "movement.speed": value=moveSpeed;return true;
                case "attack.range": value=attackRange;return true;
                default: value=0;return false;
            }
        }
    }
}
