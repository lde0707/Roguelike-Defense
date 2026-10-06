using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Rushitadel.Presentation
{
    public sealed partial class GameFrontend
    {
        private SettingsData draft, settingsBefore;
        private Action settingsReturn;
        private int settingsTab;
        private float previewDeadline;
        private Text countdown;
        private Vector2Int[] resolutions;
        private static readonly int[] FrameCaps = {30, 60, 120, 144, -1};

        private void ShowSettings(Action returnTo)
        {
            settingsBefore = GameSettings.Saved.Copy(); draft = settingsBefore.Copy();
            settingsReturn = returnTo; settingsTab = 0;
            resolutions = Screen.resolutions.Select(r => new Vector2Int(r.width, r.height)).Distinct().ToArray();
            if (resolutions.Length == 0) resolutions = new[] {new Vector2Int(Screen.width, Screen.height)};
            DrawSettings();
        }
        private void DrawSettings()
        {
            NewPage("settings", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Title");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Title")
                Image(content, "Shade", 0, 0, 1920, 1080, new Color(0, 0, 0, 0.8f));
            var p = Panel("설정");
            var tabs = new[] {"표시", "사운드", "조작"};
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                Button(p, (settingsTab == i ? "▶ " : "") + tabs[i], 50 + i * 365, 105, 345, 65, () => { settingsTab = index; DrawSettings(); });
            }
            if (settingsTab == 0)
            {
                Choice(p, "해상도", draft.width + " × " + draft.height, 215, () => {
                    int at = Array.FindIndex(resolutions, r => r.x == draft.width && r.y == draft.height);
                    var r = resolutions[(at + 1) % resolutions.Length]; draft.width = r.x; draft.height = r.y; DrawSettings(); });
                Choice(p, "화면 모드", new[] {"전체 화면", "테두리 없는 전체 화면", "창 모드"}[draft.mode], 315,
                    () => { draft.mode = (draft.mode + 1) % 3; DrawSettings(); });
                Choice(p, "FPS 제한", draft.fps < 0 ? "제한 없음" : draft.fps.ToString(), 415,
                    () => { draft.fps = FrameCaps[(Array.IndexOf(FrameCaps, draft.fps) + 1) % FrameCaps.Length]; DrawSettings(); });
                Choice(p, "수직 동기화", draft.vsync ? "ON" : "OFF", 515,
                    () => { draft.vsync = !draft.vsync; DrawSettings(); });
                Label(p, "표시 변경 후 15초 안에 유지 여부를 확인하세요.\n수직 동기화 ON에서는 모니터 주사율이 FPS 제한보다 우선합니다." +
                    (Application.isEditor ? "\n에디터에서는 해상도·화면 모드가 Game 창 크기를 바꾸지 않습니다." : ""), 50, 620, 1100, 105, 23);
            }
            else if (settingsTab == 1)
            {
                Volume(p, "전체 음량", draft.master, 220, v => draft.master = v);
                Volume(p, "BGM", draft.music, 355, v => draft.music = v);
                Volume(p, "효과음", draft.effects, 490, v => draft.effects = v);
                Button(p, "효과음 미리듣기", 620, 620, 450, 65, () => audioSource.PlayOneShot(clickClip));
                Label(p, "변경 즉시 미리 적용 · 취소하면 이전 값으로 복구", 50, 710, 1100, 40, 23);
            }
            else
            {
                Label(p, "이동                         W / A / S / D\n조준                         마우스 방향\n원거리 공격              마우스 좌클릭\n동력 범위 공격          마우스 우클릭 · 동력 70%\n일시정지 / 뒤로        ESC\n포탑                         정면 자동 사격 · 약 5유닛", 75, 205, 1050, 390, 30);
                Label(p, "고정 조작 안내 · 키 재지정은 현재 기획 범위에 없습니다.", 75, 630, 1050, 65, 24);
            }
            Button(p, "취소", 650, 760, 220, 65, CancelSettings);
            Button(p, "적용", 900, 760, 220, 65, ApplySettings);
            back = () => {
                if (draft.Same(settingsBefore)) CancelSettings();
                else Confirm("변경사항을 버리고 돌아갈까요?", CancelSettings, null, "변경 취소", "계속 설정");
            };
        }
        private void Choice(Transform p, string label, string value, float y, Action change)
        {
            Label(p, label, 75, y, 400, 70, 30);
            Button(p, value + "  ▶", 510, y, 590, 70, change);
        }
        private void Volume(Transform p, string title, float value, float y, Action<float> set)
        {
            Label(p, title, 75, y, 250, 70, 30);
            var number = Label(p, Mathf.RoundToInt(value * 100) + "%", 1010, y, 110, 70, 30);
            var root = Rect(p, title + " Slider", 350, y + 10, 610, 50);
            var track = Image(root, "Track", 0, 20, 610, 10, new Color32(75, 83, 80, 255));
            var fillArea = Rect(root, "Fill Area", 0, 20, 610, 10);
            var fill = Image(fillArea, "Fill", 0, 0, 610, 10, ink);
            fill.rectTransform.sizeDelta = Vector2.zero;
            fill.rectTransform.anchoredPosition = Vector2.zero;
            var handleArea = Rect(root, "Handle Area", 12, 0, 586, 50);
            var handle = Image(handleArea, "Handle", 0, 0, 24, 50, ink);
            handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            handle.rectTransform.sizeDelta = new Vector2(24, 0);
            handle.rectTransform.anchoredPosition = Vector2.zero;
            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            slider.minValue = 0; slider.maxValue = 100; slider.wholeNumbers = true; slider.value = Mathf.RoundToInt(value * 100);
            slider.onValueChanged.AddListener(v => { set(v / 100f); number.text = ((int)v) + "%"; GameSettings.PreviewAudio(draft); });
        }
        private void CancelSettings()
        {
            previewDeadline = 0; GameSettings.Apply(settingsBefore); settingsReturn?.Invoke();
        }
        private void ApplySettings()
        {
            bool displayChanged = draft.width != settingsBefore.width || draft.height != settingsBefore.height || draft.mode != settingsBefore.mode;
            GameSettings.Apply(draft);
            if (displayChanged)
            {
                Confirm("화면 설정을 유지할까요?", CommitSettings, RevertDisplay, "유지", "되돌리기");
                previewDeadline = Time.unscaledTime + 15f;
            }
            else CommitSettings();
        }
        private void CommitSettings()
        {
            previewDeadline = 0;
            if (!GameSettings.Save(draft, out string error)) { GameSettings.Apply(settingsBefore); Error(error, DrawSettings); return; }
            settingsReturn?.Invoke();
        }
        private void RevertDisplay()
        {
            previewDeadline = 0; CloseDialog(); GameSettings.Apply(settingsBefore);
            draft = settingsBefore.Copy(); DrawSettings();
        }
    }
}
