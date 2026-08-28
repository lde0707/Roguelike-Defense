using UnityEngine;

namespace Rushitadel.Presentation
{
    public interface ICameraFacingRotation
    {
        Quaternion Calculate(Transform cameraTransform, Quaternion rotationOffset);
    }
}
