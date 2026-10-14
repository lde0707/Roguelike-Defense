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
        public float attackRange = 3;
        public float moveSpeed = 1;
        public float encounterDistance = 6;
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
        public float playerRange = 3;
        public float playerMoveSpeed = 2;
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
        public const string Assumptions = "기대값 초안: 입력한 조우 거리에서 서로 직선 접근하고 각자 사거리에 도달하면 멈춘다고 가정합니다. 접근 시간 + 체력/유효DPS를 처치 작업량으로, 적의 사거리 진입부터 처치까지를 피격 노출 시간으로 계산합니다. 명중률·사거리 내 공격 가동률은 사용자 가정입니다. 실제 경로·탄속·회피·다수 적 동시 교전·도탄·범위·탄 파괴는 시뮬레이션하지 않습니다. 성장은 웨이브 종료 후 보상을 전량 얻어 공격력을 강화하는 계산 전용 가정입니다. 누적 생존/회복은 계산하지 않습니다.";
        public static double ReachTime(double distance, double ownRange, double otherRange, double ownSpeed, double otherSpeed)
        {
            if(distance<=ownRange)return 0;
            double sharedGap=Math.Max(0,distance-Math.Max(ownRange,otherRange));
            double sharedSpeed=ownSpeed+otherSpeed;
            if(sharedGap>0 && sharedSpeed<=0)return double.PositiveInfinity;
            double soloGap=Math.Max(0,Math.Min(distance,otherRange)-ownRange);
            if(soloGap>0 && ownSpeed<=0)return double.PositiveInfinity;
            return (sharedGap>0?sharedGap/sharedSpeed:0)+(soloGap>0?soloGap/ownSpeed:0);
        }
        public static double WorkSeconds(BalanceInput x, BalanceEnemy e, double dps) =>
            ReachTime(e.encounterDistance,x.playerRange,e.attackRange,x.playerMoveSpeed,e.moveSpeed)+e.health/dps;
        public static double IncomingDamage(BalanceInput x, BalanceEnemy e, double dps)
        {
            double start=ReachTime(e.encounterDistance,e.attackRange,x.playerRange,e.moveSpeed,x.playerMoveSpeed);
            if(double.IsPositiveInfinity(start) || e.damage==0 || e.hitRate==0)return 0;
            return Math.Max(0,WorkSeconds(x,e,dps)-start)*e.damage/e.attackInterval*e.hitRate;
        }
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
            if(!InRange(x.playerRange,0,100000)||!InRange(x.playerMoveSpeed,0,10000))e.Add("플레이어 사거리·이동속도를 확인하세요.");
            if (x.modelGrowth && (!InRange(x.firstLevelXp, 1, 10000000) || !InRange(x.xpRequirementStep, 0, 10) || !InRange(x.damagePerLevel, 0, 1) || !InRange(x.goldUpgradeCost, 1, 10000000) || !InRange(x.damagePerPurchase, 0, 1))) e.Add("성장 비용·증가율을 확인하세요.");
            if (x.enemies == null || x.enemies.Count == 0 || x.enemies.Count > 32) { e.Add("적을 1~32종 등록하세요."); return e; }
            foreach (var a in x.enemies)
            {
                if (a == null) { e.Add("빈 적 항목이 있습니다."); continue; }
                if (a.profile == null || a.profile.prefab == null) e.Add("적 프로필과 프리팹이 필요합니다.");
                if (string.IsNullOrWhiteSpace(a.spawnKey)) e.Add("스폰 영역 이름이 필요합니다.");
                if (!InRange(a.health,.01f,10000000) || !InRange(a.damage,0,1000000) || !InRange(a.attackInterval,.01f,1000) || !InRange(a.hitRate,0,1)) e.Add("적 체력·피해량·공격 간격·명중률 오류");
                if(!InRange(a.attackRange,0,100000)||!InRange(a.moveSpeed,0,10000)||!InRange(a.encounterDistance,0,100000))e.Add("적 사거리·이동속도·조우 거리 오류");
                if (a.profile != null) foreach (var r in a.profile.defeatRewards)
                    if (r == null || string.IsNullOrWhiteSpace(r.resourceId) || !InRange(r.amount,0,1000000)) e.Add("적 보상 값 오류");
            }
            return e;
        }
        public static BalanceResult Generate(BalanceInput x)
        {
            var errors = Validate(x); if (errors.Count > 0) throw new ArgumentException(string.Join("\n", errors));
            double baseDps = x.playerDamage / x.playerInterval * x.hitRate * x.attackUptime;
            double meanWork = 0; int reachable=0;
            foreach (var e in x.enemies) { double work=WorkSeconds(x,e,baseDps); if(!double.IsInfinity(work)){meanWork+=work;reachable++;} }
            if(reachable==0)throw new ArgumentException("모든 적이 플레이어 사거리 밖이며 접근할 수 없습니다. 사거리·이동속도·조우 거리를 확인하세요.");
            double preferred = Math.Max(20, Math.Min(90, 30 + 2 * meanWork/reachable));
            int n = x.waveCount == 0 ? Math.Max(1, Math.Min(100, (int)Math.Round((x.totalSeconds + x.restSeconds) / (preferred + x.restSeconds)))) : x.waveCount;
            double duration = (x.totalSeconds - (n - 1) * x.restSeconds) / n;
            if (duration < 5) throw new ArgumentException("휴식에 비해 웨이브 수가 많습니다. 웨이브 수나 휴식을 줄이세요. 전투 시간은 웨이브당 5초 이상이어야 합니다.");
            var result = new BalanceResult();
            if(reachable<x.enemies.Count)result.warnings.Add("사거리/이동속도상 교전 불가능한 적은 편성에서 제외했습니다.");
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
                        var e = x.enemies[j]; double incoming = IncomingDamage(x,e,dps);
                        if (used + WorkSeconds(x,e,dps)*dps <= budget && damage + incoming <= damageLimit) candidates.Add(j);
                    }
                    if (candidates.Count == 0) break;
                    int index = candidates[random.Next(candidates.Count)]; var a = x.enemies[index];
                    counts[index]++; total++; used += WorkSeconds(x,a,dps)*dps; damage += IncomingDamage(x,a,dps);
                }
                if (total == 0) throw new ArgumentException($"{i + 1}웨이브: 적 한 마리도 현재 화력·피해 예산에 맞지 않습니다. 약한 적을 추가하거나 플레이어 스탯·목표 시간을 조정하세요.");
                var w = new WaveDefinition { id = Id(random), title = "자동 웨이브 " + (i + 1), end = WaveEnd.Duration, durationSeconds = (float)duration, restSeconds = i == n - 1 ? 0 : x.restSeconds };
                double gainedXp = 0, gainedGold = 0;
                for (int j = 0; j < counts.Length; j++) if (counts[j] > 0)
                {
                    var e = x.enemies[j];
                    // Leave time for the final enemy to be killed under the same DPS assumption.
                    double window = Math.Max(.01, duration - WorkSeconds(x,e,dps));
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
