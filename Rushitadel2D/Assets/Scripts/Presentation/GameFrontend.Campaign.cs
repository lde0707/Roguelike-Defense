using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Rushitadel.Progression;
using Rushitadel.Player;
using Rushitadel.Power;
using Rushitadel.World;

namespace Rushitadel.Presentation
{
    public sealed partial class GameFrontend
    {
        private Text progressText, resourcesText, healthText, powerText, bossText;
        private Image xpFill, hpFill, powerFill, bossFill;
        private readonly Text[] turretText = new Text[5];
        private int augmentSlot = 1, augmentChoice = -1, webSelection = -1;
        private int nextThemeSelection = -1;
        public void ShowBattle()
        {
            NewPage("game", false); Time.timeScale = IsTransitioning ? 0 : 1;
            var campaign = CampaignDirector.Instance;
            if (campaign == null) return;
            var top = Image(content, "Progress strip", 355, 15, 1210, 65, panel);
            top.raycastTarget = false;
            progressText = Label(content, "", 380, 20, 1150, 50, 25, TextAnchor.MiddleCenter);
            var side = Image(content, "Bottom resources", 20, 965, 875, 95, panel); side.raycastTarget = false;
            healthText = Label(content, "", 35, 970, 260, 60, 22);
            powerText = Label(content, "", 325, 970, 260, 60, 22);
            resourcesText = Label(content, "", 615, 970, 265, 60, 22);
            Image(content, "HP track", 35, 1040, 260, 10, Color.black).raycastTarget = false;
            hpFill = Image(content, "HP pixels", 35, 1040, 260, 10, new Color32(150,194,127,255)); hpFill.raycastTarget = false;
            Image(content, "Power track", 325, 1040, 260, 10, Color.black).raycastTarget = false;
            powerFill = Image(content, "Power pixels", 325, 1040, 260, 10, new Color32(173,129,209,255)); powerFill.raycastTarget = false;
            Image(content, "XP track", 615, 1040, 260, 10, Color.black).raycastTarget = false;
            xpFill = Image(content, "XP pixels", 615, 1040, 260, 10, new Color32(64,180,236,255)); xpFill.raycastTarget = false;
            for (int i = 0; i < 5; i++)
            {
                int slot = i + 1;
                var b = Button(content, "", 925+i*195, 967, 183, 88, () => ShowTurret(slot));
                turretText[i] = b.GetComponentsInChildren<Text>().First(); turretText[i].fontSize = 20;
            }
            bossText = Label(content, "", 545, 82, 830, 35, 22, TextAnchor.MiddleCenter);
            bossFill = Image(content, "Boss HP", 610, 122, 700, 8, new Color32(210,116,82,255)); bossFill.raycastTarget = false;
            Label(content, "ESC  일시정지", 1660, 20, 235, 40, 21);
            UpdateBattleHud();
        }
        private void UpdateBattleHud()
        {
            var d = CampaignDirector.Instance;
            if (page != "game" || d == null || !d.Initialized || progressText == null) return;
            string next = d.State.phase == RunPhase.Boss ? "보스전" : $"스폰 {d.Spawned}/{d.SpawnBudget} · 남은 적 {Rushitadel.Combat.EnemyRegistry.ActiveEnemies.Count()}";
            progressText.text = $"STAGE {d.Stage}/3   ROUND {d.Round}/3     {next}";
            var h = PlayerHealth.Instance; var p = h.GetComponent<PlayerPower>(); var e = d.Experience;
            healthText.text = $"플레이어 HP\n{h.CurrentHealth:0} / {h.MaximumHealth:0}";
            powerText.text = $"동력 {p.CurrentPower:0}/100\n우클릭 비용 70";
            resourcesText.text = $"LV {e.Level} · {e.CurrentGaugeExperience}/100\nWeb XP {d.Wallet}";
            hpFill.rectTransform.sizeDelta = new Vector2(Mathf.Round(260*h.NormalizedHealth),10);
            powerFill.rectTransform.sizeDelta = new Vector2(Mathf.Round(260*p.NormalizedPower),10);
            xpFill.rectTransform.sizeDelta = new Vector2(Mathf.Round(260*e.NormalizedGauge),10);
            for (int i = 0; i < 5; i++) turretText[i].text = $"T{i+1} {Rushitadel.Combat.TurretForms.Names[d.State.turretForms[i]]}\n화력 {d.State.augments[i*3]} · 속도 {d.State.augments[i*3+1]}";
            bool alive = d.Boss != null && d.Boss.Health.IsAlive && d.State.phase == RunPhase.Boss;
            bossFill.gameObject.SetActive(alive);
            bossText.text = alive ? $"절벽 감시자 {d.Stage}  {d.Boss.Health.CurrentHealth:0}/{d.Boss.Health.MaximumHealth:0}  {d.Boss.PatternLabel}" : "다음 위협: 절벽 좌·우 사격";
            if (alive) bossFill.rectTransform.sizeDelta = new Vector2(Mathf.Round(700*d.Boss.Health.CurrentHealth/d.Boss.Health.MaximumHealth),8);
            if (ForwardPressureController.Instance.InDanger) progressText.text += "   ! 후방 위험 · 전진 !";
        }
        private RectTransform SafePanel(string title)
        {
            Time.timeScale = 0; NewPage("maintenance", false);
            Image(content, "Safe shade", 0, 0, 1920, 1080, new Color(0.03f,.07f,.10f,.9f));
            return Panel(title);
        }
        public void ShowAugments()
        {
            var d = CampaignDirector.Instance;
            if (d == null || d.PendingAugments <= 0) { d?.ReopenPage(); return; }
            var p = SafePanel($"LV {d.Experience.Level} · 포탑 증강 3택1"); page = "augment";
            Label(p, $"남은 선택 {d.PendingAugments}회 · 적용할 포탑을 선택하세요. 선택 중 전투는 정지합니다.", 45, 100, 1120, 65, 24);
            for (int i = 1; i <= 5; i++)
            {
                int slot = i;
                Button(p, (augmentSlot == slot ? "▶ " : "") + "T"+slot, 60+(i-1)*215, 175, 200, 60, () => { augmentSlot = slot; ShowAugments(); });
            }
            for (int i = 0; i < 3; i++)
            {
                int kind = i; int rank = CampaignDirector.Rank(augmentSlot, kind);
                Button(p, (augmentChoice == i ? "▶ 선택\n" : "") + CampaignDirector.AugmentNames[i] + $"\n\nT{augmentSlot} · 일반\n" + CampaignDirector.AugmentEffects[i] + $"\n현재 {rank} → {rank+1}",
                    45+i*380, 280, 350, 285, () => { augmentChoice = kind; ShowAugments(); },d.CanChooseStat(augmentSlot,kind));
            }
            Label(p, "선택한 T번호에 적용 · 해당 변형에 효과가 없는 일반 강화는 선택할 수 없습니다.", 60, 615, 1080, 55, 24);
            Button(p,"변형 포탑 선택",60,735,350,75,ShowTransformations);
            Button(p, "선택 확정", 780, 735, 350, 75, () => {
                if (d.ChooseAugment(augmentSlot, augmentChoice)) { augmentChoice = -1; d.ReopenPage(); }
            }, augmentChoice >= 0&&d.CanChooseStat(augmentSlot,augmentChoice));
            back = ShowPause;
        }
        public void ShowMaintenance()
        {
            var d = CampaignDirector.Instance;
            var p = SafePanel($"안전 정비 · STAGE {d.Stage} / ROUND {d.Round} 완료");
            if (!d.State.recovered)
            {
                Label(p, "적과 탄이 정리된 안전 장소입니다. 회복 하나를 선택하거나 건너뛰세요.", 55, 115, 1090, 80, 26);
                var health = PlayerHealth.Instance; var power = health.GetComponent<PlayerPower>();
                Button(p, "플레이어 HP 회복\n\n최대 HP의 30%", 70, 245, 490, 240, () => { d.Recover(0); ShowMaintenance(); }, health.CurrentHealth < health.MaximumHealth);
                Button(p, "동력 회복\n\n동력 +50", 640, 245, 490, 240, () => { d.Recover(1); ShowMaintenance(); }, power.CurrentPower < power.MaximumPower);
                Button(p, "건너뛰고 Web", 735, 700, 390, 75, () => { d.Recover(2); ShowMaintenance(); });
            }
            else DrawWeb(p, d);
            back = ShowPause;
        }
        private void DrawWeb(RectTransform p, CampaignDirector d)
        {
            Label(p, $"XP WEB   보유 XP {d.Wallet}   · 연결 노드만 구매 가능", 50, 100, 1100, 65, 27);
            // Deliberately rectilinear pixel wiring, not smooth Bezier lines.
            Image(p, "Upper connection", 200, 315, 550, 6, ink).raycastTarget = false;
            Image(p, "Right connection", 740, 315, 6, 210, ink).raycastTarget = false;
            Image(p, "Lower connection", 200, 525, 550, 6, ink).raycastTarget = false;
            Image(p, "Root connection", 200, 315, 6, 210, ink).raycastTarget = false;
            int[] xs = {55,305,555,555,305,55}; int[] ys = {220,220,220,430,430,430};
            for (int i = 0; i < 6; i++)
            {
                int id = i; d.CanBuy(id, out string status);
                Button(p, (webSelection == i ? "▶ " : "") + CampaignDirector.WebNames[i] + $"\n{CampaignDirector.WebCosts[i]} XP\n{status}", xs[i], ys[i], 220, 150,
                    () => { webSelection = id; ShowMaintenance(); });
            }
            Label(p, "시작 연결: 관통 / 동력 / 지원\n관통 → 분산 → 폭발 → 독", 55, 620, 745, 70, 23);
            Button(p,"플레이어 속성 발광",55,710,510,45,ShowElementSelection);
            if (webSelection >= 0)
            {
                int id = webSelection;
                Label(p, CampaignDirector.WebNames[id]+" · 희귀\n\n플레이어 적용\n"+CampaignDirector.WebEffects[id]+$"\n\n비용 {CampaignDirector.WebCosts[id]} XP\n구매 후 {d.Wallet-CampaignDirector.WebCosts[id]} XP", 820, 205, 330, 340, 25);
                Button(p, "노드 구매", 830, 555, 300, 65, () => { d.BuyWeb(id); ShowMaintenance(); }, d.CanBuy(id, out _));
            }
            string next = d.Round < 3 ? $"다음: ROUND {d.Round+1}" : d.Stage < 3 ? "다음: 스테이지 테마 선택" : "3스테이지 클리어";
            Label(p, next, 50, 750, 600, 60, 26);
            Button(p, d.Stage == 3 && d.Round == 3 ? "런 완료" : "출발", 830, 735, 300, 75, d.Depart);
            if (!string.IsNullOrEmpty(d.SaveError))
                Button(p, "저장 실패 · 재시도", 55, 690, 710, 50, () => { d.SaveCheckpoint("정비 재시도"); ShowMaintenance(); });
        }
        public void ShowNextTheme()
        {
            var d = CampaignDirector.Instance; var p = SafePanel($"STAGE {d.Stage+1} · 테마 선택"); page = "next-theme";
            Label(p, "이미 방문한 테마는 제외됩니다. 다음 스테이지도 총 3라운드입니다.", 55, 120, 1100, 70, 25);
            for (int i = 0; i < 2; i++)
            {
                int theme = d.State.themeChoices[i];
                Button(p, (nextThemeSelection == theme ? "▶ " : "") + Themes[theme], 70+i*560, 255, 490, 230,
                    () => { nextThemeSelection = theme; ShowNextTheme(); });
            }
            Button(p, "테마 확정 · 출발", 650, 710, 460, 75, () => { d.SelectTheme(nextThemeSelection); nextThemeSelection = -1; }, d.State.themeChoices.Contains(nextThemeSelection));
            back = ShowPause;
        }
        public void ShowRunResult(bool won)
        {
            var d = CampaignDirector.Instance; var p = SafePanel(won ? "VICTORY · 3스테이지 클리어" : "DEFEAT · 플레이어 HP 0"); page = "result";
            Label(p, $"도달 STAGE {d.Stage}/3 · ROUND {d.Round}/3\n\n보스 격파 {d.State.defeatedBosses}/3\n레벨 {d.Experience.Level} · 총 XP {d.Experience.TotalExperience}\n포탑 증강 {d.State.augmentsChosen}개 · Web {d.State.web.Count(v => v)}개", 100, 170, 1000, 340, 30);
            Button(p, "타이틀로", 360, 680, 480, 80, () => Load("Title"));
            back = () => ConfirmQuit(() => ShowRunResult(won));
        }
        private void ShowTurret(int slot)
        {
            Time.timeScale = 0; var d = CampaignDirector.Instance;
            Confirm($"T{slot} · {Rushitadel.Combat.TurretForms.Names[CampaignDirector.Form(slot)]}\n화력 {CampaignDirector.Rank(slot,0)} / 속도 {CampaignDirector.Rank(slot,1)} / 관통 {CampaignDirector.Rank(slot,2)}\n변형은 고유 타격 상한을 사용합니다.\n레벨업 시 추가 증강을 선택합니다.", d.ReopenPage, d.ReopenPage);
        }
    }
}
