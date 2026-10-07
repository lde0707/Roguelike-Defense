using UnityEngine;

namespace Rushitadel.Combat
{
    /// <summary>Shared XY-plane presentation and physics contract for projectile types.</summary>
    public abstract class ProjectileBehaviour : MonoBehaviour
    {
        public Vector2 VisualCollisionSize { get; private set; }
        protected CapsuleCollider2D visualHitbox;
        public virtual void FitVisualCollision(Vector2 size)
        {
            VisualCollisionSize=size;
            if(visualHitbox==null)
            {
                foreach(var c in GetComponents<Collider2D>())c.enabled=false;
                visualHitbox=gameObject.AddComponent<CapsuleCollider2D>();
                visualHitbox.isTrigger=true;
            }
            var scale=transform.lossyScale;
            visualHitbox.size=new Vector2(size.x/Mathf.Max(.0001f,Mathf.Abs(scale.x)),size.y/Mathf.Max(.0001f,Mathf.Abs(scale.y)));
            visualHitbox.direction=size.x>size.y?CapsuleDirection2D.Horizontal:CapsuleDirection2D.Vertical;
        }
        protected Vector2 CastSize(float fallbackRadius)=>VisualCollisionSize==Vector2.zero?Vector2.one*fallbackRadius*2:VisualCollisionSize;
        protected CapsuleDirection2D CastDirection=>VisualCollisionSize.x>VisualCollisionSize.y?CapsuleDirection2D.Horizontal:CapsuleDirection2D.Vertical;
        protected float WallExtent(float fallbackRadius)
        {
            if(VisualCollisionSize==Vector2.zero)return fallbackRadius;
            float radius=Mathf.Min(VisualCollisionSize.x,VisualCollisionSize.y)*.5f;
            Vector3 axis=VisualCollisionSize.x>VisualCollisionSize.y?transform.right:transform.up;
            return radius+Mathf.Abs(axis.x)*(Mathf.Max(VisualCollisionSize.x,VisualCollisionSize.y)*.5f-radius);
        }
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
