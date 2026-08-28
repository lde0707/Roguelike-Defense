using UnityEngine;

namespace Rushitadel.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CameraAlignedSprite : CameraFacingBehaviour
    {
        [Header("Rendering")]
        [SerializeField] private int sortingOrder = 100;

        [Header("Grounding")]
        [SerializeField] private Collider characterCollider;
        [SerializeField] private Vector3 localFootPoint = new(0.06f, -2.66f, 0f);
        [SerializeField, Min(0f)] private float groundClearance = 0.6f;

        private static readonly ICameraFacingRotation SharedRotationPolicy =
            new ParallelToCameraPlaneRotation();

        private SpriteRenderer cachedSpriteRenderer;

        protected override ICameraFacingRotation RotationPolicy => SharedRotationPolicy;

        public void SetSortingOrder(int order)
        {
            sortingOrder = order;
            ApplySortingOrder();
        }

        public void SetCharacterCollider(Collider collider)
        {
            characterCollider = collider;
            RefreshAlignment();
        }

        protected override void OnAlignedWithCamera()
        {
            ApplySortingOrder();
            if (characterCollider == null)
            {
                return;
            }

            Bounds colliderBounds = characterCollider.bounds;
            Vector3 targetFootPosition = new(
                colliderBounds.center.x,
                colliderBounds.min.y + groundClearance,
                colliderBounds.center.z);
            Vector3 footOffset = FacingTransform.TransformVector(localFootPoint);
            FacingTransform.position = targetFootPosition - footOffset;
        }

        private void ApplySortingOrder()
        {
            if (cachedSpriteRenderer == null)
            {
                cachedSpriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (cachedSpriteRenderer != null && cachedSpriteRenderer.sortingOrder != sortingOrder)
            {
                cachedSpriteRenderer.sortingOrder = sortingOrder;
            }
        }
    }
}
