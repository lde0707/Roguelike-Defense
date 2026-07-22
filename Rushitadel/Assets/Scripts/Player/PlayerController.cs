using Rushitadel.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rushitadel.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 250f;

        [Header("Shooting")]
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform playerShotPos;
        [SerializeField] private GameObject playerBulletPrefab;
        [SerializeField, Min(0.01f)] private float shotsPerSecond = 4f;
        [SerializeField, Min(0f)] private float bulletDamage = 25f;

        private Rigidbody body;
        private Vector2 moveInput;
        private float nextShotTime;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }
        }

        private void Update()
        {
            ReadMovementInput();
            HandleShooting();
        }

        private void FixedUpdate()
        {
            Vector3 movement = new(moveInput.x, 0f, moveInput.y);
            body.linearVelocity = movement * moveSpeed;
        }

        private void OnDisable()
        {
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
            }
        }

        private void ReadMovementInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                moveInput = Vector2.zero;
                return;
            }

            float horizontal = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float vertical = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            moveInput = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        }

        private void HandleShooting()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.isPressed || Time.time < nextShotTime)
            {
                return;
            }

            Fire();
            nextShotTime = Time.time + 1f / shotsPerSecond;
        }

        private void Fire()
        {
            if (playerBulletPrefab == null)
            {
                return;
            }

            Transform origin = playerShotPos != null ? playerShotPos : transform;
            Vector3 destination = GetMouseWorldPosition(origin.position);
            Vector3 direction = destination - origin.position;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0f ? direction.normalized : transform.forward;
            Quaternion rotation = direction.sqrMagnitude > 0f
                ? Quaternion.LookRotation(direction, Vector3.up)
                : origin.rotation;
            GameObject bulletObject = Instantiate(playerBulletPrefab, origin.position, rotation);
            bulletObject.SetActive(true);

            PlayerProjectileController bullet = bulletObject.GetComponent<PlayerProjectileController>();
            if (bullet == null)
            {
                bullet = bulletObject.AddComponent<PlayerProjectileController>();
            }

            bullet.Launch(direction, destination, bulletDamage);
        }

        private Vector3 GetMouseWorldPosition(Vector3 origin)
        {
            Mouse mouse = Mouse.current;
            if (aimCamera == null || mouse == null)
            {
                return origin + transform.forward * 1000f;
            }

            Ray mouseRay = aimCamera.ScreenPointToRay(mouse.position.ReadValue());
            Plane movementPlane = new(Vector3.up, origin);
            if (!movementPlane.Raycast(mouseRay, out float distance))
            {
                return origin + transform.forward * 1000f;
            }

            Vector3 destination = mouseRay.GetPoint(distance);
            destination.y = origin.y;
            return destination;
        }
    }
}
