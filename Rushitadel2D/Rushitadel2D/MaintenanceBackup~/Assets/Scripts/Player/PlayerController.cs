using Rushitadel.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rushitadel.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 2.5f;

        [Header("Shooting")]
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform playerShotPos;
        [SerializeField] private GameObject playerBulletPrefab;
        [SerializeField, Min(0.01f)] private float shotsPerSecond = 4f;
        [SerializeField, Min(0f)] private float bulletDamage = 25f;

        private Rigidbody2D body;
        private Vector2 moveInput;
        private float nextShotTime;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            ReadMovementInput();
            HandleShooting();
        }

        private void FixedUpdate()
        {
            Vector3 movement = new(moveInput.x, moveInput.y, 0f);
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
            direction.z = 0f;
            direction = direction.sqrMagnitude > 0f ? direction.normalized : transform.up;
            Quaternion rotation = direction.sqrMagnitude > 0f
                ? Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f)
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
                return origin + transform.up * 10f;
            }

            Ray mouseRay = aimCamera.ScreenPointToRay(mouse.position.ReadValue());
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
