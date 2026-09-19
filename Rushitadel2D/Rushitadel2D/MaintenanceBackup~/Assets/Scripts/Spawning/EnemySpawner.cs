using System.Collections;
using Rushitadel.Combat;
using Rushitadel.Power;
using UnityEngine;

namespace Rushitadel.Spawning
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField, Min(0.01f)] private float spawnInterval = 1f;
        [SerializeField, Min(1)] private int enemiesPerWave = 2;
        [SerializeField] private MonoBehaviour enemyCountModifierSource;
        [Header("Spawn Position")]
        [SerializeField] private float leftSpawnX = -9.5f;
        [SerializeField] private float rightSpawnX = 11f;
        [SerializeField] private float spawnDepth = 0f;
        [SerializeField] private Vector2 spawnYRange = new(-4f, 5f);

        private IEnemySpawnModifier enemyCountModifier;

        public void Configure(GameObject prefab) => enemyPrefab = prefab;

        public void SetEnemyCountModifier(MonoBehaviour modifierSource)
        {
            enemyCountModifierSource = modifierSource;
            ResolveEnemyCountModifier();
        }

        private void Awake()
        {
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
                GameObject enemyObject = Instantiate(enemyPrefab, new Vector3(x, z, spawnDepth), Quaternion.identity);
                if (enemyObject.GetComponent<EnemyController>() == null)
                {
                    enemyObject.AddComponent<EnemyController>();
                }

                enemyObject.SetActive(true);
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
