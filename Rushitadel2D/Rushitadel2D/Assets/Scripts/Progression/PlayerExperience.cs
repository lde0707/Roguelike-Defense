using Rushitadel.Data;
using System;
using UnityEngine;

namespace Rushitadel.Progression
{
    public sealed class PlayerExperience : MonoBehaviour
    {
        [SerializeField] private ExperienceDefinition definition;
        public ExperienceDefinition Definition => definition;

        private void Awake() => DefinitionGuard.Require(this, definition);


        private int experiencePerThreshold => definition.ExperiencePerThreshold;
        private int totalExperience;

        public int TotalExperience => totalExperience;
        public int ExperiencePerThreshold => experiencePerThreshold;
        public int CompletedThresholds => totalExperience / experiencePerThreshold;
        public int CurrentGaugeExperience => totalExperience % experiencePerThreshold;
        public float NormalizedGauge => (float)CurrentGaugeExperience / experiencePerThreshold;

        public event Action<int> ExperienceChanged;

        public void AddExperience(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            totalExperience += amount;
            ExperienceChanged?.Invoke(totalExperience);
        }
    }
}
