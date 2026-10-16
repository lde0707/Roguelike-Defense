using UnityEngine;

namespace Rushitadel.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerHealth))]
    public sealed class PlayerTurretRig : MonoBehaviour
    {
        public const int TurretCount = 6;
        public const float CoreRadius = .42f;
        public const float OrbitRadius = .88f;
        [SerializeField] private Transform rotatingBody;
        [SerializeField] private float degreesPerSecond = 30f;
        private PlayerHealth health;
        public Transform RotatingBody => rotatingBody;
        public void Configure(Transform body) => rotatingBody = body;
        private void Awake() => health = GetComponent<PlayerHealth>();
        public void EnsureOrbit()
        {
            if(rotatingBody==null)return;
            var towers=rotatingBody.GetComponentsInChildren<Rushitadel.Combat.TowerController>();
            if(towers.Length==5)
            {
                var extra=Instantiate(towers[0],rotatingBody);
                extra.name="Orbit turret 6";
                extra.SetSlot(6);
                towers=rotatingBody.GetComponentsInChildren<Rushitadel.Combat.TowerController>();
            }
            foreach(var tower in towers)
            {
                float angle=(tower.SlotId-1)*360f/TurretCount;
                tower.transform.localRotation=Quaternion.Euler(0,0,-angle);
                tower.transform.localPosition=tower.transform.localRotation*Vector3.up*OrbitRadius;
                foreach(var collider in tower.GetComponentsInChildren<Collider2D>())collider.enabled=false;
            }
            var hitbox=GetComponent<CircleCollider2D>();
            hitbox.radius=CoreRadius;hitbox.offset=Vector2.zero;
        }
        private void Update()
        {
            if (Time.timeScale <= 0f || !health.IsAlive || rotatingBody == null) return;
            rotatingBody.Rotate(0f, 0f, -degreesPerSecond * Time.deltaTime);
        }
    }
}
