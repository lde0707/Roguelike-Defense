using Rushitadel.Player;
using Rushitadel.Combat;
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
        private void Awake() { Instance = this; RemoveFrontBarrier(); }
        private void RemoveFrontBarrier()
        {
            if(frontBarrier!=null)frontBarrier.gameObject.SetActive(false);
            if(frontVisual!=null)frontVisual.gameObject.SetActive(false);
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Configure(Camera camera, BoxCollider2D rear, BoxCollider2D front,
            ThemePressureVisual rearArt, ThemePressureVisual frontArt)
        {
            gameplayCamera = camera;
            rearHazard = rear;
            frontBarrier = front;
            rearVisual = rearArt;
            frontVisual = frontArt;
            RemoveFrontBarrier();
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
        }

        public void RefreshAnchors()
        {
            float halfHeight = gameplayCamera.orthographicSize;
            // Both objects are camera children. z=10 puts them on the XY gameplay plane.
            rearHazard.transform.localPosition = new Vector3(0f, -halfHeight + rearDepth * 0.5f, 10f);
            var boundary = PlayfieldBoundary.Instance;
            float width = rearHazard.size.x;
            if (boundary != null)
            {
                // Match damage and every theme's art to the same world-space cliff edges.
                var center = rearHazard.transform.position;
                center.x = (boundary.LeftEdge + boundary.RightEdge) * 0.5f;
                rearHazard.transform.position = center;
                width = (boundary.RightEdge - boundary.LeftEdge) / Mathf.Abs(rearHazard.transform.lossyScale.x);
            }
            rearHazard.size = new Vector2(width, rearDepth);
            rearVisual.SetSize(rearHazard.size);
        }

        private void Update()
        {
            var director=Rushitadel.Progression.CampaignDirector.Instance;
            bool boss=director!=null && director.Initialized && director.State.phase==Rushitadel.Progression.RunPhase.Boss;
            if(rearVisual!=null)rearVisual.gameObject.SetActive(!boss);
            if(rearHazard!=null)rearHazard.enabled=!boss;
            if(boss){ExposureSeconds=0;InDanger=false;return;}
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
            position.y = Mathf.Max(position.y, Bottom + radius);
            return position;
        }

        private void OnDisable()
        {
            ExposureSeconds = 0f;
            InDanger = false;
        }
    }
}
