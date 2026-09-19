using UnityEngine;

namespace Rushitadel.Data
{
    [CreateAssetMenu(menuName = "Rushitadel 2D/Spawning/WaveDefinition", fileName = "WaveDefinition")]
    public sealed class WaveDefinition : GameplayDefinition
    {
        [SerializeField, Min(0.01f)] private float spawnInterval = 1f;
        [SerializeField, Min(1)] private int enemiesPerWave = 2;
        [SerializeField] private float leftSpawnX = -9.5f;
        [SerializeField] private float rightSpawnX = 11f;
        [SerializeField] private float spawnDepth = 0f;
        [SerializeField] private Vector2 spawnYRange = new(-4f, 5f);

        public float SpawnInterval => spawnInterval;
        public int EnemiesPerWave => enemiesPerWave;
        public float LeftSpawnX => leftSpawnX;
        public float RightSpawnX => rightSpawnX;
        public float SpawnDepth => spawnDepth;
        public Vector2 SpawnYRange => spawnYRange;
        protected override void ValidateValues()
        {
            spawnInterval = Mathf.Max(0.01f, spawnInterval);
            enemiesPerWave = Mathf.Max(1, enemiesPerWave);
            if (spawnYRange.x > spawnYRange.y) spawnYRange = new Vector2(spawnYRange.y, spawnYRange.x);
        }
    }
}