using Rushitadel.Player;
using UnityEngine;

namespace Rushitadel.Combat
{
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyRangedAttack : MonoBehaviour
    {
        [SerializeField] private EnemyProjectileController projectilePrefab;
        [SerializeField, Min(0.1f)] private float shotInterval = 2f;
        [SerializeField, Min(0f)] private float initialDelay = 0.8f;
        private EnemyController owner;
        private float nextShotTime;
        public EnemyProjectileController ProjectilePrefab => projectilePrefab;
        private void Awake() => owner = GetComponent<EnemyController>();
        private void OnEnable() => nextShotTime = Time.time + initialDelay;
        private void Update()
        {
            var target = PlayerHealth.Instance;
            if (Time.timeScale <= 0f || !owner.IsAlive || target == null || !target.IsAlive ||
                projectilePrefab == null || Time.time < nextShotTime) return;
            Vector2 direction = target.transform.position - transform.position;
            var shot = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            shot.Launch(direction);
            nextShotTime = Time.time + shotInterval;
        }
    }
}
