using UnityEngine;

namespace Rushitadel.Data
{
    [CreateAssetMenu(menuName = "Rushitadel 2D/Player/PlayerDefinition", fileName = "PlayerDefinition")]
    public sealed class PlayerDefinition : GameplayDefinition
    {
        [SerializeField, Min(0f)] private float moveSpeed = 2.5f;
        [SerializeField, Min(0.01f)] private float shotsPerSecond = 4f;
        [SerializeField, Min(0f)] private float bulletDamage = 25f;

        public float MoveSpeed => moveSpeed;
        public float ShotsPerSecond => shotsPerSecond;
        public float BulletDamage => bulletDamage;
        protected override void ValidateValues()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            shotsPerSecond = Mathf.Max(0.01f, shotsPerSecond);
            bulletDamage = Mathf.Max(0f, bulletDamage);
        }
    }
}