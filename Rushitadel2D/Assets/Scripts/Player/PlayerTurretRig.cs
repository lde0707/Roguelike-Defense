using UnityEngine;

namespace Rushitadel.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerHealth))]
    public sealed class PlayerTurretRig : MonoBehaviour
    {
        public const int TurretCount = 5;
        [SerializeField] private Transform rotatingBody;
        [SerializeField] private float degreesPerSecond = 30f;
        private PlayerHealth health;
        public Transform RotatingBody => rotatingBody;
        public void Configure(Transform body) => rotatingBody = body;
        private void Awake() => health = GetComponent<PlayerHealth>();
        private void Update()
        {
            if (Time.timeScale <= 0f || !health.IsAlive || rotatingBody == null) return;
            rotatingBody.Rotate(0f, 0f, -degreesPerSecond * Time.deltaTime);
        }
    }
}
