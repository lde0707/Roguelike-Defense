using UnityEngine;

namespace Rushitadel.Data
{
    [CreateAssetMenu(menuName = "Rushitadel 2D/Power/PowerCycleDefinition", fileName = "PowerCycleDefinition")]
    public sealed class PowerCycleDefinition : GameplayDefinition
    {
        [SerializeField, Min(1f)] private float maximumPower = 100f;
        [SerializeField, Min(1f)] private float recoveryTarget = 60f;
        [SerializeField, Min(0f)] private float depletionPerSecond = 1f;
        [SerializeField, Min(0f)] private float recoveryPerSecond = 1f;
        [SerializeField, Min(1f)] private float recoveringEnemyMultiplier = 1.5f;

        public float MaximumPower => maximumPower;
        public float RecoveryTarget => recoveryTarget;
        public float DepletionPerSecond => depletionPerSecond;
        public float RecoveryPerSecond => recoveryPerSecond;
        public float RecoveringEnemyMultiplier => recoveringEnemyMultiplier;
        protected override void ValidateValues()
        {
            maximumPower = Mathf.Max(1f, maximumPower);
            recoveryTarget = Mathf.Clamp(recoveryTarget, 1f, maximumPower);
            depletionPerSecond = Mathf.Max(0f, depletionPerSecond);
            recoveryPerSecond = Mathf.Max(0f, recoveryPerSecond);
            recoveringEnemyMultiplier = Mathf.Max(1f, recoveringEnemyMultiplier);
        }
    }
}