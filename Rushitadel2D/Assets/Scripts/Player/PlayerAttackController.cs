using Rushitadel.Combat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Rushitadel.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class PlayerAttackController : MonoBehaviour
    {
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform shotOrigin;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField, Min(0f)] private float damage = 25f;
        [SerializeField, Min(0.01f)] private float shotInterval = 0.25f;
        [SerializeField, Min(0f)] private float attackRange = 8f;
        private PlayerHealth health;
        private float nextShotTime;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            if (aimCamera == null) aimCamera = Camera.main;
        }

        public void Configure(Camera camera, Transform origin, GameObject basicProjectilePrefab)
        {
            aimCamera = camera;
            shotOrigin = origin;
            projectilePrefab = basicProjectilePrefab;
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed) TryFire(mouse.position.ReadValue());
        }

        public bool TryFire(Vector2 screenPosition)
        {
            if (Time.timeScale <= 0f || health == null || !health.IsAlive || Time.time < nextShotTime ||
                aimCamera == null || projectilePrefab == null ||
                (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return false;

            Transform origin = shotOrigin != null ? shotOrigin : transform;
            Ray ray = aimCamera.ScreenPointToRay(screenPosition);
            var plane = new Plane(Vector3.forward, origin.position);
            if (!plane.Raycast(ray, out float distance)) return false;
            Vector3 direction = ray.GetPoint(distance) - origin.position;
            direction.z = 0f;
            if (direction.sqrMagnitude < 0.0001f) return false;

            int spread = Rushitadel.Progression.CampaignDirector.HasWeb(1) ? 1 : 0;
            for (int i = -spread; i <= spread; i++)
            {
            GameObject shot = Instantiate(projectilePrefab, origin.position, Quaternion.identity);
            shot.SetActive(true);
            var projectile = shot.GetComponent<ProjectileController>();
            if (projectile == null) projectile = shot.AddComponent<ProjectileController>();
            projectile.Launch(Quaternion.Euler(0,0,i*12) * direction, damage, attackRange);
            projectile.SetModifiers(Rushitadel.Progression.CampaignDirector.HasWeb(0) ? 1 : 0,
                Rushitadel.Progression.CampaignDirector.HasWeb(2), Rushitadel.Progression.CampaignDirector.HasWeb(3));
            }
            nextShotTime = Time.time + shotInterval;
            return true;
        }
    }
}
