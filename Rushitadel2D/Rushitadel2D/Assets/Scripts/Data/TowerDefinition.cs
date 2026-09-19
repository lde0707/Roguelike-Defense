using UnityEngine;

namespace Rushitadel.Data
{
    [CreateAssetMenu(menuName = "Rushitadel 2D/Combat/TowerDefinition", fileName = "TowerDefinition")]
    public sealed class TowerDefinition : GameplayDefinition
    {
        [SerializeField, Min(0f)] private float attackRange = 3f;
        [SerializeField, Min(0.01f)] private float shotInterval = 0.5f;
        [SerializeField, Min(0f)] private float damage = 25f;

        public float AttackRange => attackRange;
        public float ShotInterval => shotInterval;
        public float Damage => damage;
        protected override void ValidateValues()
        {
            attackRange = Mathf.Max(0f, attackRange);
            shotInterval = Mathf.Max(0.01f, shotInterval);
            damage = Mathf.Max(0f, damage);
        }
    }
}