using Rushitadel.Data;
using Rushitadel.Power;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rushitadel.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerPower))]
    public sealed class PlayerPowerShotController : MonoBehaviour
    {
        [SerializeField] private PowerShotDefinition definition;
        public PowerShotDefinition Definition => definition;

        [Header("References")]
        [SerializeField] private PlayerPower power;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform shotOrigin;
        [SerializeField] private PlayerProjectileController projectilePrefab;

        private float powerCost => definition.PowerCost;

        private float explosionRadius => definition.ExplosionRadius;

        private float projectileScaleMultiplier => definition.ProjectileScaleMultiplier;

        private void Awake()
        {
            if (!DefinitionGuard.Require(this, definition)) return;
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
            PlayerProjectileController powerProjectilePrefab)
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
            PlayerProjectileController projectile = Instantiate(projectilePrefab, origin.position, rotation);
            projectile.transform.localScale *= projectileScaleMultiplier;
            projectile.gameObject.SetActive(true);
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
