using UnityEngine;
namespace Rushitadel.Presentation
{
    public sealed class TitleMenu : MonoBehaviour
    {
        private void Awake() => GameFrontend.Ensure();
    }
}
