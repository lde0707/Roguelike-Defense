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
        private int rosterIndex;
        // Boss endurance grows faster than ordinary enemies so later builds still see multiple patterns.
        public static float BossHealthForStage(int stage) => 6000f + (Mathf.Max(1, stage) - 1) * 3000f;
        public void TakeCampaignControl() { CampaignControlled = true; StopAllCoroutines(); }
        public EnemyController SpawnCampaignEnemy(int stage, bool boss = false, int forcedVariant = -1, GameObject selectedPrefab = null)
        {
            var prefab = !boss && selectedPrefab != null ? selectedPrefab : enemyPrefab;
            if (prefab == null) return null;
            float cameraY = Camera.main.transform.position.y;
            int variant=forcedVariant >= 0 ? Mathf.Clamp(forcedVariant,0,6) : rosterIndex%7;
            if(!boss && variant>=5 && FindObjectsByType<EnemyArchetype>(FindObjectsSortMode.None).Count(e=>e.Elite)>=2)return null;
            var pos=new Vector3(0,cameraY+2.5f,0);
            if(!boss)
            {
                // Near-cliff suppression, rear artillery/snipers, and staggered elite crossfire.
                float preferredY=variant==2||variant==3?3.7f:variant>=5?3f:1.6f;
                bool placed=false;
                for(int attempt=0;attempt<10;attempt++)
                {
                    bool left=(rosterIndex+attempt)%2==0;
                    float edge=PlayfieldBoundary.Instance!=null?(left?PlayfieldBoundary.Instance.LeftEdge:PlayfieldBoundary.Instance.RightEdge):(left?-5.6f:5.6f);
                    float offset=variant==2||variant==3?2.4f:1.9f;
                    pos=new Vector3(edge+(left?-offset:offset),cameraY+preferredY-(attempt/2)*1.4f,0);
                    if(EnemyRegistry.ActiveEnemies.Any(e=>e!=null&&Vector2.Distance(e.transform.position,pos)<1.25f))continue;
                    placed=true;break;
                }
                if(!placed)return null;
                rosterIndex++;
            }
            var go = Instantiate(prefab, pos, Quaternion.identity); go.SetActive(true);
            var enemy = go.GetComponent<EnemyController>();
            enemy.ConfigureRun(boss ? BossHealthForStage(stage) : 180 + (stage-1) * 60, boss ? 100 : 20, true);
            if (!boss)
            {
                var theme=Rushitadel.World.ForwardPressureController.Instance!=null?Rushitadel.World.ForwardPressureController.Instance.Theme:Rushitadel.World.PressureTheme.Grass;
                var identity=go.GetComponent<CampaignEnemyPrefab>();
                if(identity!=null)theme=identity.ResolveTheme(theme);
                (go.GetComponent<EnemyArchetype>() ?? go.AddComponent<EnemyArchetype>()).Configure(variant,stage,theme);
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
                SpawnCampaignEnemy(1);
            }
        }
    }
}
