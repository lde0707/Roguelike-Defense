using UnityEngine;

namespace Rushitadel.Data
{
    [CreateAssetMenu(menuName = "Rushitadel 2D/Player/PowerShotDefinition", fileName = "PowerShotDefinition")]
    public sealed class PowerShotDefinition : GameplayDefinition
    {
        [SerializeField, Min(0f)] private float powerCost = 20f;
        [SerializeField, Min(0f)] private float explosionRadius = 2.5f;
        [SerializeField, Min(0.01f)] private float projectileScaleMultiplier = 2f;

        public float PowerCost => powerCost;
        public float ExplosionRadius => explosionRadius;
        public float ProjectileScaleMultiplier => projectileScaleMultiplier;
        protected override void ValidateValues()
        {
            powerCost = Mathf.Max(0.01f, powerCost);
            explosionRadius = Mathf.Max(0f, explosionRadius);
            projectileScaleMultiplier = Mathf.Max(0.01f, projectileScaleMultiplier);
        }
    }
}