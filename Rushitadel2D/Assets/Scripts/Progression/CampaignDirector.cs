using System;
using System.Linq;
using UnityEngine;
using Rushitadel.Combat;
using Rushitadel.Player;
using Rushitadel.Power;
using Rushitadel.Presentation;
using Rushitadel.Spawning;
using Rushitadel.World;

namespace Rushitadel.Progression
{
    public enum RunPhase { Round, Boss, Maintenance, Theme, Complete, Defeat }
    [Serializable]
    public sealed class CampaignState
    {
        public RunPhase phase;
        public int spentXp, augmentsChosen, defeatedBosses, kills, usedThemes;
        public int[] augments = new int[15]; // T1..T5, damage / cadence / penetration
        public int[] turretForms = new int[5];
        public int transformationsChosen, element;
        public bool[] web = new bool[6];
        public int[] themeChoices = new int[2];
        public bool recovered;
    }
    public sealed class CampaignDirector : MonoBehaviour
    {
        public static CampaignDirector Instance { get; private set; }
        public CampaignState State { get; private set; }
        public int Stage { get; private set; } = 1;
        public int Round { get; private set; } = 1;
        public int Spawned { get; private set; }
        public int SpawnBudget => 10 + Stage * 2 + Round * 2;
        public int Wallet => Mathf.Max(0, Experience.TotalExperience - State.spentXp);
        public int PendingAugments => Mathf.Max(0, Experience.CompletedThresholds - State.augmentsChosen);
        public PlayerExperience Experience => PlayerHealth.Instance.GetComponent<PlayerExperience>();
        public CampaignBoss Boss { get; private set; }
        public bool Initialized { get; private set; }
        private EnemySpawner spawner;
        private float nextSpawn;
        public string SaveError { get; private set; }
        public static readonly string[] AugmentNames = {"강화 포신", "고속 장전", "관통 탄두"};
        public static readonly string[] AugmentEffects = {"포탄 피해 +25%", "발사 빈도 +20%", "관통 대상 +1"};
        public static readonly string[] WebNames = {"관통", "분산", "폭발", "독", "동력", "지원"};
        public static readonly string[] WebEffects = {"기본탄 관통 +1", "기본 공격 3방향 발사", "기본탄 폭발 반경 0.8 / 피해 50%", "기본탄 명중 시 3초간 초당 8 피해", "동력 자동 회복 +50%", "최대 HP +25 / 이동 속도 +10%"};
        public static readonly int[] WebCosts = {40,60,80,80,40,60};
        public static bool HasWeb(int id) => Instance != null && Instance.Initialized && Instance.State.web[id];
        public static int Rank(int slot, int kind) => Instance != null && Instance.Initialized ? Instance.State.augments[(Mathf.Clamp(slot,1,5)-1)*3+kind] : 0;
        public void Initialize(RunData data)
        {
            Instance = this; State = data.campaign ?? new CampaignState(); Stage = data.stage; Round = data.wave;
            if (State.turretForms == null || State.turretForms.Length != 5) State.turretForms = new int[5];
            State.usedThemes |= 1 << data.theme;
            spawner = FindFirstObjectByType<EnemySpawner>(); spawner.TakeCampaignControl();
            Initialized = true; ApplyBuild();
            PlayerHealth.Instance.Died += Defeat;
            ForwardPressureController.Instance.HoldAdvance = State.phase != RunPhase.Round;
            if (State.phase == RunPhase.Round) BeginRound();
            else if (State.phase == RunPhase.Boss) BeginBoss();
            else ReopenPage();
        }
        public void ApplyBuild()
        {
            PlayerHealth.Instance.SetMaximumHealth(HasWeb(5) ? 125 : 100);
            var art = PlayerHealth.Instance.GetComponent<Rushitadel.Presentation.ModularPlayerArt>();
            if (art == null) art = PlayerHealth.Instance.gameObject.AddComponent<Rushitadel.Presentation.ModularPlayerArt>();
            art.Refresh();
        }
        public static int Form(int slot) => Instance != null && Instance.Initialized ? Instance.State.turretForms[Mathf.Clamp(slot,1,5)-1] : 0;
        public bool CanTransform(int slot, int form)
        {
            if (slot < 1 || slot > 5 || form < 1 || form > 16 || State.turretForms[slot-1] == form) return false;
            for(int i=0;i<5;i++) if(i!=slot-1 && TurretForms.Support(form) && TurretForms.Support(State.turretForms[i])) return false;
            return true;
        }
        public int[] OfferedForms(int slot)
        {
            // Stable for this level-up: reopening the menu does not reroll the offer.
            var session=FindAnyObjectByType<RunSession>();int seed=session!=null?session.Seed:1;
            var random=new System.Random(unchecked(seed*397+State.augmentsChosen*31+slot));
            return Enumerable.Range(1,16).Where(f=>CanTransform(slot,f)).OrderBy(_=>random.Next()).Take(3).ToArray();
        }
        public bool ChooseTransformation(int slot,int form)
        {
            if(PendingAugments<=0 || !PlayerHealth.Instance.IsAlive || !CanTransform(slot,form))return false;
            State.turretForms[slot-1]=form; State.transformationsChosen++; State.augmentsChosen++; ApplyBuild(); return true;
        }
        public bool SetElement(int element)
        {
            if(State.phase!=RunPhase.Maintenance || !State.recovered || element<0 || element>=TurretForms.ElementNames.Length)return false;
            State.element=element; ApplyBuild(); SaveCheckpoint("안전 정비 · 속성 색상"); return true;
        }
        private void OnDestroy()
        {
            if (PlayerHealth.Instance != null) PlayerHealth.Instance.Died -= Defeat;
            if (Instance == this) Instance = null;
        }
        private void Update()
        {
            if (!Initialized || !PlayerHealth.Instance.IsAlive || State.phase == RunPhase.Defeat || State.phase == RunPhase.Complete) return;
            if (GameFrontend.Instance.CurrentPage == "game" && PendingAugments > 0)
            { GameFrontend.Instance.ShowAugments(); return; }
            if (Time.timeScale <= 0) return;
            if (State.phase == RunPhase.Round)
            {
                if (Spawned < SpawnBudget && Time.time >= nextSpawn && EnemyRegistry.ActiveEnemies.Count() < 8)
                {
                    var enemy = spawner.SpawnCampaignEnemy(Stage);
                    if (enemy != null) Spawned++;
                    nextSpawn = Time.time + Mathf.Max(1.1f,1.65f-.1f*Stage-.05f*Round);
                }
                if (Spawned >= SpawnBudget && !EnemyRegistry.ActiveEnemies.Any(e => e != null && e.IsAlive))
                {
                    if (Round == 3) BeginBoss(); else TryEnterMaintenance();
                }
            }
            else if (State.phase == RunPhase.Boss && (Boss == null || !Boss.Health.IsAlive)) TryEnterMaintenance();
        }
        private void BeginRound()
        {
            State.phase = RunPhase.Round; Spawned = 0; nextSpawn = Time.time + 1.5f;
            ForwardPressureController.Instance.HoldAdvance = false;
            GameFrontend.Instance.ShowBattle();
        }
        private void BeginBoss()
        {
            ClearProjectiles();
            State.phase = RunPhase.Boss; ForwardPressureController.Instance.HoldAdvance = true;
            var enemy = spawner.SpawnCampaignEnemy(Stage, true);
            Boss = enemy.GetComponent<CampaignBoss>();
        }
        private void TryEnterMaintenance()
        {
            ClearProjectiles();
            ForwardPressureController.Instance.HoldAdvance = true;
            // Let the last crystals finish their delayed attraction before freezing the safe area.
            if (FindObjectsByType<ExperiencePickup>(FindObjectsSortMode.None).Length > 0 || PendingAugments > 0) return;
            if (State.phase == RunPhase.Boss) State.defeatedBosses++;
            State.phase = RunPhase.Maintenance; State.recovered = false;
            var health = PlayerHealth.Instance;
            var pos = new Vector3(0, Camera.main.transform.position.y - 1, 0);
            health.transform.position = pos; health.GetComponent<Rigidbody2D>().position = pos;
            SaveCheckpoint("라운드 종료 · 안전 정비");
            GameFrontend.Instance.ShowMaintenance();
        }
        public static void ClearProjectiles()
        {
            foreach (var shot in FindObjectsByType<ProjectileBehaviour>(FindObjectsSortMode.None))
            { shot.gameObject.SetActive(false); Destroy(shot.gameObject); }
        }
        public bool ChooseAugment(int slot, int kind)
        {
            if (PendingAugments <= 0 || slot < 1 || slot > 5 || kind < 0 || kind > 2 || !PlayerHealth.Instance.IsAlive) return false;
            if(!CanChooseStat(slot,kind))return false;
            State.augments[(slot-1)*3+kind]++; State.augmentsChosen++;
            return true;
        }
        public bool CanChooseStat(int slot,int kind)
        {
            int f=Form(slot);
            return !(kind==2&&f>0 || kind==0&&TurretForms.Support(f) || kind==1&&f==6);
        }
        public bool CanBuy(int id, out string reason)
        {
            reason = "구매 가능";
            if (id < 0 || id >= 6 || State.phase != RunPhase.Maintenance) { reason = "정비에서만 구매 가능"; return false; }
            if (State.web[id]) { reason = "획득 완료"; return false; }
            if (!State.recovered) { reason = "회복 선택 먼저"; return false; }
            if (id != 0 && id != 4 && id != 5 && !State.web[id-1]) { reason = WebNames[id-1] + " 연결 필요"; return false; }
            if (Wallet < WebCosts[id]) { reason = "XP 부족"; return false; }
            return true;
        }
        public bool BuyWeb(int id)
        {
            if (!CanBuy(id, out _)) return false;
            State.spentXp += WebCosts[id]; State.web[id] = true; ApplyBuild();
            SaveCheckpoint("안전 정비 · Web 구매"); return true;
        }
        public void Recover(int type)
        {
            if (State.phase != RunPhase.Maintenance || State.recovered) return;
            if (type == 0) PlayerHealth.Instance.Heal(PlayerHealth.Instance.MaximumHealth * .3f);
            if (type == 1) { var power = PlayerHealth.Instance.GetComponent<PlayerPower>(); power.RestorePower(power.CurrentPower + 50, false); }
            State.recovered = true; SaveCheckpoint("안전 정비 · 회복 확정");
        }
        public void Depart()
        {
            if (State.phase != RunPhase.Maintenance || !State.recovered || PendingAugments > 0) return;
            if (Round < 3) { Round++; State.phase = RunPhase.Round; SaveCheckpoint("다음 라운드 시작점"); BeginRound(); }
            else if (Stage < 3)
            {
                State.phase = RunPhase.Theme;
                var available = Enumerable.Range(0,5).Where(i => (State.usedThemes & (1 << i)) == 0).ToList();
                var random = new System.Random(Stage * 137 + RunSession.Active.Seed);
                State.themeChoices[0] = available[random.Next(available.Count)]; available.Remove(State.themeChoices[0]);
                State.themeChoices[1] = available[random.Next(available.Count)];
                SaveCheckpoint("다음 스테이지 테마 선택"); GameFrontend.Instance.ShowNextTheme();
            }
            else { State.phase = RunPhase.Complete; SaveCheckpoint("3스테이지 완료"); GameFrontend.Instance.ShowRunResult(true); }
        }
        public void SelectTheme(int theme)
        {
            if (State.phase != RunPhase.Theme || !State.themeChoices.Contains(theme) || (State.usedThemes & (1 << theme)) != 0) return;
            Stage++; Round = 1; State.usedThemes |= 1 << theme;
            ForwardPressureController.Instance.SetTheme((PressureTheme)theme);
            State.phase = RunPhase.Round; SaveCheckpoint("스테이지 시작점"); BeginRound();
        }
        public bool SaveCheckpoint(string label)
        {
            bool ok = RunSession.Active.SaveSafeCheckpoint(label, out string error);
            SaveError = ok ? null : error; return ok;
        }
        public void ReopenPage()
        {
            if (State.phase == RunPhase.Complete || State.phase == RunPhase.Defeat) GameFrontend.Instance.ShowRunResult(State.phase == RunPhase.Complete);
            else if (PendingAugments > 0) GameFrontend.Instance.ShowAugments();
            else if (State.phase == RunPhase.Maintenance) GameFrontend.Instance.ShowMaintenance();
            else if (State.phase == RunPhase.Theme) GameFrontend.Instance.ShowNextTheme();
            else GameFrontend.Instance.ShowBattle();
        }
        private void Defeat() { State.phase = RunPhase.Defeat; GameFrontend.Instance.ShowRunResult(false); }
    }
}
