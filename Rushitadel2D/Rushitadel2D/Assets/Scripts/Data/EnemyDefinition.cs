using UnityEngine;

namespace Rushitadel.Data
{
    [CreateAssetMenu(menuName = "Rushitadel 2D/Combat/EnemyDefinition", fileName = "EnemyDefinition")]
    public sealed class EnemyDefinition : GameplayDefinition
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float moveSpeed = 1f;
        [SerializeField, Min(0f)] private float stoppingDistance = 0.45f;

        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float StoppingDistance => stoppingDistance;
        protected override void ValidateValues()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            stoppingDistance = Mathf.Max(0f, stoppingDistance);
        }
    }
}