using Rushitadel.Data;
using System;
using UnityEngine;

namespace Rushitadel.Power
{
    [DisallowMultipleComponent]
    public sealed class PlayerPower : MonoBehaviour, IPowerResource, IEnemySpawnModifier
    {
        [SerializeField] private PowerCycleDefinition definition;
        public PowerCycleDefinition Definition => definition;


        private float maximumPower => definition.MaximumPower;

        private float recoveryTarget => definition.RecoveryTarget;

        private float depletionPerSecond => definition.DepletionPerSecond;

        private float recoveryPerSecond => definition.RecoveryPerSecond;

        private float recoveringEnemyMultiplier => definition.RecoveringEnemyMultiplier;

        [Header("Runtime State")]
        private float currentPower;
        private PowerCycleState state;

        public event Action<float, float> PowerChanged;
        public event Action<PowerCycleState> StateChanged;

        public float CurrentPower => currentPower;
        public float MaximumPower => maximumPower;
        public float NormalizedPower => maximumPower > 0f ? currentPower / maximumPower : 0f;
        public PowerCycleState State => state;
        public float EnemyCountMultiplier =>
            state == PowerCycleState.Recovering ? recoveringEnemyMultiplier : 1f;

        private void Awake()
        {
            if (!DefinitionGuard.Require(this, definition)) return;
            currentPower = maximumPower;
            state = PowerCycleState.Depleting;
        }

        private void Update()
        {
            if (state == PowerCycleState.Depleting)
            {
                UpdateDepletion();
                return;
            }

            UpdateRecovery();
        }

public bool TrySpend(float amount)
        {
            if (amount <= 0f || currentPower + Mathf.Epsilon < amount)
            {
                return false;
            }

            SetPower(currentPower - amount);
            if (currentPower <= 0f && state == PowerCycleState.Depleting)
            {
                ChangeState(PowerCycleState.Recovering);
            }

            return true;
        }

        private void UpdateDepletion()
        {
            SetPower(Mathf.MoveTowards(currentPower, 0f, depletionPerSecond * Time.deltaTime));
            if (currentPower <= 0f)
            {
                ChangeState(PowerCycleState.Recovering);
            }
        }

        private void UpdateRecovery()
        {
            float target = Mathf.Min(recoveryTarget, maximumPower);
            SetPower(Mathf.MoveTowards(currentPower, target, recoveryPerSecond * Time.deltaTime));
            if (currentPower >= target)
            {
                ChangeState(PowerCycleState.Depleting);
            }
        }

        private void SetPower(float value)
        {
            float clampedValue = Mathf.Clamp(value, 0f, maximumPower);
            if (Mathf.Approximately(currentPower, clampedValue))
            {
                return;
            }

            currentPower = clampedValue;
            PowerChanged?.Invoke(currentPower, maximumPower);
        }

        private void ChangeState(PowerCycleState newState)
        {
            if (state == newState)
            {
                return;
            }

            state = newState;
            StateChanged?.Invoke(state);
        }
    }
}
