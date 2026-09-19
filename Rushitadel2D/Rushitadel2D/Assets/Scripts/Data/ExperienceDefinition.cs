using UnityEngine;

namespace Rushitadel.Data
{
    [CreateAssetMenu(menuName = "Rushitadel 2D/Progression/ExperienceDefinition", fileName = "ExperienceDefinition")]
    public sealed class ExperienceDefinition : GameplayDefinition
    {
        [SerializeField, Min(1)] private int experiencePerThreshold = 100;

        public int ExperiencePerThreshold => experiencePerThreshold;
        protected override void ValidateValues()
        {
            experiencePerThreshold = Mathf.Max(1, experiencePerThreshold);
        }
    }
}