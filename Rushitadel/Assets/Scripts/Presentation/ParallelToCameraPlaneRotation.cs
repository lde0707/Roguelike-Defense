using UnityEngine;

namespace Rushitadel.Presentation
{
    public sealed class ParallelToCameraPlaneRotation : ICameraFacingRotation
    {
        public Quaternion Calculate(Transform cameraTransform, Quaternion rotationOffset)
        {
            return cameraTransform.rotation * rotationOffset;
        }
    }
}
