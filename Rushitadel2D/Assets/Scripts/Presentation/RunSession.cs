using System;
using System.Linq;
using UnityEngine;
using Rushitadel.Player;
using Rushitadel.Power;
using Rushitadel.Progression;
using Rushitadel.World;
using Rushitadel.Combat;

namespace Rushitadel.Presentation
{
    [DefaultExecutionOrder(-150)]
    public sealed class RunSession : MonoBehaviour
    {
        public static RunData Pending;
        public static RunSession Active { get; private set; }
        public static string SaveSummary => Active == null || Active.lastSaved == null || Active.lastSaved.slot == 0 ? "저장 슬롯 없는 테스트 전투" :
            $"슬롯 {Active.lastSaved.slot} · {Active.lastSaved.checkpoint}\n최근 저장 {DateTime.Parse(Active.lastSaved.savedUtc).ToLocalTime():HH:mm:ss} · 전투 중 나가면 이 지점에서 재개";
        private RunData lastSaved;
        private float elapsed;
        private float nextSafeCheck = 30f;
        public string SaveError { get; private set; }
        public int Seed => lastSaved != null ? lastSaved.seed : 1;
        public static void Reset() { Pending = null; Active = null; }
        public static void Leave() { Pending = null; Active = null; }
        private void Start()
        {
            Active = this;
            var data = Pending; Pending = null;
            if (data == null) data = new RunData { seed = 1, runId = Guid.NewGuid().ToString() };
            var health = PlayerHealth.Instance;
            if (health == null) return;
            lastSaved = data; elapsed = data.seconds;
            UnityEngine.Random.InitState(data.seed);
            health.SetMaximumHealth(data.campaign != null && data.campaign.web[5] ? 125 : 100);
            health.RestoreHealth(data.health);
            health.GetComponent<PlayerPower>().RestorePower(data.power, data.recovering);
            health.GetComponent<PlayerExperience>().RestoreExperience(data.experience);
            var body = health.GetComponent<Rigidbody2D>(); body.position = new Vector2(data.playerX, data.playerY); body.linearVelocity = Vector2.zero;
            health.transform.position = new Vector3(data.playerX, data.playerY, 0);
            var rig = health.GetComponent<PlayerTurretRig>();
            if (rig.RotatingBody != null) rig.RotatingBody.localRotation = Quaternion.Euler(0, 0, data.turretAngle);
            var pressure = ForwardPressureController.Instance;
            pressure.RestoreProgress(data.cameraY, data.distance, (PressureTheme)data.theme);
            FindFirstObjectByType<LoopingBattlefield>()?.RefreshLayout();
            Physics2D.SyncTransforms();
            gameObject.AddComponent<CampaignDirector>().Initialize(data);
        }
        private void Update()
        {
            if (Time.timeScale > 0 && PlayerHealth.Instance != null && PlayerHealth.Instance.IsAlive) elapsed += Time.deltaTime;
            if (CampaignDirector.Instance != null) return; // Only explicit round/maintenance boundaries save a campaign.
            if (lastSaved == null || elapsed < nextSafeCheck || Time.timeScale <= 0) return;
            nextSafeCheck = elapsed + 5f;
            if (EnemyRegistry.ActiveEnemies.Any(e => e != null && e.IsAlive) ||
                FindObjectsByType<EnemyProjectileController>(FindObjectsSortMode.None).Length > 0 ||
                FindObjectsByType<ExperiencePickup>(FindObjectsSortMode.None).Length > 0) return;
            if (!SaveSafeCheckpoint("적·적 탄 정리 후 안전 구간", out var error)) SaveError = error;
            else { SaveError = null; nextSafeCheck = elapsed + 30f; }
        }
        // Explicit campaign checkpoint boundary. Never called merely on pause or quit.
        public bool SaveSafeCheckpoint(string name, out string error)
        {
            error = null;
            if (lastSaved == null) { error = "저장 슬롯이 없는 테스트 전투입니다."; return false; }
            var health = PlayerHealth.Instance;
            if (health == null || !health.IsAlive || EnemyRegistry.ActiveEnemies.Any(e => e != null && e.IsAlive) ||
                FindObjectsByType<EnemyProjectileController>(FindObjectsSortMode.None).Length > 0)
            { error = "적 또는 적 탄이 남아 있어 안전 저장할 수 없습니다."; return false; }
            var data = JsonUtility.FromJson<RunData>(JsonUtility.ToJson(lastSaved));
            var pressure = ForwardPressureController.Instance;
            data.health = health.CurrentHealth; data.power = health.GetComponent<PlayerPower>().CurrentPower;
            data.recovering = health.GetComponent<PlayerPower>().State == PowerCycleState.Recovering;
            data.experience = health.GetComponent<PlayerExperience>().TotalExperience;
            data.playerX = health.transform.position.x; data.playerY = health.transform.position.y;
            data.cameraY = pressure.GameplayCamera.transform.position.y; data.distance = pressure.DistanceTravelled;
            data.turretAngle = health.GetComponent<PlayerTurretRig>().RotatingBody.localEulerAngles.z;
            data.seconds = elapsed; data.checkpoint = name;
            var campaign = CampaignDirector.Instance;
            if (campaign != null)
            {
                data.stage = campaign.Stage; data.wave = campaign.Round; data.theme = (int)pressure.Theme;
                data.campaign = JsonUtility.FromJson<CampaignState>(JsonUtility.ToJson(campaign.State));
            }
            if (data.slot == 0) { lastSaved = data; return true; }
            if (!RunStore.Write(data, out error)) { SaveError = error; return false; }
            lastSaved = data; SaveError = null; return true;
        }
        private void OnDestroy() { if (Active == this) Active = null; }
    }
}
