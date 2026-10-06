using System.Collections;
using Rushitadel.Player;
using Rushitadel.Combat;
using System.Linq;
using UnityEngine;

namespace Rushitadel.Spawning
{
    // CampaignDirector owns finite wave spawning; the coroutine remains a standalone test fallback.
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField, Min(0.01f)] private float spawnInterval = 2f;
        [SerializeField, Min(1)] private int enemiesPerBatch = 2;
        [SerializeField] private float leftSpawnX = -7.3f;
        [SerializeField] private float rightSpawnX = 7.3f;
        [SerializeField, Min(1)] private int maximumEnemies = 8;
        [SerializeField] private Vector2 spawnYRange = new(1.5f, 5.5f);
        public void Configure(GameObject prefab) => enemyPrefab = prefab;
        public bool CampaignControlled { get; private set; }
        public void TakeCampaignControl() { CampaignControlled = true; StopAllCoroutines(); }
        public EnemyController SpawnCampaignEnemy(int stage, bool boss = false)
        {
            if (enemyPrefab == null) return null;
            float cameraY = Camera.main.transform.position.y;
            var pos = boss ? new Vector3(0, cameraY + 2.5f, 0) :
                new Vector3(Random.value < .5f ? leftSpawnX : rightSpawnX, cameraY + Random.Range(1.5f, 4f), 0);
            var go = Instantiate(enemyPrefab, pos, Quaternion.identity); go.SetActive(true);
            var enemy = go.GetComponent<EnemyController>();
            enemy.ConfigureRun(boss ? 450 + stage * 200 : 60 + stage * 20, boss ? 100 : 20, true);
            if (!boss)
            {
                var sentry = go.GetComponent<SpriteRenderer>(); sentry.sprite = Rushitadel.Progression.PixelRunArt.Sentry; sentry.color = Color.white;
                go.transform.localScale = Vector3.one * .65f;
            }
            if (boss)
            {
                go.name = "Stage " + stage + " Pixel Sentinel";
                var attack = go.GetComponent<EnemyRangedAttack>();
                var projectile = attack.ProjectilePrefab; attack.enabled = false;
                if (go.TryGetComponent<Rushitadel.World.CullBehindCamera>(out var cull)) cull.enabled = false;
                go.transform.localScale = Vector3.one * 2.4f;
                var art = go.GetComponent<SpriteRenderer>(); art.sprite = Rushitadel.Progression.PixelRunArt.Boss; art.color = Color.white;
                if (go.TryGetComponent<CircleCollider2D>(out var circle)) circle.radius = .36f;
                if (go.TryGetComponent<BoxCollider2D>(out var box)) box.size = new Vector2(.75f,.7f);
                go.AddComponent<Rushitadel.Progression.CampaignBoss>().Configure(projectile, stage);
            }
            return enemy;
        }
        private IEnumerator Start()
        {
            while (enabled && !CampaignControlled)
            {
                if (Time.timeScale > 0f && PlayerHealth.Instance != null && PlayerHealth.Instance.IsAlive)
                    SpawnBatch();
                yield return new WaitForSeconds(spawnInterval);
            }
        }
        private void SpawnBatch()
        {
            if (enemyPrefab == null) return;
            int available = Mathf.Max(0, maximumEnemies - EnemyRegistry.ActiveEnemies.Count());
            for (int i = 0; i < Mathf.Min(enemiesPerBatch, available); i++)
            {
                float x = Random.value < 0.5f ? leftSpawnX : rightSpawnX;
                float cameraY = Camera.main != null ? Camera.main.transform.position.y : 0f;
                GameObject enemy = Instantiate(enemyPrefab, new Vector3(x, cameraY + Random.Range(spawnYRange.x, spawnYRange.y), 0f), Quaternion.identity);
                enemy.SetActive(true);
            }
        }
    }
}
