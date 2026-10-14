using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using Rushitadel.Player;

namespace Rushitadel.Presentation
{
    public sealed partial class GameFrontend : MonoBehaviour
    {
        public static GameFrontend Instance { get; private set; }
        public static bool BlocksGameplay => Instance != null && (Instance.page != "game" || Instance.transitioning);
        private Canvas canvas;
        private RectTransform content, modal;
        private Font font;
        private string page = "";
        private Action back;
        private Action dialogCancel;
        private bool loading;
        private AudioSource audioSource;
        private AudioClip clickClip;
        private readonly Color ink = new Color32(183, 195, 173, 255);
        private readonly Color panel = new Color32(27, 34, 43, 255);
        public string CurrentPage => page;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SceneManager.sceneLoaded -= BootstrapScene;
            Instance = null; RunSession.Reset();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            SceneManager.sceneLoaded -= BootstrapScene;
            SceneManager.sceneLoaded += BootstrapScene;
        }
        private static void BootstrapScene(Scene scene, LoadSceneMode mode)
        {
            if (scene.path != "Assets/Scenes/Title.unity" && scene.path != "Assets/Scenes/inGame.unity") return;
            if (Instance != null) return; // Awake already subscribed SceneReady before this load.
            Ensure();
            // A listener added during sceneLoaded does not receive the current dispatch.
            Instance.SceneReady(scene, mode);
        }
        public static void Ensure()
        {
            if (Instance == null) new GameObject("Game Frontend").AddComponent<GameFrontend>();
        }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            GameSettings.Load();
            font = Resources.Load<Font>("Fonts/NeoDunggeunmo");
            if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] {"Malgun Gothic", "Arial"}, 28);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; audioSource.ignoreListenerPause = true;
            gameObject.AddComponent<AudioChannel>();
            clickClip = AudioClip.Create("UI confirm", 2400, 1, 48000, false);
            var samples = new float[2400];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(i * 2f * Mathf.PI * 660f / 48000f) * 0.1f * (1f - i / 2400f);
            clickClip.SetData(samples, 0);
            SceneManager.sceneLoaded += SceneReady;
        }
        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= SceneReady;
            if (clickClip != null) Destroy(clickClip);
            DisposeTitleMaterials();
            // The bundled font is a shared Resources asset, not an owned runtime object.
            Instance = null;
        }
        private void SceneReady(Scene scene, LoadSceneMode mode)
        {
            if (!transitioning) loading = false;
            previewDeadline = 0;
            if (scene.name == "Title")
            {
                foreach (var old in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    if (old != canvas && !old.transform.IsChildOf(transform)) old.gameObject.SetActive(false);
                Time.timeScale = transitioning ? 0 : 1f; RunSession.Leave(); ShowTitle();
            }
            else if (scene.name == "inGame")
            {
                FindFirstObjectByType<Rushitadel.Spawning.EnemySpawner>()?.TakeCampaignControl();
                NewPage("game", false);
                new GameObject("Run Session").AddComponent<RunSession>();
            }
        }
        private void Update()
        {
            UpdateBattleHud();
            if (previewDeadline > 0)
            {
                if (countdown != null) countdown.text = $"화면 설정을 유지할까요?  {Mathf.CeilToInt(previewDeadline - Time.unscaledTime)}초";
                if (Time.unscaledTime >= previewDeadline) RevertDisplay();
            }
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame || loading) return;
            if (modal != null) { dialogCancel?.Invoke(); return; }
            if (page == "game") { ShowPause(); return; }
            back?.Invoke();
        }
        private void NewPage(string name, bool backdrop = true)
        {
            EnsureTitleMaterials();
            page = name; back = null; dialogCancel = null;
            if (canvas == null)
            {
                var go = new GameObject("Frontend Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                go.transform.SetParent(transform);
                canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 500;
                canvas.pixelPerfect = true;
                var scale = go.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scale.referenceResolution = new Vector2(1920, 1080);
                scale.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }
            foreach (Transform child in canvas.transform.Cast<Transform>().ToArray())
            {
                child.gameObject.SetActive(false);
                if(Application.isPlaying)Destroy(child.gameObject);else DestroyImmediate(child.gameObject);
            }
            modal = null;
            content = Rect(canvas.transform, "Page " + name, 0, 0, 1920, 1080);
            CenterPage(content);
            if (backdrop)
            {
                var art = Image(canvas.transform, "Background", 0, 0, 1920, 1080, new Color(0.32f, 0.34f, 0.4f), "Background");
                art.rectTransform.anchorMin = Vector2.zero; art.rectTransform.anchorMax = Vector2.one;
                art.rectTransform.offsetMin = art.rectTransform.offsetMax = Vector2.zero;
                art.transform.SetAsFirstSibling();
                art.raycastTarget = false;
                art.material = titleBackgroundMaterial;
            }
            if (Application.isPlaying && EventSystem.current == null)
                new GameObject("UI Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
        private static void CenterPage(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }
        private RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false); rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1); rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(w, h);
            return rt;
        }
        private Image Image(Transform parent, string name, float x, float y, float w, float h, Color color, string sprite = null)
        {
            var image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color;
            if (page == "title" && name != "Background") image.material = titleCursorMaterial;
            if (sprite != null) image.sprite = Resources.Load<Sprite>("TitleUI/" + sprite);
            return image;
        }
        private Text Label(Transform parent, string text, float x, float y, float w, float h, int size = 28, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var label = Rect(parent, text, x, y, w, h).gameObject.AddComponent<Text>();
            label.font = font; label.text = text; label.fontSize = size; label.color = ink; label.alignment = align;
            if (page == "title") label.material = titleCursorMaterial;
            label.raycastTarget = false; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }
        private Button Button(Transform parent, string text, float x, float y, float w, float h, Action action, bool enabled = true)
        {
            var border = Image(parent, text + " Button", x, y, w, h, ink);
            var fill = Image(border.transform, "Fill", 3, 3, w - 6, h - 6, panel);
            var button = border.gameObject.AddComponent<Button>();
            button.targetGraphic = fill; button.interactable = enabled;
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(2f, 2f, 1.8f);
            colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(0.65f, 0.7f, 0.65f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f); colors.fadeDuration = 0;
            button.colors = colors;
            var label = Label(border.transform, text, 12, 0, w - 24, h, 28, TextAnchor.MiddleCenter);
            if (!enabled) { border.color = Color.gray; label.color = Color.gray; }
            button.onClick.AddListener(() => { audioSource.PlayOneShot(clickClip); action?.Invoke(); });
            return button;
        }
        private void ArtButton(string id, int row, Action action, bool enabled = true)
        {
            var image = Image(content, id + "Button", 720, 340 + row * 116, 480, 96, Color.white, id + "_Normal");
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            var state = new SpriteState {
                highlightedSprite = Resources.Load<Sprite>("TitleUI/" + id + "_Hover"),
                selectedSprite = Resources.Load<Sprite>("TitleUI/" + id + "_Hover"),
                pressedSprite = Resources.Load<Sprite>("TitleUI/" + id + "_Pressed"),
                disabledSprite = Resources.Load<Sprite>("TitleUI/" + id + "_Normal") };
            button.spriteState = state; button.interactable = enabled;
            if (!enabled) image.color = new Color(0.5f, 0.5f, 0.5f);
            button.onClick.AddListener(() => { if (!loading) { audioSource.PlayOneShot(clickClip); action(); } });
        }
        private RectTransform Panel(string title)
        {
            var border = Image(content, "Panel Border", 360, 110, 1200, 860, ink);
            Image(border.transform, "Panel Fill", 3, 3, 1194, 854, panel);
            Label(border.transform, title, 50, 25, 1100, 65, 40);
            return border.rectTransform;
        }
        private void CloseDialog()
        {
            if (modal != null) { modal.gameObject.SetActive(false); Destroy(modal.gameObject); }
            modal = null; dialogCancel = null;
            if (content != null && content.TryGetComponent<CanvasGroup>(out var group)) group.interactable = true;
        }
        private void Confirm(string text, Action yes, Action no = null, string yesText = "확인", string noText = "취소")
        {
            CloseDialog();
            var group = content.GetComponent<CanvasGroup>();
            if (group == null) group = content.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            modal = Rect(canvas.transform, "Confirmation", 0, 0, 1920, 1080);
            CenterPage(modal);
            Image(modal, "Input Blocker", 0, 0, 1920, 1080, new Color(0, 0, 0, 0.72f));
            var box = Image(modal, "Dialog Border", 470, 340, 980, 400, ink);
            Image(box.transform, "Dialog Fill", 3, 3, 974, 394, panel);
            countdown = Label(box.transform, text, 45, 35, 890, 200, 30, TextAnchor.MiddleCenter);
            Button(box.transform, yesText, 170, 285, 280, 70, () => { CloseDialog(); yes?.Invoke(); });
            Button(box.transform, noText, 530, 285, 280, 70, () => { CloseDialog(); no?.Invoke(); });
            dialogCancel = () => { CloseDialog(); no?.Invoke(); };
        }
        private void Error(string text, Action returnTo = null) => Confirm(text, returnTo, returnTo, "확인", "돌아가기");
        public void ShowTitle()
        {
            NewPage("title");
            Image(content, "Logo", 650, 175, 620, 95, Color.white, "Logo").raycastTarget = false;
            ArtButton("Start", 0, () => ShowSlots(true));
            ArtButton("Continue", 1, () => ShowSlots(false), RunStore.Latest() != null);
            ArtButton("Settings", 2, () => ShowSettings(ShowTitle));
            ArtButton("Quit", 3, () => ConfirmQuit(ShowTitle));
            Label(content, RunStore.Latest() == null ? "이어하기: 유효한 저장 데이터가 없습니다" : "이어하기: 최근 안전 저장 지점에서 재개", 650, 825, 620, 45, 21, TextAnchor.MiddleCenter);
            // This link also allows damaged slots to be inspected when Continue is disabled.
            Button(content, "저장 슬롯", 815, 900, 290, 55, () => ShowSlots(false));
            back = () => ConfirmQuit(ShowTitle);
        }
        public void ShowPause()
        {
            var hud = FindFirstObjectByType<GameplayHud>();
            if (hud == null) return;
            bool dead = PlayerHealth.Instance != null && !PlayerHealth.Instance.IsAlive;
            hud.SetPaused(true); Time.timeScale = 0;
            NewPage("pause", false);
            Image(content, "Pause Shade", 0, 0, 1920, 1080, new Color(0, 0, 0, 0.8f));
            var p = Panel(dead ? "플레이어 HP 0" : "일시정지");
            Label(p, RunSession.SaveSummary, 50, 115, 1100, 160, 26);
            Button(p, "계속하기", 350, 300, 500, 75, ResumeGame, !dead);
            Button(p, "설정", 350, 400, 500, 75, () => ShowSettings(ShowPause));
            Button(p, "타이틀로", 350, 500, 500, 75, () => Confirm("타이틀로 이동할까요?\n최근 안전 저장 이후 전투 진행은 저장되지 않습니다.", () => Load("Title")));
            Button(p, "게임 종료", 350, 600, 500, 75, () => ConfirmQuit(ShowPause));
            if (!string.IsNullOrEmpty(RunSession.Active?.SaveError))
            {
                Label(p, "저장 실패: " + RunSession.Active.SaveError, 50, 700, 790, 95, 21);
                Button(p, "저장 재시도", 865, 725, 270, 60, () =>
                {
                    if (!RunSession.Active.SaveSafeCheckpoint("재시도 안전 구간", out var error)) Error(error);
                    else ShowPause();
                });
            }
            back = dead ? null : ResumeGame;
        }
        private void ResumeGame()
        {
            FindFirstObjectByType<GameplayHud>()?.SetPaused(false);
            if (Rushitadel.Progression.CampaignDirector.Instance != null) Rushitadel.Progression.CampaignDirector.Instance.ReopenPage();
            else NewPage("game", false);
        }
        private void ConfirmQuit(Action returnTo) => Confirm("게임을 종료하시겠습니까?" +
            (SceneManager.GetActiveScene().name == "inGame" ? "\n최근 안전 저장 이후 진행은 저장되지 않습니다.\n" + RunSession.SaveSummary : ""), Quit, returnTo, "종료", "취소");
        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        private void Load(string scene)
        {
            if (loading) return;
            StartCoroutine(TransitionTo(scene));
        }
    }
}
