using UnityEngine;

namespace Rushitadel.Presentation
{
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class PlayerSpriteMotion : MonoBehaviour
    {
        private Rigidbody2D body;
        private Animator animator;
        private SpriteRenderer sprite;

        private void Awake()
        {
            body = GetComponentInParent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            sprite = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            Vector2 velocity = body != null ? body.linearVelocity : Vector2.zero;
            animator.speed = velocity.sqrMagnitude > 0.01f ? 1f : 0f;
            if (Mathf.Abs(velocity.x) > 0.01f) sprite.flipX = velocity.x > 0f;
        }
    }
}
