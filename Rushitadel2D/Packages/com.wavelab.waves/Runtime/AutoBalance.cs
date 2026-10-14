using System;
using System.Collections.Generic;
using UnityEngine;
namespace WaveLab.Waves
{
    [Serializable] public sealed class BalanceEnemy
    {
        public EnemyProfile profile;
        public string spawnKey = "main";
        public float health = 100;
        public float damage = 5;
        public float attackInterval = 2;
        public float hitRate = .2f;
    }
    [Serializable] public sealed class BalanceInput
    {
        public int seed = 1;
        public float totalSeconds = 300;
        public int waveCount; // 0 = auto
        public float restSeconds = 5;
        public int difficulty = 1;
        public float playerHealth = 100;
        public float playerDamage = 20;
        public float playerInterval = .5f;
        public float hitRate = .8f;
        public float attackUptime = .75f;
        public bool modelGrowth = true;
        public float firstLevelXp = 100;
        public float xpRequirementStep = .25f;
        public float damagePerLevel = .1f;
        public float goldUpgradeCost = 100;
        public float damagePerPurchase = .1f;
        public List<BalanceEnemy> enemies = new List<BalanceEnemy>();
    }
    [Serializable] public sealed class BalanceWaveReport
    {
        public int wave, count, startLevel, endLevel, purchases;
        public float seconds, effectiveDps, estimatedWorkSeconds, estimatedDamage, xpEarned, goldEarned, goldRemaining;
        public string note;
    }
    public sealed class BalanceResult
    {
        public WavePlan plan;
        public List<BalanceWaveReport> rows = new List<BalanceWaveReport>();
        public List<string> warnings = new List<string>();
    }
    public static class AutoBalance
    {
        public const string Assumptions = "기대값 초안: 명중률·공격 가능 시간은 입력한 가정입니다. 이동·사거리·회전·도탄·범위·탄 파괴·실제 회피는 계산하지 않습니다. 피해량은 각 적이 처치까지 계속 공격한다고 가정한 합계이며 생존율이 아닙니다. 성장은 웨이브 종료 시 경험치/골드 전량 획득 후 공격력 증가만 모델링합니다. 실제 게임의 증강·구매를 실행하거나 보상 값을 바꾸지 않습니다. 휴식 회복과 누적 잔여 체력은 모델링하지 않습니다. 실제 시간은 처치 속도와 잔여 적에 따라 달라집니다.";
        private static bool InRange(float x, float min, float max) => WaveValidation.Finite(x) && x >= min && x <= max;
        public static List<string> Validate(BalanceInput x)
        {
            var e = new List<string>();
            if (x == null) { e.Add("설정이 없습니다."); return e; }
            if (!InRange(x.totalSeconds, 10, 7200)) e.Add("목표 시간은 10~7200초입니다.");
            if (x.waveCount < 0 || x.waveCount > 100) e.Add("웨이브 수는 자동(0) 또는 1~100입니다.");
            if (!InRange(x.restSeconds, 0, 120)) e.Add("휴식은 0~120초입니다.");
            if (x.difficulty < 0 || x.difficulty > 2) e.Add("난이도를 선택하세요.");
            if (!InRange(x.playerHealth, .01f, 10000000) || !InRange(x.playerDamage, .01f, 1000000) || !InRange(x.playerInterval, .01f, 1000)) e.Add("플레이어 체력·피해량·공격 간격을 확인하세요.");
            if (!InRange(x.hitRate, .01f, 1) || !InRange(x.attackUptime, .01f, 1)) e.Add("명중률·공격 가능 비율은 1~100%입니다.");
            if (x.modelGrowth && (!InRange(x.firstLevelXp, 1, 10000000) || !InRange(x.xpRequirementStep, 0, 10) || !InRange(x.damagePerLevel, 0, 1) || !InRange(x.goldUpgradeCost, 1, 10000000) || !InRange(x.damagePerPurchase, 0, 1))) e.Add("성장 비용·증가율을 확인하세요.");
            if (x.enemies == null || x.enemies.Count == 0 || x.enemies.Count > 32) { e.Add("적을 1~32종 등록하세요."); return e; }
            foreach (var a in x.enemies)
            {
                if (a == null) { e.Add("빈 적 항목이 있습니다."); continue; }
                if (a.profile == null || a.profile.prefab == null) e.Add("적 프로필과 프리팹이 필요합니다.");
                if (string.IsNullOrWhiteSpace(a.spawnKey)) e.Add("스폰 영역 이름이 필요합니다.");
                if (!InRange(a.health,.01f,10000000) || !InRange(a.damage,0,1000000) || !InRange(a.attackInterval,.01f,1000) || !InRange(a.hitRate,0,1)) e.Add("적 체력·피해량·공격 간격·명중률 오류");
                if (a.profile != null) foreach (var r in a.profile.defeatRewards)
                    if (r == null || string.IsNullOrWhiteSpace(r.resourceId) || !InRange(r.amount,0,1000000)) e.Add("적 보상 값 오류");
            }
            return e;
        }
        public static BalanceResult Generate(BalanceInput x)
        {
            var errors = Validate(x); if (errors.Count > 0) throw new ArgumentException(string.Join("\n", errors));
            double baseDps = x.playerDamage / x.playerInterval * x.hitRate * x.attackUptime;
            double meanHp = 0; foreach (var e in x.enemies) meanHp += e.health / x.enemies.Count;
            double preferred = Math.Max(20, Math.Min(90, 30 + 2 * meanHp / baseDps));
            int n = x.waveCount == 0 ? Math.Max(1, Math.Min(100, (int)Math.Round((x.totalSeconds + x.restSeconds) / (preferred + x.restSeconds)))) : x.waveCount;
            double duration = (x.totalSeconds - (n - 1) * x.restSeconds) / n;
            if (duration < 5) throw new ArgumentException("휴식에 비해 웨이브 수가 많습니다. 웨이브 수나 휴식을 줄이세요. 전투 시간은 웨이브당 5초 이상이어야 합니다.");
            var result = new BalanceResult();
            var waves = new List<WaveDefinition>();
            var random = new System.Random(x.seed);
            int level = 1, purchases = 0; double xp = 0, gold = 0;
            for (int i = 0; i < n; i++)
            {
                double dps = baseDps * (1 + (x.modelGrowth ? (level - 1) * x.damagePerLevel + purchases * x.damagePerPurchase : 0));
                double ramp = n == 1 ? 1 : .8 + .4 * i / (n - 1.0);
                double budget = dps * duration * new[] {.45,.6,.75}[x.difficulty] * ramp;
                double damageLimit = x.playerHealth * new[] {.12,.22,.35}[x.difficulty] * ramp;
                var counts = new int[x.enemies.Count]; double used = 0, damage = 0; int total = 0;
                while (total < Math.Min(10000, Math.Floor(duration * 100)))
                {
                    var candidates = new List<int>();
                    for (int j = 0; j < x.enemies.Count; j++)
                    {
                        var e = x.enemies[j]; double incoming = e.damage / e.attackInterval * e.hitRate * e.health / dps;
                        if (used + e.health <= budget && damage + incoming <= damageLimit) candidates.Add(j);
                    }
                    if (candidates.Count == 0) break;
                    int index = candidates[random.Next(candidates.Count)]; var a = x.enemies[index];
                    counts[index]++; total++; used += a.health; damage += a.damage / a.attackInterval * a.hitRate * a.health / dps;
                }
                if (total == 0) throw new ArgumentException($"{i + 1}웨이브: 적 한 마리도 현재 화력·피해 예산에 맞지 않습니다. 약한 적을 추가하거나 플레이어 스탯·목표 시간을 조정하세요.");
                var w = new WaveDefinition { id = Id(random), title = "자동 웨이브 " + (i + 1), end = WaveEnd.Duration, durationSeconds = (float)duration, restSeconds = i == n - 1 ? 0 : x.restSeconds };
                double gainedXp = 0, gainedGold = 0;
                for (int j = 0; j < counts.Length; j++) if (counts[j] > 0)
                {
                    var e = x.enemies[j];
                    // Leave time for the final enemy to be killed under the same DPS assumption.
                    double window = Math.Max(.01, duration - e.health / dps);
                    w.groups.Add(new SpawnGroup { id = Id(random), enemy = e.profile, spawnKey = e.spawnKey, count = counts[j], batchSize = 1, startSeconds = 0, intervalSeconds = (float)Math.Max(.01, window / counts[j]) });
                    foreach (var reward in e.profile.defeatRewards)
                    { if (reward.resourceId == "xp") gainedXp += reward.amount * counts[j]; if (reward.resourceId == "gold") gainedGold += reward.amount * counts[j]; }
                }
                int startLevel = level, bought = 0; xp += gainedXp; gold += gainedGold;
                if (x.modelGrowth)
                {
                    while (level < 100 && xp >= x.firstLevelXp * (1 + x.xpRequirementStep * (level - 1)))
                    { xp -= x.firstLevelXp * (1 + x.xpRequirementStep * (level - 1)); level++; }
                    bought = (int)Math.Min(100 - purchases, Math.Floor(gold / x.goldUpgradeCost)); purchases += bought; gold -= bought * x.goldUpgradeCost;
                }
                string note = used < budget * .65 ? "목표 화력 예산 미달: 적 피해 또는 적 1마리의 크기가 제한 요인" : "기대값 예산 내";
                result.rows.Add(new BalanceWaveReport { wave=i+1,count=total,startLevel=startLevel,endLevel=level,purchases=bought,seconds=(float)duration,effectiveDps=(float)dps,estimatedWorkSeconds=(float)(used/dps),estimatedDamage=(float)damage,xpEarned=(float)gainedXp,goldEarned=(float)gainedGold,goldRemaining=(float)gold,note=note });
                waves.Add(w);
            }
            if (level == 100 || purchases == 100) result.warnings.Add("모델 안전 상한(레벨 100/구매 100)에 도달했습니다. 보상·비용을 재검토하세요.");
            if (x.modelGrowth) result.warnings.Add("성장 가정은 계산 전용입니다. 실제 게임에 같은 경험치 곡선과 강화 규칙을 연결해야 이후 웨이브 예상치가 맞습니다.");
            result.warnings.Add("예상 피해를 웨이브마다 따로 제한합니다. 누적 생존·빌드 선택·다수 적 동시 공격은 검증하지 않았습니다.");
            result.plan = ScriptableObject.CreateInstance<WavePlan>();result.plan.seed=x.seed;result.plan.waves=waves;
            return result;
        }
        private static string Id(System.Random r) { var b=new byte[16];r.NextBytes(b);return new Guid(b).ToString("N"); }
    }
}
