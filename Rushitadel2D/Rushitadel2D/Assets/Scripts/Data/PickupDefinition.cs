using UnityEngine;

namespace Rushitadel.Data
{
    [CreateAssetMenu(menuName = "Rushitadel 2D/Progression/PickupDefinition", fileName = "PickupDefinition")]
    public sealed class PickupDefinition : GameplayDefinition
    {
        [SerializeField, Min(1)] private int experienceValue = 20;
        [SerializeField, Min(0f)] private float collectSpeed = 14f;
        [SerializeField, Min(0f)] private float collectDistance = 0.5f;

        public int ExperienceValue => experienceValue;
        public float CollectSpeed => collectSpeed;
        public float CollectDistance => collectDistance;
        protected override void ValidateValues()
        {
            experienceValue = Mathf.Max(1, experienceValue);
            collectSpeed = Mathf.Max(0f, collectSpeed);
            collectDistance = Mathf.Max(0f, collectDistance);
        }
    }
}