using System;
using Rushitadel.Player;
using UnityEngine;

namespace Rushitadel.Power
{
    [DisallowMultipleComponent]
    public sealed class PlayerPower : MonoBehaviour, IPowerResource
    {
        [SerializeField, Min(1f)] private float maximumPower = 100f;
        [SerializeField, Range(0.01f, 1f)] private float recoveryThreshold = 0.6f;
        [SerializeField, Min(0f)] private float recoveryPerSecond = 5f;
        private float currentPower;
        private PowerCycleState state;
        private PlayerHealth health;
        public event Action<float, float> PowerChanged;
        public event Action<PowerCycleState> StateChanged;
        public float CurrentPower => currentPower;
        public float MaximumPower => maximumPower;
        public float NormalizedPower => currentPower / maximumPower;
        public PowerCycleState State => state;

        private void Awake()
        {
            currentPower = maximumPower;
            state = PowerCycleState.Ready;
            health = GetComponent<PlayerHealth>();
        }

        private void Update()
        {
            if (Time.timeScale <= 0f || (health != null && !health.IsAlive)) return;
            SetPower(Mathf.MoveTowards(currentPower, maximumPower, recoveryPerSecond * (Rushitadel.Progression.CampaignDirector.HasWeb(4) ? 1.5f : 1f) * Rushitadel.Progression.RandomPlayerWeb.Stat(7,.12f) * Time.deltaTime));
            if (state == PowerCycleState.Recovering && NormalizedPower >= recoveryThreshold)
                ChangeState(PowerCycleState.Ready);
        }

        public void RestorePower(float value, bool recovering)
        {
            SetPower(value);
            ChangeState(recovering && NormalizedPower < recoveryThreshold ? PowerCycleState.Recovering : PowerCycleState.Ready);
        }
        public void SetMaximumPower(float value){maximumPower=Mathf.Max(1,value);SetPower(currentPower);}

        public bool TrySpend(float amount)
        {
            if (Time.timeScale <= 0f || (health != null && !health.IsAlive) ||
                state == PowerCycleState.Recovering || !float.IsFinite(amount) || amount <= 0f ||
                currentPower < amount) return false;
            SetPower(currentPower - amount);
            if (currentPower <= 0f) ChangeState(PowerCycleState.Recovering);
            return true;
        }

        private void SetPower(float value)
        {
            float clamped = Mathf.Clamp(value, 0f, maximumPower);
            if (Mathf.Approximately(currentPower, clamped)) return;
            currentPower = clamped;
            PowerChanged?.Invoke(currentPower, maximumPower);
        }

        private void ChangeState(PowerCycleState value)
        {
            if (state == value) return;
            state = value;
            StateChanged?.Invoke(state);
        }
    }
}
