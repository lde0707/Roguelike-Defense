using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace WaveLab.Waves
{
    [Serializable] public sealed class RewardEvent : UnityEvent<string, float> { }
    public sealed class WaveRunner : MonoBehaviour
    {
        public WavePlan plan;
        public SpawnArea[] spawnAreas = Array.Empty<SpawnArea>();
        [Min(0)] public int startWaveIndex;
        public bool playOnStart;
        [Tooltip("처치/반환 알림이 없을 때 안전 정지할 시간입니다. 정지 시 보상이나 완료 이벤트를 발생시키지 않습니다.")]
        [Min(1)] public float safetyTimeoutSeconds = 600;
        public UnityEvent<int> onWaveStarted = new UnityEvent<int>();
        public UnityEvent<int> onWaveCompleted = new UnityEvent<int>();
        public RewardEvent onReward = new RewardEvent();
        public UnityEvent onPlanCompleted = new UnityEvent();
        public event Action<GameObject, EnemyProfile> UnitSpawned;
        public bool IsRunning { get; private set; }
        public string Status { get; private set; } = "대기";
        public int CurrentWave { get; private set; } = -1;
        public int SpawnedCount { get; private set; }
        public int DefeatedCount { get; private set; }
        public int AliveCount => units.Count;
        private readonly HashSet<SpawnedUnit> units = new HashSet<SpawnedUnit>();
        private readonly HashSet<SpawnedUnit> elites = new HashSet<SpawnedUnit>();
        public int EliteCount => elites.Count;
        private Coroutine routine;
        private WavePlan snapshot;
        private readonly List<EnemyProfile> profiles = new List<EnemyProfile>();
        private void Start() { if (playOnStart) Play(); }
        private void OnDisable() { Stop(); }
        public List<string> ValidateSetup()
        {
            var errors = WaveValidation.Errors(plan);
            var keys = new HashSet<string>();
            if (spawnAreas == null) { errors.Add("스폰 영역을 연결하세요."); return errors; }
            foreach (var area in spawnAreas)
            {
                if (area == null) { errors.Add("빈 스폰 영역 연결"); continue; }
                if (!keys.Add(area.key)) errors.Add("스폰 키 중복: " + area.key);
                if (!area.Valid) errors.Add("스폰 영역 크기 오류: " + area.key);
            }
            if (!WaveValidation.Finite(safetyTimeoutSeconds) || safetyTimeoutSeconds <= 0) errors.Add("안전 정지 시간 오류");
            if (plan != null && plan.waves != null)
            {
                if (startWaveIndex < 0 || startWaveIndex >= plan.waves.Count) errors.Add("시작 웨이브 범위 오류");
                foreach (var w in plan.waves) if (w != null && w.groups != null) foreach (var g in w.groups)
                    if (g != null && !keys.Contains(g.spawnKey)) errors.Add("스폰 영역 연결 필요: " + g.spawnKey);
            }
            return errors;
        }
        [ContextMenu("Play Waves")]
        public void Play()
        {
            if (!Application.isPlaying || IsRunning) return;
            var errors = ValidateSetup();
            if (errors.Count > 0) { Status = string.Join("\n", errors); Debug.LogError(Status, this); return; }
            Stop();
            snapshot = Instantiate(plan);
            // Copy nested serialized data and reward definitions: authoring changes cannot alter a run.
            foreach (var w in snapshot.waves) foreach (var g in w.groups)
            { var copy = Instantiate(g.enemy); profiles.Add(copy); g.enemy = copy; }
            SpawnedCount = 0; DefeatedCount = 0; IsRunning = true;
            routine = StartCoroutine(Run());
        }
        [ContextMenu("Stop Waves")]
        public void Stop()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null; IsRunning = false;
            foreach (var unit in units) if (unit != null) { unit.Detach(); Destroy(unit.gameObject); }
            units.Clear();
            elites.Clear();
            if (snapshot != null) Destroy(snapshot);
            snapshot = null;
            foreach (var p in profiles) if (p != null) Destroy(p);
            profiles.Clear(); Status = "중지";
        }
        private IEnumerator Run()
        {
            var random = new System.Random(snapshot.seed);
            var areas = new Dictionary<string, SpawnArea>();
            foreach (var area in spawnAreas) areas.Add(area.key, area);
            for (int i = startWaveIndex; i < snapshot.waves.Count; i++)
            {
                CurrentWave = i; var wave = snapshot.waves[i];
                var emitted = new int[wave.groups.Count];
                var nextAt = new double[wave.groups.Count];
                for (int g=0;g<nextAt.Length;g++) nextAt[g]=wave.groups[g].startSeconds;
                double elapsed = 0; Status = $"웨이브 {i + 1}: {wave.title}";
                double expectedEnd = wave.durationSeconds;
                foreach(var group in wave.groups) expectedEnd=Math.Max(expectedEnd,group.LastSpawnSeconds);
                onWaveStarted.Invoke(i);
                if (!IsRunning) yield break;
                while (true)
                {
                    if (Time.timeScale <= 0) { yield return null; continue; }
                    int budget = 256; bool allSpawned = true;
                    for (int j = 0; j < wave.groups.Count; j++)
                    {
                        var g = wave.groups[j];
                        int batch = 0;
                        while (emitted[j] < g.count && budget > 0 && elapsed >= nextAt[j] && batch < g.batchSize)
                        {
                            if ((snapshot.maximumAlive > 0 && units.Count >= snapshot.maximumAlive) ||
                                (g.role == EnemyRole.Elite && snapshot.maximumElites > 0 && elites.Count >= snapshot.maximumElites)) break;
                            var area = areas[g.spawnKey];
                            if (area == null) { Stop(); Status = "오류: 실행 중 스폰 영역이 제거되었습니다."; yield break; }
                            if (!TryPosition(area,random,out var position)) break;
                            Spawn(g.enemy, position, g.role); emitted[j]++; budget--; batch++;
                            if (!IsRunning) yield break;
                        }
                        // Don't release an accumulated backlog in a single frame when a cap opens.
                        if (batch > 0) nextAt[j] = elapsed + g.intervalSeconds;
                        if (emitted[j] < g.count) allSpawned = false;
                    }
                    bool complete = wave.end == WaveEnd.DurationAndCleared ? elapsed >= wave.durationSeconds && allSpawned && units.Count == 0
                        : wave.end == WaveEnd.Duration ? elapsed >= wave.durationSeconds && allSpawned
                        : allSpawned && (wave.end == WaveEnd.AllSpawned || units.Count == 0);
                    if (complete) break;
                    if (elapsed >= expectedEnd + safetyTimeoutSeconds)
                    { Stop(); Status = "안전 정지: 제한 시간 초과. 처치/디스폰 연결과 적 수를 확인하세요."; Debug.LogWarning(Status, this); yield break; }
                    yield return null;
                    elapsed += Time.deltaTime;
                }
                onWaveCompleted.Invoke(i);
                if (!IsRunning) yield break;
                Status = "휴식";
                yield return new WaitForSeconds(wave.restSeconds);
            }
            // Earlier timed waves may still have living units. Keep tracking until resolved.
            double tail = 0; Status = "등장 종료 · 잔여 적 대기";
            while (units.Count > 0)
            {
                if (tail >= safetyTimeoutSeconds) { Stop(); Status = "안전 정지: 잔여 적 시간 초과"; yield break; }
                yield return null; tail += Time.deltaTime;
            }
            IsRunning = false; routine = null; Status = "플랜 완료"; onPlanCompleted.Invoke();
        }
        private bool TryPosition(SpawnArea area, System.Random random, out Vector3 position)
        {
            for(int attempt=0;attempt<16;attempt++)
            {
                position=area.Sample(random);
                if(area.avoid != null && Vector3.Distance(position,area.avoid.position)<area.avoidRadius)continue;
                bool occupied=false;
                foreach(var unit in units)
                    if(unit!=null && Vector3.Distance(unit.transform.position,position)<snapshot.minimumSpawnSpacing){occupied=true;break;}
                if(!occupied)return true;
            }
            position=default;return false;
        }
        private void Spawn(EnemyProfile profile, Vector3 position, EnemyRole role)
        {
            var obj = Instantiate(profile.prefab, position, profile.prefab.transform.rotation);
            var unit = obj.GetComponent<SpawnedUnit>() ?? obj.AddComponent<SpawnedUnit>();
            units.Add(unit); SpawnedCount++;
            if(role==EnemyRole.Elite)elites.Add(unit);
            unit.Initialize(defeated =>
            {
                elites.Remove(unit);
                if (!units.Remove(unit) || !defeated) return;
                DefeatedCount++;
                foreach (var reward in profile.defeatRewards) onReward.Invoke(reward.resourceId, reward.amount);
            });
            UnitSpawned?.Invoke(obj, profile);
        }
    }
}
