using System;
using UnityEngine;
namespace Rushitadel.Presentation
{
    public sealed partial class GameFrontend
    {
        private static readonly string[] Themes = {"풀 평원", "불·용암", "얼음", "물", "물·전기"};
        private int newSlot, chosenTheme = -1;
        private int[] candidates;
        public void ShowSlots(bool newGame)
        {
            NewPage("slots"); var p = Panel(newGame ? "새 게임 · 저장 슬롯 선택" : "이어하기 · 저장 슬롯");
            Label(p, "최근 안전 저장 지점에서 재개합니다. 손상된 파일은 자동 삭제하지 않습니다.", 50, 105, 1100, 55, 23);
            var latest = RunStore.Latest();
            for (int i = 1; i <= 3; i++)
            {
                int slot = i; float y = 185 + (i - 1) * 180;
                bool valid = RunStore.Read(slot, out var data, out var error);
                bool exists = RunStore.Exists(slot);
                string title = $"슬롯 {slot}" + (latest != null && latest.slot == slot ? " · 최근 저장" : "");
                string details = valid ? $"{Themes[data.theme]} · STAGE {data.stage}/3 · ROUND {data.wave}/3\n{TimeSpan.FromSeconds(data.seconds):hh\\:mm\\:ss}  |  {DateTime.Parse(data.savedUtc).ToLocalTime():yyyy-MM-dd HH:mm}\nHP {data.health:0} · LV {data.experience/100+1} · 증강 {data.campaign.augmentsChosen}개\n{data.checkpoint}" : exists ? "불러올 수 없음\n" + error : "빈 슬롯 · 새 런을 시작하세요";
                Label(p, title, 65, y, 700, 40, 30);
                Label(p, details, 65, y + 42, 730, 128, 23);
                Button(p, newGame || !exists ? "새 게임" : "불러오기", 855, y + 45, 270, 70, () => {
                    if (newGame || !exists) SelectNewSlot(slot); else ContinueSlot(slot);
                }, newGame || !exists || valid);
            }
            Button(p, "돌아가기", 850, 765, 275, 65, ShowTitle);
            back = ShowTitle;
        }
        private void ContinueSlot(int slot)
        {
            if (!RunStore.Read(slot, out var data, out var error)) { Error(error, () => ShowSlots(false)); return; }
            RunSession.Pending = data; Load("inGame");
        }
        private void SelectNewSlot(int slot)
        {
            newSlot = slot;
            Action proceed = () => {
                int a = UnityEngine.Random.Range(0, Themes.Length), b;
                do { b = UnityEngine.Random.Range(0, Themes.Length); } while (b == a);
                candidates = new[] {a, b}; chosenTheme = -1; ShowThemes();
            };
            if (RunStore.Exists(slot)) Confirm($"슬롯 {slot}을 새 런으로 덮어쓸까요?\n테마를 확정할 때 저장합니다. 이전 정상본은 백업으로 보존됩니다.", proceed);
            else proceed();
        }
        private void ShowThemes()
        {
            NewPage("themes"); var p = Panel("스테이지 테마 선택");
            ExpandThemePanel(p);
            Label(p, "두 후보 중 하나를 선택하세요. 확정 후 안전 저장하고 전투를 시작합니다.", 50, 115, 1100, 65, 25);
            for (int i = 0; i < 2; i++)
            {
                int theme = candidates[i];
                ThemeCard(p, theme, chosenTheme == theme, 60 + i * 690, 230, 630, 374,
                    () => { chosenTheme = theme; ShowThemes(); });
            }
            Button(p, "돌아가기", 75, 720, 330, 75, () => ShowSlots(true));
            Button(p, "선택 확정 · 시작", 750, 720, 630, 75, () => {
                var data = new RunData { slot = newSlot, theme = chosenTheme, seed = UnityEngine.Random.Range(1, int.MaxValue), runId = Guid.NewGuid().ToString() };
                Rushitadel.Progression.RandomPlayerWeb.Ensure(data.campaign,data.seed);
                if (!RunStore.Write(data, out string error)) { Error(error, ShowThemes); return; }
                RunSession.Pending = data; Load("inGame");
            }, chosenTheme >= 0);
            back = () => ShowSlots(true);
        }
    }
}
