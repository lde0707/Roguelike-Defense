using System;
using System.Collections.Generic;
using UnityEngine;
namespace WaveLab.Waves
{
    [Serializable] public sealed class BalanceEnemy
    {
        public EnemyProfile profile;
        public EnemyRole role;
        public float selectionWeight = 1;
        public string spawnKey = "main";
        public float health = 100;
        public float healthPerTier;
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
        public int wavesPerTier; // 0 = no tier scaling
        public float restSeconds = 5;
        public int difficulty = 1;
        public int maximumAlive = 8;
        public int maximumElites = 2;
        public int rhythm; // 0 steady, 1 alternating pressure/recovery, 2 build-up
        public bool clearBeforeRest = true;
        public float effectiveDpsOverride;
        public float pressureMargin = 1.2f;
        public float recoveryPercent;
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
        public float remainingHealth;
        public string note;
    }
    [Serializable] public sealed class BalanceResult
    {
        public WavePlan plan;
        public List<BalanceWaveReport> rows = new List<BalanceWaveReport>();
        public List<string> warnings = new List<string>();
    }
    public static class AutoBalance
    {
        public const string Assumptions = "기대값 초안: 입력한 조우 거리에서 서로 직선 접근하고 각자 사거리에 도달하면 멈춘다고 가정합니다. 접근 시간 + 체력/유효DPS를 처치 작업량으로, 적의 사거리 진입부터 처치까지를 피격 노출 시간으로 계산합니다. 명중률·사거리 내 공격 가동률은 사용자 가정입니다. 실제 경로·탄속·회피·다수 적 동시 교전·도탄·범위·탄 파괴는 시뮬레이션하지 않습니다. 성장은 웨이브 종료 후 보상을 전량 얻어 공격력을 강화하는 계산 전용 가정입니다. 누적 HP는 예상 피해와 입력한 회복 비율을 합산한 계산값이며 생존 확률은 아닙니다.";
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
        public static double WorkSeconds(BalanceInput x, BalanceEnemy e, double dps, int tier = 0) =>
            ReachTime(e.encounterDistance,x.playerRange,e.attackRange,x.playerMoveSpeed,e.moveSpeed)+(e.health+e.healthPerTier*tier)/dps;
        public static double IncomingDamage(BalanceInput x, BalanceEnemy e, double dps, int tier = 0)
        {
            double start=ReachTime(e.encounterDistance,e.attackRange,x.playerRange,e.moveSpeed,x.playerMoveSpeed);
            if(double.IsPositiveInfinity(start) || e.damage==0 || e.hitRate==0)return 0;
            return Math.Max(0,WorkSeconds(x,e,dps,tier)-start)*e.damage/e.attackInterval*e.hitRate;
        }
        private static bool InRange(float x, float min, float max) => WaveValidation.Finite(x) && x >= min && x <= max;
        public static List<string> Validate(BalanceInput x)
        {
            var e = new List<string>();
            if (x == null) { e.Add("설정이 없습니다."); return e; }
            if (!InRange(x.totalSeconds, 10, 7200)) e.Add("목표 시간은 10~7200초입니다.");
            if (x.wavesPerTier < 0 || x.wavesPerTier > 100) e.Add("스테이지당 웨이브 수 오류");
            if (x.waveCount < 0 || x.waveCount > 100) e.Add("웨이브 수는 자동(0) 또는 1~100입니다.");
            if (!InRange(x.restSeconds, 0, 120)) e.Add("휴식은 0~120초입니다.");
            if (x.difficulty < 0 || x.difficulty > 2) e.Add("난이도를 선택하세요.");
            if (x.maximumAlive < 1 || x.maximumAlive > 64 || x.maximumElites < 1 || x.maximumElites > x.maximumAlive) e.Add("동시 적은 1~64마리, 엘리트는 1~동시 적 상한으로 지정하세요.");
            if (x.rhythm < 0 || x.rhythm > 2 || !InRange(x.effectiveDpsOverride,0,10000000) ||
                !InRange(x.pressureMargin,1,3) || !InRange(x.recoveryPercent,0,1)) e.Add("전투 흐름·실측 DPS·안전 여유·예상 회복 값을 확인하세요.");
            if (!InRange(x.playerHealth, .01f, 10000000) || !InRange(x.playerDamage, .01f, 1000000) || !InRange(x.playerInterval, .01f, 1000)) e.Add("플레이어 체력·피해량·공격 간격을 확인하세요.");
            if (!InRange(x.hitRate, .01f, 1) || !InRange(x.attackUptime, .01f, 1)) e.Add("명중률·공격 가능 비율은 1~100%입니다.");
            if(!InRange(x.playerRange,0,100000)||!InRange(x.playerMoveSpeed,0,10000))e.Add("플레이어 사거리·이동속도를 확인하세요.");
            if (x.modelGrowth && (!InRange(x.firstLevelXp, 1, 10000000) || !InRange(x.xpRequirementStep, 0, 10) || !InRange(x.damagePerLevel, 0, 1) || !InRange(x.goldUpgradeCost, 1, 10000000) || !InRange(x.damagePerPurchase, 0, 1))) e.Add("성장 비용·증가율을 확인하세요.");
            if (x.enemies == null || x.enemies.Count == 0 || x.enemies.Count > 32) { e.Add("적을 1~32종 등록하세요."); return e; }
            foreach (var a in x.enemies)
            {
                if (a == null) { e.Add("빈 적 항목이 있습니다."); continue; }
                if (!InRange(a.healthPerTier,0,10000000)) e.Add("스테이지별 체력 증가 값 오류");
                if (a.profile == null || a.profile.prefab == null) e.Add("적 프로필과 프리팹이 필요합니다.");
                if (!Enum.IsDefined(typeof(EnemyRole),a.role) || !InRange(a.selectionWeight,.01f,100)) e.Add("적 역할 또는 등장 비중 오류");
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
            double baseDps = x.effectiveDpsOverride > 0 ? x.effectiveDpsOverride : x.playerDamage / x.playerInterval * x.hitRate * x.attackUptime;
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
            int level = 1, purchases = 0; double xp = 0, gold = 0, health = x.playerHealth;
            bool survivalWarning = false;
            for (int i = 0; i < n; i++)
            {
                int tier=x.wavesPerTier>0?i/x.wavesPerTier:0;
                double dps = baseDps * (1 + (x.modelGrowth ? (level - 1) * x.damagePerLevel + purchases * x.damagePerPurchase : 0));
                double ramp = n == 1 ? 1 : .8 + .4 * i / (n - 1.0);
                if (x.rhythm == 1 && i % 3 == 2) ramp *= .72;
                if (x.rhythm == 2 && n > 1) ramp *= .85 + .3 * i / (n - 1.0);
                double budget = dps * duration * new[] {.45,.6,.75}[x.difficulty] * ramp;
                double damageLimit = x.playerHealth * new[] {.12,.22,.35}[x.difficulty] * ramp;
                var counts = new int[x.enemies.Count]; double used = 0, damage = 0; int total = 0;
                while (total < Math.Min(10000, Math.Floor(duration * 100)))
                {
                    var candidates = new List<int>();
                    for (int j = 0; j < x.enemies.Count; j++)
                    {
                        var e = x.enemies[j]; double incoming = IncomingDamage(x,e,dps,tier) * x.pressureMargin;
                        // Elites remain an accent rather than replacing the normal roster.
                        if (e.role == EnemyRole.Elite && counts[j] >= Math.Max(1, x.maximumElites * (i + 1))) continue;
                        if (used + WorkSeconds(x,e,dps,tier)*dps <= budget && damage + incoming <= damageLimit) candidates.Add(j);
                    }
                    if (candidates.Count == 0) break;
                    double sum = 0; foreach (int c in candidates) sum += x.enemies[c].selectionWeight;
                    double pick = random.NextDouble() * sum; int index = candidates[candidates.Count - 1];
                    foreach (int c in candidates) { pick -= x.enemies[c].selectionWeight; if (pick <= 0) { index = c; break; } }
                    var a = x.enemies[index];
                    counts[index]++; total++; used += WorkSeconds(x,a,dps,tier)*dps; damage += IncomingDamage(x,a,dps,tier) * x.pressureMargin;
                }
                if (total == 0) throw new ArgumentException($"{i + 1}웨이브: 적 한 마리도 현재 화력·피해 예산에 맞지 않습니다. 약한 적을 추가하거나 플레이어 스탯·목표 시간을 조정하세요.");
                string mood = x.rhythm == 1 && i % 3 == 2 ? "숨 고르기" : x.rhythm == 2 ? "점층 전투" : "균형 전투";
                var w = new WaveDefinition { id = Id(random), title = (i + 1) + " · " + mood, end = x.clearBeforeRest ? WaveEnd.DurationAndCleared : WaveEnd.Duration, durationSeconds = (float)duration, restSeconds = i == n - 1 ? 0 : x.restSeconds };
                double gainedXp = 0, gainedGold = 0;
                for (int j = 0; j < counts.Length; j++) if (counts[j] > 0)
                {
                    var e = x.enemies[j];
                    // Leave time for the final enemy to be killed under the same DPS assumption.
                    double window = Math.Max(.01, duration - WorkSeconds(x,e,dps,tier));
                    double start = Math.Min(window * .6, j * 1.25 + (e.role == EnemyRole.Elite ? duration * .35 : 0));
                    w.groups.Add(new SpawnGroup { id = Id(random), enemy = e.profile, role=e.role, spawnKey = e.spawnKey, count = counts[j], batchSize = 1, startSeconds = (float)start, intervalSeconds = (float)Math.Max(.01, (window-start) / counts[j]) });
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
                health -= damage;
                if (health < x.playerHealth * .25 && !survivalWarning) { result.warnings.Add($"{i+1}웨이브부터 누적 예상 HP가 25% 미만입니다. 난이도를 낮추거나 실제 회복·빌드로 검증하세요."); survivalWarning=true; }
                result.rows.Add(new BalanceWaveReport { wave=i+1,count=total,startLevel=startLevel,endLevel=level,purchases=bought,seconds=(float)duration,effectiveDps=(float)dps,estimatedWorkSeconds=(float)(used/dps),estimatedDamage=(float)damage,xpEarned=(float)gainedXp,goldEarned=(float)gainedGold,goldRemaining=(float)gold,remainingHealth=(float)health,note=note });
                // A failed survival forecast cannot be revived by hypothetical recovery.
                if (health > 0 && i < n-1) health=Math.Min(x.playerHealth,health+x.playerHealth*x.recoveryPercent);
                waves.Add(w);
            }
            if (level == 100 || purchases == 100) result.warnings.Add("모델 안전 상한(레벨 100/구매 100)에 도달했습니다. 보상·비용을 재검토하세요.");
            if (x.modelGrowth) result.warnings.Add("성장 가정은 계산 전용입니다. 실제 게임에 같은 경험치 곡선과 강화 규칙을 연결해야 이후 웨이브 예상치가 맞습니다.");
            result.warnings.Add("시간은 계획상 기준입니다. 동시 적 제한과 잔여 적 정리로 실제 전투는 길어질 수 있습니다. 누적 HP는 기대값이며 회복·성장은 실제 게임에 자동 적용되지 않습니다.");
            result.warnings.Add($"동시 공격 안전 여유 {x.pressureMargin:0.##}배를 적용했습니다. 탄 파괴·광역·도탄·실제 다중 포탑 전투는 시뮬레이션하지 않습니다. 실측 유효 DPS로 보정하세요.");
            result.plan = ScriptableObject.CreateInstance<WavePlan>();result.plan.seed=x.seed;result.plan.waves=waves;
            result.plan.maximumAlive=x.maximumAlive;result.plan.maximumElites=x.maximumElites;result.plan.minimumSpawnSpacing=.75f;
            return result;
        }
        private static string Id(System.Random r) { var b=new byte[16];r.NextBytes(b);return new Guid(b).ToString("N"); }
    }
}
