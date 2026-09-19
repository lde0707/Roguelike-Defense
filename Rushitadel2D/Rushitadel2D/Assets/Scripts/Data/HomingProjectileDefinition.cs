using UnityEngine;

namespace Rushitadel.Data
{
    [CreateAssetMenu(menuName = "Rushitadel 2D/Combat/HomingProjectileDefinition", fileName = "HomingProjectileDefinition")]
    public sealed class HomingProjectileDefinition : GameplayDefinition
    {
        [SerializeField, Min(0f)] private float speed = 10f;
        [SerializeField, Min(0f)] private float hitDistance = 0.2f;
        [SerializeField, Min(0.1f)] private float lifetime = 5f;

        public float Speed => speed;
        public float HitDistance => hitDistance;
        public float Lifetime => lifetime;
        protected override void ValidateValues()
        {
            speed = Mathf.Max(0f, speed);
            lifetime = Mathf.Max(0.1f, lifetime);
            hitDistance = Mathf.Max(0f, hitDistance);
        }
    }
}