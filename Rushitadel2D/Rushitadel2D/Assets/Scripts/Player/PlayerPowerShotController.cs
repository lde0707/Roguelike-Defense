using Rushitadel.Power;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rushitadel.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerPower))]
    public sealed class PlayerPowerShotController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerPower power;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform shotOrigin;
        [SerializeField] private GameObject projectilePrefab;

        [Header("Power Shot")]
        [SerializeField, Min(0f)] private float powerCost = 20f;
        [SerializeField, Min(0f)] private float explosionRadius = 2.5f;
        [SerializeField, Min(0.01f)] private float projectileScaleMultiplier = 2f;

        private void Awake()
        {
            if (power == null)
            {
                power = GetComponent<PlayerPower>();
            }

            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.rightButton.wasPressedThisFrame)
            {
                return;
            }

            TryFire(mouse.position.ReadValue());
        }

        public void Configure(
            PlayerPower powerResource,
            Camera camera,
            Transform origin,
            GameObject powerProjectilePrefab)
        {
            power = powerResource;
            aimCamera = camera;
            shotOrigin = origin;
            projectilePrefab = powerProjectilePrefab;
        }

        private void TryFire(Vector2 screenPosition)
        {
            if (power == null || aimCamera == null || projectilePrefab == null)
            {
                return;
            }

            Transform origin = shotOrigin != null ? shotOrigin : transform;
            Vector3 destination = GetWorldPosition(screenPosition, origin.position);
            Vector3 direction = destination - origin.position;
            direction.z = 0f;
            direction = direction.sqrMagnitude > 0f ? direction.normalized : transform.up;
            if (!power.TrySpend(powerCost))
            {
                return;
            }

            Quaternion rotation = direction.sqrMagnitude > 0f
                ? Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f)
                : origin.rotation;
            GameObject projectileObject = Instantiate(projectilePrefab, origin.position, rotation);
            projectileObject.transform.localScale *= projectileScaleMultiplier;
            projectileObject.SetActive(true);

            PlayerProjectileController projectile =
                projectileObject.GetComponent<PlayerProjectileController>();
            if (projectile == null)
            {
                projectile = projectileObject.AddComponent<PlayerProjectileController>();
            }

            projectile.LaunchEliminating(direction, destination, explosionRadius);
        }

        private Vector3 GetWorldPosition(Vector2 screenPosition, Vector3 origin)
        {
            Ray mouseRay = aimCamera.ScreenPointToRay(screenPosition);
            Plane movementPlane = new(Vector3.forward, origin);
            if (!movementPlane.Raycast(mouseRay, out float distance))
            {
                return origin + transform.up * 10f;
            }

            Vector3 destination = mouseRay.GetPoint(distance);
            destination.z = origin.z;
            return destination;
        }
    }
}
