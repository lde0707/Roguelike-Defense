using System;
using UnityEngine;

namespace Rushitadel.Progression
{
    public sealed class PlayerExperience : MonoBehaviour
    {
        [SerializeField, Min(1)] private int experiencePerThreshold = 100;
        [SerializeField, Min(0)] private int totalExperience;

        public int TotalExperience => totalExperience;
        public int Level => CompletedThresholds + 1;
        public int ExperiencePerThreshold => experiencePerThreshold;
        public int CompletedThresholds => totalExperience / experiencePerThreshold;
        public int CurrentGaugeExperience => totalExperience % experiencePerThreshold;
        public float NormalizedGauge => (float)CurrentGaugeExperience / experiencePerThreshold;

        public event Action<int> ExperienceChanged;

        public void RestoreExperience(int amount)
        {
            totalExperience = Mathf.Max(0, amount);
            ExperienceChanged?.Invoke(totalExperience);
        }

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
