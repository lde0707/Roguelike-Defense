using Rushitadel.Player;
using UnityEngine;

namespace Rushitadel.World
{
    /// <summary>The camera advances independently. Standing still means falling behind on screen.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class ForwardPressureController : MonoBehaviour
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private BoxCollider2D rearHazard;
        [SerializeField] private BoxCollider2D frontBarrier;
        [SerializeField] private ThemePressureVisual rearVisual;
        [SerializeField] private ThemePressureVisual frontVisual;
        [SerializeField] private PressureTheme theme = PressureTheme.Electric;
        [SerializeField, Min(0f)] private float forwardSpeed = 0.8f;
        [SerializeField, Min(0.1f)] private float rearDepth = 1.2f;
        [SerializeField, Min(0.1f)] private float frontDepth = 0.6f;
        [SerializeField, Min(0f)] private float frontInset = 0.35f;
        [SerializeField, Min(0.1f)] private float damageInterval = 1f;
        [SerializeField, Range(0.01f, 1f)] private float damageFraction = 0.5f;
        private PressureTheme appliedTheme;
        private bool themeApplied;
        public static ForwardPressureController Instance { get; private set; }
        public float DistanceTravelled { get; private set; }
        public float ExposureSeconds { get; private set; }
        public bool InDanger { get; private set; }
        public float ForwardSpeed => forwardSpeed;
        public bool HoldAdvance { get; set; }
        public Camera GameplayCamera => gameplayCamera;
        public BoxCollider2D RearHazard => rearHazard;
        public BoxCollider2D FrontBarrier => frontBarrier;
        public PressureTheme Theme => theme;
        public float Bottom => gameplayCamera.transform.position.y - gameplayCamera.orthographicSize;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() => Instance = null;
        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Configure(Camera camera, BoxCollider2D rear, BoxCollider2D front,
            ThemePressureVisual rearArt, ThemePressureVisual frontArt)
        {
            gameplayCamera = camera;
            rearHazard = rear;
            frontBarrier = front;
            rearVisual = rearArt;
            frontVisual = frontArt;
            RefreshAnchors();
            SetTheme(theme);
        }

        public void RestoreProgress(float cameraY, float distance, PressureTheme value)
        {
            var p = gameplayCamera.transform.position;
            p.y = cameraY; gameplayCamera.transform.position = p;
            DistanceTravelled = distance; ExposureSeconds = 0f; InDanger = false;
            RefreshAnchors(); SetTheme(value);
        }

        public void SetTheme(PressureTheme value)
        {
            theme = value;
            appliedTheme = value;
            themeApplied = true;
            rearVisual.ApplyTheme(value);
            frontVisual.ApplyTheme(value);
        }

        public void RefreshAnchors()
        {
            float halfHeight = gameplayCamera.orthographicSize;
            // Both objects are camera children. z=10 puts them on the XY gameplay plane.
            rearHazard.transform.localPosition = new Vector3(0f, -halfHeight + rearDepth * 0.5f, 10f);
            frontBarrier.transform.localPosition = new Vector3(0f, halfHeight - frontInset - frontDepth * 0.5f, 10f);
            rearHazard.size = new Vector2(rearHazard.size.x, rearDepth);
            frontBarrier.size = new Vector2(frontBarrier.size.x, frontDepth);
            rearVisual.SetSize(rearHazard.size);
            frontVisual.SetSize(frontBarrier.size);
        }

        private void Update()
        {
            var player = PlayerHealth.Instance;
            if (gameplayCamera == null || Time.timeScale <= 0f || player == null || !player.IsAlive) return;
            if (!themeApplied || appliedTheme != theme) SetTheme(theme);
            float advance = HoldAdvance ? 0 : forwardSpeed * Time.deltaTime;
            gameplayCamera.transform.position += Vector3.up * advance;
            DistanceTravelled += advance;
            RefreshAnchors();
            Physics2D.SyncTransforms();
            var movement = player.GetComponent<PlayerController>();
            Vector2 center = player.GetComponent<Rigidbody2D>().position;
            Vector2 closest = rearHazard.ClosestPoint(center);
            InDanger = Vector2.SqrMagnitude(center - closest) <= movement.BodyRadius * movement.BodyRadius;
            if (!InDanger) { ExposureSeconds = 0f; return; }
            ExposureSeconds += Time.deltaTime;
            while (ExposureSeconds >= damageInterval && player.IsAlive)
            {
                ExposureSeconds -= damageInterval;
                player.TakeEnvironmentalDamage(player.MaximumHealth * damageFraction);
            }
        }

        public Vector2 ClampPlayer(Vector2 position, float radius)
        {
            position.y = Mathf.Clamp(position.y, Bottom + radius, frontBarrier.bounds.min.y - radius);
            return position;
        }

        private void OnDisable()
        {
            ExposureSeconds = 0f;
            InDanger = false;
        }
    }
}
