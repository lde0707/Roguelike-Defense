using Rushitadel.Data;
using System.Collections;
using Rushitadel.Combat;
using Rushitadel.Power;
using UnityEngine;

namespace Rushitadel.Spawning
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private WaveDefinition definition;
        public WaveDefinition Definition => definition;

        [SerializeField] private EnemyController enemyPrefab;

        private float spawnInterval => definition.SpawnInterval;

        private int enemiesPerWave => definition.EnemiesPerWave;
        [SerializeField] private MonoBehaviour enemyCountModifierSource;

        private float leftSpawnX => definition.LeftSpawnX;

        private float rightSpawnX => definition.RightSpawnX;

        private float spawnDepth => definition.SpawnDepth;

        private Vector2 spawnYRange => definition.SpawnYRange;

        private IEnemySpawnModifier enemyCountModifier;

        public void Configure(EnemyController prefab) => enemyPrefab = prefab;

        public void SetEnemyCountModifier(MonoBehaviour modifierSource)
        {
            enemyCountModifierSource = modifierSource;
            ResolveEnemyCountModifier();
        }

        private void Awake()
        {
            if (!DefinitionGuard.Require(this, definition)) return;
            ResolveEnemyCountModifier();
        }

        private IEnumerator Start()
        {
            while (enabled)
            {
                SpawnWave();
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private void SpawnWave()
        {
            if (enemyPrefab == null)
            {
                return;
            }

            int spawnCount = GetCurrentSpawnCount();
            for (int index = 0; index < spawnCount; index++)
            {
                float x = Random.value < 0.5f ? leftSpawnX : rightSpawnX;
                float z = Random.Range(spawnYRange.x, spawnYRange.y);
                EnemyController enemy = Instantiate(enemyPrefab, new Vector3(x, z, spawnDepth), Quaternion.identity);
                enemy.gameObject.SetActive(true);
            }
        }

        private int GetCurrentSpawnCount()
        {
            float multiplier = enemyCountModifier != null
                ? Mathf.Max(1f, enemyCountModifier.EnemyCountMultiplier)
                : 1f;
            return Mathf.Max(1, Mathf.CeilToInt(enemiesPerWave * multiplier));
        }

        private void ResolveEnemyCountModifier()
        {
            enemyCountModifier = enemyCountModifierSource as IEnemySpawnModifier;
            if (enemyCountModifierSource != null && enemyCountModifier == null)
            {
                Debug.LogError(
                    $"{enemyCountModifierSource.name} must implement {nameof(IEnemySpawnModifier)}.",
                    enemyCountModifierSource);
            }
        }
    }
}
