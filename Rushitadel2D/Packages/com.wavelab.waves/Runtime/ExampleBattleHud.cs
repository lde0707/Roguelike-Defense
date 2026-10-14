using UnityEngine;
namespace WaveLab.Waves
{
    public sealed class ExampleBattleHud : MonoBehaviour
    {
        public ExampleEnemy player;
        public WaveRunner runner;
        private float xp, gold;
        private bool lost;
        private void Awake() { runner.onReward.AddListener(Reward); }
        private void Reward(string resource, float amount) { if(resource=="xp")xp+=amount; if(resource=="gold")gold+=amount; }
        private void Update()
        {
            if (!lost && (player == null || player.CurrentHealth <= 0)) { lost = true; runner.Stop(); }
        }
        private void OnDestroy() { if(runner!=null)runner.onReward.RemoveListener(Reward); }
        private void OnGUI()
        {
            GUI.Box(new Rect(12,12,420,110), "자동 전투 테스트 (XY 평면)");
            GUI.Label(new Rect(24,38,395,25), lost ? "플레이어 패배 · Play를 다시 실행하세요" : $"플레이어 HP {player.CurrentHealth:0}/{player.maxHealth:0} · {runner.Status}");
            GUI.Label(new Rect(24,63,395,25), $"경험치 {xp:0} · 골드 {gold:0} · 처치 {runner.DefeatedCount}");
            GUI.Label(new Rect(24,88,395,25), "청록색: 플레이어 탄 / 주황색: 적 탄");
        }
    }
}
