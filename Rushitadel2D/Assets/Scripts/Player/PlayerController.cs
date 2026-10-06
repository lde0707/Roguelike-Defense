using UnityEngine;
using UnityEngine.InputSystem;
using Rushitadel.Combat;
using Rushitadel.World;

namespace Rushitadel.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerHealth), typeof(CircleCollider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 2.5f;
        [SerializeField] private Vector2 minimumPosition = new(-5.4f, -4.3f);
        [SerializeField] private Vector2 maximumPosition = new(5.4f, 4.3f);
        private Rigidbody2D body;
        private PlayerHealth health;
        private Vector2 moveInput;
        private Vector2 recoilVelocity;
        private float recoilRemaining;
        private CircleCollider2D hitbox;
        public float BodyRadius => hitbox.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<PlayerHealth>();
            hitbox = GetComponent<CircleCollider2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Update()
        {
            moveInput = Vector2.zero;
            if (Time.timeScale <= 0f || !health.IsAlive) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            moveInput = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        private void FixedUpdate()
        {
            if (!health.IsAlive) { body.linearVelocity = Vector2.zero; return; }
            float recoilStep = Mathf.Min(recoilRemaining, Time.fixedDeltaTime);
            Vector2 next = body.position + (recoilStep > 0f ? recoilVelocity * recoilStep :
                moveInput * (moveSpeed * (Rushitadel.Progression.CampaignDirector.HasWeb(5) ? 1.1f : 1f) * Time.fixedDeltaTime));
            recoilRemaining = Mathf.Max(0f, recoilRemaining - recoilStep);
            next.x = Mathf.Clamp(next.x, minimumPosition.x, maximumPosition.x);
            var pressure = ForwardPressureController.Instance;
            if (pressure != null && pressure.isActiveAndEnabled) next = pressure.ClampPlayer(next, BodyRadius);
            else next.y = Mathf.Clamp(next.y, minimumPosition.y, maximumPosition.y);
            if (PlayfieldBoundary.Instance != null) next = PlayfieldBoundary.Instance.ClampPlayer(next, BodyRadius);
            body.MovePosition(next);
        }

        public void ApplyRecoil(Vector2 firingDirection, float distance, float duration)
        {
            if (!health.IsAlive || Time.timeScale <= 0f || duration <= 0f) return;
            recoilVelocity = -firingDirection.normalized * Mathf.Max(0f, distance) / duration;
            recoilRemaining = duration;
        }

        private void OnDisable()
        {
            if (body != null) body.linearVelocity = Vector2.zero;
            recoilRemaining = 0f;
        }
    }
}
