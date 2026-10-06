using Rushitadel.Player;
using UnityEngine;

namespace Rushitadel.World
{
    public sealed class CullBehindCamera : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float margin = 2f;
        private Camera gameplayCamera;
        private void Start() => gameplayCamera = Camera.main;
        private void Update()
        {
            if (Time.timeScale <= 0f || gameplayCamera == null || PlayerHealth.Instance == null || !PlayerHealth.Instance.IsAlive) return;
            if (transform.position.y < gameplayCamera.transform.position.y - gameplayCamera.orthographicSize - margin)
                Destroy(gameObject); // Passing an enemy does not count as killing it or award XP.
        }
    }
}
