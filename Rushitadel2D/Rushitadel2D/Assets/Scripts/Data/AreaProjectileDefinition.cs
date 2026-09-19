using UnityEngine;

namespace Rushitadel.Data
{
    [CreateAssetMenu(menuName = "Rushitadel 2D/Combat/AreaProjectileDefinition", fileName = "AreaProjectileDefinition")]
    public sealed class AreaProjectileDefinition : GameplayDefinition
    {
        [SerializeField, Min(0f)] private float speed = 12f;
        [SerializeField, Min(0.1f)] private float lifetime = 3f;

        public float Speed => speed;
        public float Lifetime => lifetime;
        protected override void ValidateValues()
        {
            speed = Mathf.Max(0f, speed);
            lifetime = Mathf.Max(0.1f, lifetime);
        }
    }
}