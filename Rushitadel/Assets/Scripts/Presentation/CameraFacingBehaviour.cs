using UnityEngine;

namespace Rushitadel.Presentation
{
    [ExecuteAlways]
    public abstract class CameraFacingBehaviour : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Vector3 rotationOffsetEuler = Vector3.zero;

        protected abstract ICameraFacingRotation RotationPolicy { get; }

        protected virtual Transform FacingTransform => transform;

        private void OnEnable()
        {
            AlignWithCamera();
        }

        private void LateUpdate()
        {
            AlignWithCamera();
        }

        private void OnValidate()
        {
            AlignWithCamera();
        }

        public void SetTargetCamera(Camera camera)
        {
            targetCamera = camera;
            RefreshAlignment();
        }

        protected void RefreshAlignment()
        {
            Camera camera = ResolveCamera();
            if (camera == null || FacingTransform == null)
            {
                return;
            }

            Quaternion rotationOffset = Quaternion.Euler(rotationOffsetEuler);
            FacingTransform.rotation = RotationPolicy.Calculate(camera.transform, rotationOffset);
            OnAlignedWithCamera();
        }

        protected virtual void OnAlignedWithCamera()
        {
        }

        private Camera ResolveCamera()
        {
            return targetCamera != null ? targetCamera : Camera.main;
        }

        private void AlignWithCamera()
        {
            RefreshAlignment();
        }
    }
}
