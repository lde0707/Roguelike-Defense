using UnityEngine;
using Rushitadel.Player;

namespace Rushitadel.Combat
{
    public sealed class TowerController : MonoBehaviour
    {
        [SerializeField, Range(1, 5)] private int slotId = 1;
        private PlayerHealth owner;
        public int SlotId => slotId;
        private void Awake() => owner = GetComponentInParent<PlayerHealth>();

        [Header("Attack Settings")]
        [SerializeField, Min(0f)] private float attackRange = 5f;
        public float AttackRange => attackRange;
        [SerializeField, Min(0.01f)] private float shotInterval = 0.5f;
        [SerializeField, Min(0f)] private float damage = 25f;

        [Header("Projectile References")]
        [SerializeField] private Transform shotPos;
        [SerializeField] private GameObject projectilePrefab;
        private float nextShotTime;

        public void Configure(Transform newShotPos, GameObject newProjectilePrefab)
        {
            shotPos = newShotPos != null ? newShotPos : transform;
            projectilePrefab = newProjectilePrefab;
        }

        private void OnEnable() => TowerRegistry.Register(this);

        private void OnDisable() => TowerRegistry.Unregister(this);

        private void Update()
        {
            if (Time.timeScale <= 0f || owner == null || !owner.IsAlive || Time.time < nextShotTime || projectilePrefab == null)
            {
                return;
            }

            Fire();
            nextShotTime = Time.time + shotInterval / (1 + .2f * Rushitadel.Progression.CampaignDirector.Rank(slotId, 1));
        }

        private void Fire()
        {
            Transform origin = shotPos != null ? shotPos : transform;
            GameObject projectileObject = Instantiate(projectilePrefab, origin.position, Quaternion.identity);
            projectileObject.SetActive(true);

            ProjectileController projectile = projectileObject.GetComponent<ProjectileController>();
            if (projectile == null)
            {
                projectile = projectileObject.AddComponent<ProjectileController>();
            }

            // Capture forward at launch. Neither the mouse nor enemy positions steer this shot.
            projectile.Launch(transform.up, damage * (1 + .25f * Rushitadel.Progression.CampaignDirector.Rank(slotId, 0)), attackRange);
            projectile.SetModifiers(Rushitadel.Progression.CampaignDirector.Rank(slotId, 2), false, false);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.25f, 0.1f, 0.75f);
            Vector3 origin = shotPos != null ? shotPos.position : transform.position;
            Gizmos.DrawLine(origin, origin + transform.up * attackRange);
        }
    }
}
