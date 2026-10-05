using UnityEngine;

namespace Rushitadel.Player
{
    [CreateAssetMenu(fileName = "MouseWorldInteractionSettings", menuName = "Rushitadel/Mouse World Interaction Settings")]
    public sealed class MouseWorldInteractionSettings : ScriptableObject
    {
        [SerializeField, Min(0f)] private float radius = 1f;

        public float Radius => radius;
    }
}
