using UnityEngine;

namespace Rushitadel.Combat
{
    /// <summary>Solid player walls; hostile bullets only reflect on an inside-to-outside crossing.</summary>
    public sealed class PlayfieldBoundary : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D leftWall;
        [SerializeField] private BoxCollider2D rightWall;
        public static PlayfieldBoundary Instance { get; private set; }
        public float LeftEdge => leftWall.bounds.max.x;
        public float RightEdge => rightWall.bounds.min.x;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() => Instance = null;
        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public void Configure(BoxCollider2D left, BoxCollider2D right) { leftWall = left; rightWall = right; }

        public Vector2 ClampPlayer(Vector2 position, float radius)
        {
            position.x = Mathf.Clamp(position.x, LeftEdge + radius, RightEdge - radius);
            return position;
        }

        public bool Contains(Vector2 position) => position.x >= LeftEdge && position.x <= RightEdge;

        public bool TryGetExitHit(Vector2 start, Vector2 direction, float radius, float travel,
            out float distance, out Vector2 normal)
        {
            distance = 0f;
            normal = Vector2.zero;
            if (Mathf.Abs(direction.x) < 0.00001f) return false;
            // Only the FAR side is considered. The entry wall is deliberately transparent.
            bool goingRight = direction.x > 0f;
            float edge = goingRight ? RightEdge : LeftEdge;
            if (goingRight ? start.x > edge + 0.0001f : start.x < edge - 0.0001f) return false;
            float centerLimit = edge + (goingRight ? -radius : radius);
            distance = Mathf.Max(0f, (centerLimit - start.x) / direction.x);
            if (distance > travel) return false;
            Bounds wall = (goingRight ? rightWall : leftWall).bounds;
            float y = start.y + direction.y * distance;
            if (y < wall.min.y - radius || y > wall.max.y + radius) return false;
            normal = goingRight ? Vector2.left : Vector2.right;
            return true;
        }
    }
}
