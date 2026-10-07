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
        [SerializeField, Min(0f)] private float attackRange = 3f;
        public float AttackRange => attackRange;
        [SerializeField, Min(0.01f)] private float shotInterval = 0.5f;
        [SerializeField, Min(0f)] private float damage = 25f;

        [Header("Projectile References")]
        [SerializeField] private Transform shotPos;
        [SerializeField] private GameObject projectilePrefab;
        private float nextShotTime;
        private bool alternate;
        public event System.Action Fired;
        public Vector3 MuzzlePosition => shotPos != null ? shotPos.position : transform.position;

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
            int form = Rushitadel.Progression.CampaignDirector.Form(slotId);
            nextShotTime = Time.time + (form > 0 ? TurretForms.Intervals[form] : shotInterval) / (form==6 ? 1 : 1 + .2f * Rushitadel.Progression.CampaignDirector.Rank(slotId, 1));
        }

        private void Fire()
        {
            Transform origin = shotPos != null ? shotPos : transform;
            Fired?.Invoke();
            int form = Rushitadel.Progression.CampaignDirector.Form(slotId);
            if(form>0)
            {
                float mult=1+.25f*Rushitadel.Progression.CampaignDirector.Rank(slotId,0);
                int element=Rushitadel.Progression.CampaignDirector.Instance.State.element;
                Vector3 pos=origin.position;
                if(form==1) { alternate=!alternate; pos+=transform.right*(alternate?.06f:-.06f); }
                TransformationShot.Fire(form,pos,transform.up,mult,slotId,TurretForms.ElementColor(element));
                return;
            }
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
