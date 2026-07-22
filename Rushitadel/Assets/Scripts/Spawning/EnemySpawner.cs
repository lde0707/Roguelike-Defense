using System.Collections;
using Rushitadel.Combat;
using UnityEngine;

namespace Rushitadel.Spawning
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField, Min(0.01f)] private float spawnInterval = 1f;
        [SerializeField, Min(1)] private int enemiesPerWave = 2;
        [Header("Spawn Position")]
        [SerializeField] private float leftSpawnX = -950f;
        [SerializeField] private float rightSpawnX = 1100f;
        [SerializeField] private float spawnY = -5.6f;
        [SerializeField] private Vector2 spawnZRange = new(-400f, 500f);

        public void Configure(GameObject prefab) => enemyPrefab = prefab;

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

            for (int index = 0; index < enemiesPerWave; index++)
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
    }
}
