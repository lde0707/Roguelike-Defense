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
        private Image twinWaterFill,twinElectricFill;
        private Image twinWaterPanel,twinElectricPanel;
        private Text twinWaterText,twinElectricText;
        private readonly Text[] turretText = new Text[6];
        private int augmentSlot = 1, augmentChoice = -1, webSelection = -1;
        private int nextThemeSelection = -1;
        private RectTransform battleLeft,battleRight;
        public void ShowBattle()
        {
            NewPage("game", false); Time.timeScale = IsTransitioning ? 0 : 1;
            var campaign = CampaignDirector.Instance;
            if (campaign == null) return;
            battleLeft=Rect(content,"Battle HUD Left",0,0,280,1040);
            battleRight=Rect(content,"Battle HUD Right",1640,0,280,1040);
            var top = Image(battleLeft, "Progress strip", 0, 0, 280, 120, panel);
            top.raycastTarget = false;
            progressText = Label(battleLeft, "", 24, 5, 232, 110, 23);
            var side = Image(battleLeft, "Player resources", 0, 135, 280, 330, panel); side.raycastTarget = false;
            healthText = Label(battleLeft, "", 24, 145, 232, 60, 22);
            powerText = Label(battleLeft, "", 24, 245, 232, 60, 22);
            resourcesText = Label(battleLeft, "", 24, 345, 232, 60, 22);
            Image(battleLeft, "HP track", 24, 215, 232, 10, Color.black).raycastTarget = false;
            hpFill = Image(battleLeft, "HP pixels", 24, 215, 232, 10, new Color32(150,194,127,255)); hpFill.raycastTarget = false;
            Image(battleLeft, "Power track", 24, 315, 232, 10, Color.black).raycastTarget = false;
            powerFill = Image(battleLeft, "Power pixels", 24, 315, 232, 10, new Color32(173,129,209,255)); powerFill.raycastTarget = false;
            Image(battleLeft, "XP track", 24, 415, 232, 10, Color.black).raycastTarget = false;
            xpFill = Image(battleLeft, "XP pixels", 24, 415, 232, 10, new Color32(64,180,236,255)); xpFill.raycastTarget = false;
            for (int i = 0; i < 6; i++)
            {
                int slot = i + 1;
                var b = Button(battleRight, "", 0, 375+i*96, 280, 88, () => ShowTurret(slot));
                turretText[i] = b.GetComponentsInChildren<Text>().First(); turretText[i].fontSize = 20;
            }
            Image(battleRight,"Boss status panel",0,0,280,165,panel).raycastTarget=false;
            bossText = Label(battleRight, "", 24, 5, 232, 140, 22);
            bossFill = Image(battleRight, "Boss HP", 24, 150, 232, 8, new Color32(210,116,82,255)); bossFill.raycastTarget = false;
            twinWaterPanel=Image(battleRight,"Water Boss panel",0,175,280,65,panel);twinWaterPanel.raycastTarget=false;
            twinElectricPanel=Image(battleRight,"Electric Boss panel",0,250,280,65,panel);twinElectricPanel.raycastTarget=false;
            twinWaterText=Label(battleRight,"",24,175,232,45,21);
            twinElectricText=Label(battleRight,"",24,250,232,45,21);
            twinWaterFill=Image(battleRight,"Water Boss HP",24,225,232,10,new Color32(90,200,240,255));twinWaterFill.raycastTarget=false;
            twinElectricFill=Image(battleRight,"Electric Boss HP",24,300,232,10,new Color32(244,199,80,255));twinElectricFill.raycastTarget=false;
            Image(battleRight,"Controls panel",0,965,280,55,panel).raycastTarget=false;
            Label(battleRight, "ESC  일시정지", 24, 970, 232, 40, 21);
            UpdateBattleHud();
        }
        private void UpdateBattleHud()
        {
            var d = CampaignDirector.Instance;
            if (page != "game" || d == null || !d.Initialized || progressText == null) return;
            LayoutBattleSides();
            string next = d.State.phase == RunPhase.Boss ? "보스전" : $"스폰 {d.Spawned}/{d.SpawnBudget} · 남은 적 {Rushitadel.Combat.EnemyRegistry.ActiveEnemies.Count()}";
            progressText.text = $"STAGE {d.Stage}/3 · R{d.Round}/3\n{next}";
            var h = PlayerHealth.Instance; var p = h.GetComponent<PlayerPower>(); var e = d.Experience;
            healthText.text = $"플레이어 HP\n{h.CurrentHealth:0} / {h.MaximumHealth:0}";
            powerText.text = $"동력 {p.CurrentPower:0}/{p.MaximumPower:0}\n우클릭 비용 {p.MaximumPower*h.GetComponent<PlayerPowerShotController>().CostFraction:0}";
            resourcesText.text = $"LV {e.Level} · {e.CurrentGaugeExperience}/100\nWeb XP {d.Wallet}";
            hpFill.rectTransform.sizeDelta = new Vector2(Mathf.Round(232*h.NormalizedHealth),10);
            powerFill.rectTransform.sizeDelta = new Vector2(Mathf.Round(232*p.NormalizedPower),10);
            xpFill.rectTransform.sizeDelta = new Vector2(Mathf.Round(232*e.NormalizedGauge),10);
            for (int i = 0; i < 6; i++) turretText[i].text = $"T{i+1} {Rushitadel.Combat.TurretForms.Names[d.State.turretForms[i]]}\n화력 {d.State.augments[i*3]} · 속도 {d.State.augments[i*3+1]}";
            bool alive = d.Boss != null && d.Boss.Health.IsAlive && d.State.phase == RunPhase.Boss;
            bool twins=alive&&d.Boss.Kind==BossKind.Twins;
            bossFill.gameObject.SetActive(alive&&!twins);
            twinWaterFill.gameObject.SetActive(twins);twinElectricFill.gameObject.SetActive(twins);
            twinWaterPanel.gameObject.SetActive(twins);twinElectricPanel.gameObject.SetActive(twins);
            twinWaterText.gameObject.SetActive(twins);twinElectricText.gameObject.SetActive(twins);
            if(twins)
            {
                var water=d.Boss.TwinHealth(0);var electric=d.Boss.TwinHealth(1);
                twinWaterText.text=water!=null?$"수압 보스  {water.CurrentHealth:0}/{water.MaximumHealth:0}":"수압 보스 · 격파";
                twinElectricText.text=electric!=null?$"전격 보스  {electric.CurrentHealth:0}/{electric.MaximumHealth:0}":"전격 보스 · 격파";
                twinWaterFill.rectTransform.sizeDelta=new Vector2(water!=null?232*water.CurrentHealth/water.MaximumHealth:0,10);
                twinElectricFill.rectTransform.sizeDelta=new Vector2(electric!=null?232*electric.CurrentHealth/electric.MaximumHealth:0,10);
            }
            bossText.text = alive ? $"{d.Boss.DisplayName} · {d.Boss.Phase+1}단계  {d.Boss.Health.CurrentHealth:0}/{d.Boss.Health.MaximumHealth:0}  {d.Boss.PatternLabel}" :
                Rushitadel.Combat.EnemyRangedAttack.IsReloadWindow(Time.time) ? "적 재장전 · 지금 공격!" : "코어 파괴 → 탄 약화 → 절벽 반사로 역습 (최대 3발)";
            if (alive) bossFill.rectTransform.sizeDelta = new Vector2(Mathf.Round(232*d.Boss.Health.CurrentHealth/d.Boss.Health.MaximumHealth),8);
            if(twins)bossText.text=$"수압·전격 쌍성 · {d.Boss.Phase+1}단계 · {d.Boss.PatternLabel}";
            if (ForwardPressureController.Instance.InDanger) progressText.text += "   ! 후방 위험 · 전진 !";
        }
        private void LayoutBattleSides()
        {
            if(battleLeft==null||battleRight==null)return;
            // Use the actual cliff boundaries, not a fixed aspect-ratio assumption.
            float unit=canvas.scaleFactor;
            float left=BattleSideSpace.LeftPixels/unit,right=BattleSideSpace.RightPixels/unit;
            float width=canvas.pixelRect.width/unit,height=canvas.pixelRect.height/unit;
            Place(battleLeft,left,false);Place(battleRight,right,true);
            battleLeft.gameObject.SetActive(!BossTestController.PanelVisible);
            void Place(RectTransform rt,float available,bool isRight)
            {
                float scale=Mathf.Min(1,Mathf.Max(0,available-24)/280,(height-24)/1040);
                rt.localScale=Vector3.one*scale;
                rt.anchoredPosition=new Vector2((1920-width)*.5f+(isRight?width-12-280*scale:12),-(1080-height)*.5f-12);
            }
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
            if(d.NeedsFirstForms){ShowTransformations();return;}
            var p = SafePanel($"LV {d.Experience.Level} · 포탑 증강 3택1"); page = "augment";
            Label(p, $"남은 선택 {d.PendingAugments}회 · 적용할 포탑을 선택하세요. 선택 중 전투는 정지합니다.", 45, 100, 1120, 65, 24);
            for (int i = 1; i <= 6; i++)
            {
                int slot = i;
                Button(p, (augmentSlot == slot ? "▶ " : "") + "T"+slot, 60+(i-1)*180, 175, 165, 60, () => { augmentSlot = slot; ShowAugments(); });
            }
            var offers=d.OfferedUpgrades(augmentSlot);
            if(!offers.Contains(augmentChoice))augmentChoice=-1;
            for (int i = 0; i < offers.Length; i++)
            {
                int kind = offers[i]; int rank = TurretAugments.Rank(augmentSlot,kind);
                Button(p, (augmentChoice == kind ? "▶ 선택\n" : "") + TurretAugments.Names[kind] + $"\n\nT{augmentSlot} · "+(kind==2?"고유":"공통")+"\n" + TurretAugments.Description(CampaignDirector.Form(augmentSlot),kind) + $"\n현재 {rank} → {rank+1} / {TurretAugments.Caps[kind]}",
                    45+i*380, 280, 350, 285, () => { augmentChoice = kind; ShowAugments(); });
                var icon=Image(p,"Augment icon "+kind,183+i*380,575,64,64,Color.white);
                icon.sprite=RetroUiArt.Icon(kind switch {0=>12,1=>3,2=>6,3=>15,4=>14,_=>10});
                icon.raycastTarget=false;icon.preserveAspect=true;
            }
            Label(p, "선택한 T번호에 적용 · 해당 변형에 효과가 없는 일반 강화는 선택할 수 없습니다.", 60, 660, 1080, 55, 24);
            Button(p, "선택 확정", 780, 735, 350, 75, () => {
                if (d.ChooseUpgrade(augmentSlot, augmentChoice)) { augmentChoice = -1; d.ReopenPage(); }
            }, augmentChoice >= 0&&offers.Contains(augmentChoice));
            if(offers.Length==0)Button(p,"모든 증강 완료 · 정비 보상",60,735,650,75,()=>{if(d.ChooseCompletionReward(augmentSlot))d.ReopenPage();});
            back = ShowPause;
        }
        public void ShowMaintenance()
        {
            CampaignDirector.Instance.CompleteRoundRecovery();
        }
        public void ShowNextTheme()
        {
            var d = CampaignDirector.Instance; var p = SafePanel($"STAGE {d.Stage+1} · 테마 선택"); page = "next-theme";
            ExpandThemePanel(p);
            Label(p, "이미 방문한 테마는 제외됩니다. 다음 스테이지도 총 3라운드입니다.", 55, 120, 1100, 70, 25);
            for (int i = 0; i < 2; i++)
            {
                int theme = d.State.themeChoices[i];
                ThemeCard(p, theme, nextThemeSelection == theme, 60+i*690, 230, 630, 374,
                    () => { nextThemeSelection = theme; ShowNextTheme(); });
            }
            Button(p, "테마 확정 · 출발", 750, 710, 630, 75, () => { d.SelectTheme(nextThemeSelection); nextThemeSelection = -1; }, d.State.themeChoices.Contains(nextThemeSelection));
            back = ShowPause;
        }
        public void ShowRunResult(bool won)
        {
            var d = CampaignDirector.Instance;
            if (d == null) return;
            BuildResult(won, $"도달 STAGE {d.Stage}/3 · ROUND {d.Round}/3\n보스 격파 {d.State.defeatedBosses}/3 · LV {d.Experience.Level}\n총 XP {d.Experience.TotalExperience} · 포탑 증강 {d.State.augmentsChosen}개\nWeb {d.State.web.Count(v => v)}개");
            if (!resultCuePlayed && Application.isPlaying)
            {
                resultCuePlayed=true;
                audioSource.PlayOneShot(won ? clearClip : defeatClip);
            }
            back = () => ConfirmQuit(() => ShowRunResult(won));
        }
        private void BuildResult(bool won, string summary)
        {
            var p = SafePanel(won ? "CLEAR · 성채 돌파" : "GAME OVER · 코어 소실"); page = "result";
            var emblem=Image(p,"Result emblem",460,115,280,280,Color.white);
            emblem.sprite=RetroUiArt.Result(won);emblem.preserveAspect=true;emblem.raycastTarget=false;
            Label(p,won?"세 개의 스테이지를 모두 돌파했습니다":"이번 원정은 여기서 마무리됩니다",150,400,900,55,30,TextAnchor.MiddleCenter);
            Label(p,summary,150,470,900,180,28,TextAnchor.MiddleCenter);
            Button(p,"타이틀로",360,715,480,80,()=>Load("Title"));
            if(Application.isPlaying)p.gameObject.AddComponent<ResultUiMotion>().Initialize(emblem.rectTransform,won);
        }

        private void ShowTurret(int slot)
        {
            Time.timeScale = 0; var d = CampaignDirector.Instance;
            Confirm($"T{slot} · {Rushitadel.Combat.TurretForms.Names[CampaignDirector.Form(slot)]}\n화력 {CampaignDirector.Rank(slot,0)} / 속도 {CampaignDirector.Rank(slot,1)} / 관통 {CampaignDirector.Rank(slot,2)}\n변형은 고유 타격 상한을 사용합니다.\n레벨업 시 추가 증강을 선택합니다.", d.ReopenPage, d.ReopenPage);
        }
    }
}
