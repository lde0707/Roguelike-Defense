using System;
using System.Collections.Generic;
using UnityEngine;
namespace WaveLab.Waves
{
    public enum WaveEnd { AllDefeated, AllSpawned, Duration }
    [Serializable] public sealed class SpawnGroup
    {
        public string id = Guid.NewGuid().ToString("N");
        public EnemyProfile enemy;
        public string spawnKey = "main";
        [Min(1)] public int count = 10;
        [Min(1)] public int batchSize = 1;
        [Min(0)] public float startSeconds;
        [Min(0.01f)] public float intervalSeconds = 1;
        public double LastSpawnSeconds => startSeconds + ((Math.Max(1, count) - 1) / Math.Max(1, batchSize)) * (double)intervalSeconds;
    }
    [Serializable] public sealed class WaveDefinition
    {
        public string id = Guid.NewGuid().ToString("N");
        public string title = "New Wave";
        public WaveEnd end = WaveEnd.AllDefeated;
        [Min(0.01f)] public float durationSeconds = 30;
        [Min(0)] public float restSeconds = 3;
        public List<SpawnGroup> groups = new List<SpawnGroup>();
    }
    [CreateAssetMenu(menuName = "WaveLab/Wave Plan")]
    public sealed class WavePlan : ScriptableObject
    {
        public int schemaVersion = 1;
        public int seed = 1;
        public List<WaveDefinition> waves = new List<WaveDefinition>();
    }
    public static class WaveValidation
    {
        public static List<string> Errors(WavePlan plan)
        {
            var errors = new List<string>();
            if (plan == null) { errors.Add("웨이브 플랜을 선택하세요."); return errors; }
            if (plan.schemaVersion != 1) errors.Add("지원하지 않는 데이터 버전입니다.");
            if (plan.waves == null || plan.waves.Count == 0) { errors.Add("웨이브를 추가하세요."); return errors; }
            var ids = new HashSet<string>();
            for (int i = 0; i < plan.waves.Count; i++)
            {
                var w = plan.waves[i]; string p = $"웨이브 {i + 1}";
                if (w == null) { errors.Add(p + ": 데이터 없음"); continue; }
                if (string.IsNullOrWhiteSpace(w.id) || !ids.Add(w.id)) errors.Add(p + ": ID 누락/중복");
                if (!Finite(w.restSeconds) || w.restSeconds < 0) errors.Add(p + ": 휴식 시간 오류");
                if (w.end == WaveEnd.Duration && (!Finite(w.durationSeconds) || w.durationSeconds <= 0)) errors.Add(p + ": 제한 시간 오류");
                if (w.groups == null || w.groups.Count == 0) { errors.Add(p + ": 적 그룹을 추가하세요."); continue; }
                foreach (var g in w.groups)
                {
                    if (g == null) { errors.Add(p + ": 빈 그룹"); continue; }
                    if (string.IsNullOrWhiteSpace(g.id) || !ids.Add(g.id)) errors.Add(p + ": 그룹 ID 누락/중복");
                    if (g.enemy == null || g.enemy.prefab == null) errors.Add(p + ": 적 프로필/프리팹 필요");
                    if (string.IsNullOrWhiteSpace(g.spawnKey)) errors.Add(p + ": 스폰 키 필요");
                    if (g.count < 1 || g.count > 10000 || g.batchSize < 1 || g.batchSize > 10000) errors.Add(p + ": 수량은 1~10000");
                    if (!Finite(g.startSeconds) || g.startSeconds < 0 || !Finite(g.intervalSeconds) || g.intervalSeconds <= 0) errors.Add(p + ": 등장 시간 오류");
                    if (w.end == WaveEnd.Duration && g.LastSpawnSeconds >= w.durationSeconds) errors.Add(p + ": 마지막 등장보다 종료 시간을 늦게 지정하세요.");
                    if (g.enemy != null) foreach (var r in g.enemy.defeatRewards)
                        if (r == null || string.IsNullOrWhiteSpace(r.resourceId) || !Finite(r.amount) || r.amount < 0) errors.Add(p + ": 보상 값 오류");
                }
            }
            return errors;
        }
        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
