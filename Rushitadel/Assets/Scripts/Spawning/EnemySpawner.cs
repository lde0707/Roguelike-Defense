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
        [SerializeField] private float leftSpawnX = -950f;
        [SerializeField] private float rightSpawnX = 1100f;
        [SerializeField] private float spawnY = -5.6f;
        [SerializeField] private Vector2 spawnZRange = new(-400f, 500f);

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
                float z = Random.Range(spawnZRange.x, spawnZRange.y);
                GameObject enemyObject = Instantiate(enemyPrefab, new Vector3(x, spawnY, z), Quaternion.identity);
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
