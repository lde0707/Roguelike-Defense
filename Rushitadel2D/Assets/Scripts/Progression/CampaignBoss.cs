using UnityEngine;
using Rushitadel.Combat;
using Rushitadel.Player;

namespace Rushitadel.Progression
{
    public sealed class CampaignBoss : MonoBehaviour
    {
        public EnemyController Health { get; private set; }
        public string PatternLabel => charging ? (pattern == 1 ? "전방위 구체 · 충전" : pattern == 2 ? "조준 3연탄 · 충전" : "부채꼴 탄막 · 충전") : "회복";
        private EnemyProjectileController projectile;
        private int stage, pattern, burst;
        private float timer, burstTimer;
        private bool charging;
        private Vector2 aim;
        private SpriteRenderer art;
        public int ShotsFired { get; private set; }
        public void Configure(EnemyProjectileController prefab, int value)
        {
            projectile = prefab; stage = value; Health = GetComponent<EnemyController>();
            art = GetComponent<SpriteRenderer>(); timer = 1.5f;
        }
        private void Update()
        {
            if (Time.timeScale <= 0 || !Health.IsAlive || !PlayerHealth.Instance.IsAlive) return;
            if (burst > 0)
            {
                burstTimer -= Time.deltaTime;
                if (burstTimer <= 0) { Fire(aim); burst--; burstTimer = .22f; }
            }
            timer -= Time.deltaTime;
            if (charging) art.color = ((int)(Time.time * 8) % 2 == 0) ? new Color(1,.65f,.4f) : Color.white;
            if (timer > 0) return;
            if (!charging)
            {
                charging = true; timer = 1.2f;
                aim = ((Vector2)(PlayerHealth.Instance.transform.position - transform.position)).normalized;
                return;
            }
            charging = false; art.color = Color.white;
            if (pattern == 0) for (int i = -2; i <= 2; i++) Fire(Quaternion.Euler(0,0,i*18) * aim);
            else if (pattern == 1) for (int i = 0; i < 10 + stage * 2; i++) Fire(Quaternion.Euler(0,0,i * 360f/(10+stage*2)) * Vector2.down);
            else { burst = 3; burstTimer = 0; }
            pattern = (pattern + 1) % 3; timer = 2.2f;
        }
        private void Fire(Vector2 direction)
        {
            if (projectile == null || !Health.IsAlive) return;
            ShotsFired++;
            var shot = Instantiate(projectile, transform.position, Quaternion.identity);
            shot.gameObject.SetActive(true); shot.Launch(direction);
        }
    }
}
