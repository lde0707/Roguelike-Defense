using UnityEngine;

namespace Rushitadel.Combat
{
    /// <summary>Shared XY-plane presentation and physics contract for projectile types.</summary>
    public abstract class ProjectileBehaviour : MonoBehaviour
    {
        protected void PrepareBody()
        {
            if (TryGetComponent(out Rigidbody2D body))
            {
                body.gravityScale = 0f;
                body.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        protected void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0f)
                transform.rotation = Quaternion.Euler(0f, 0f,
                    Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
        }

        protected static void ApplyDamage(IDamageable target, float amount)
        {
            if (target != null && target.IsAlive) target.TakeDamage(amount);
        }
    }
}
