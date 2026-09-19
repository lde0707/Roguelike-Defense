using UnityEngine;
using Rushitadel.Data;

namespace Rushitadel.Player
{
    [CreateAssetMenu(fileName = "MouseWorldInteractionSettings", menuName = "Rushitadel/Mouse World Interaction Settings")]
    public sealed class MouseWorldInteractionSettings : GameplayDefinition
    {
        [SerializeField, Min(0f)] private float radius = 1f;

        public float Radius => radius;
        protected override void ValidateValues() => radius = Mathf.Max(0f, radius);
    }
}
