#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rushitadel.Player;
using Rushitadel.Power;
using Rushitadel.Progression;
using Rushitadel.World;
using Rushitadel.Spawning;
using Rushitadel.Combat;

namespace Rushitadel.Presentation
{
    public sealed class TitleUiProbe : MonoBehaviour
    {
        private string output;
        private bool failed;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../output/title-implementation-20261006"));
            RunStore.TestDirectory = Path.Combine(output, "test-saves-" + DateTime.Now.ToString("HHmmss"));
            GameSettings.StorageKey = "Rushitadel.UiTest." + Guid.NewGuid(); GameSettings.Load();
            Application.logMessageReceived += Log;
        }
        private void Log(string message, string stack, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error) return;
            if (message.StartsWith("Curl error")) return;
            failed = true; Debug.LogWarning("UI TEST ERROR: " + message);
            EditorApplication.delayCall += () => EditorApplication.Exit(1);
        }
        private static Button Find(string name) => FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == name && b.gameObject.activeInHierarchy);
        private static void Click(string name) { var b = Find(name); Check(b.interactable, "Clickable: " + name); b.onClick.Invoke(); }
        private static void Check(bool condition, string text) { if (!condition) throw new Exception("UI TEST FAILED: " + text); Debug.Log("PASS: " + text); }
        private static void Call(string method) => typeof(GameFrontend).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(GameFrontend.Instance, null);
        private IEnumerator Start()
        {
            yield return null; yield return null;
            var ui = GameFrontend.Instance; ui.ShowTitle(); yield return null;
            Check(SceneManager.GetActiveScene().name == "Title" && ui.CurrentPage == "title", "Editor Play begins at title");
            Check(!Find("ContinueButton").interactable, "Continue disabled without a valid save");
            Capture("title-default.png");
            foreach (var id in new[] {"Start", "Continue", "Settings", "Quit"})
            {
                var button = Find(id + "Button");
                Check(button.spriteState.highlightedSprite != null && button.spriteState.pressedSprite != null, id + " owns separate hover/pressed sprites");
                if (!button.interactable) continue;
                ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
                yield return null;
                Check(button.GetComponent<Image>().overrideSprite == button.spriteState.highlightedSprite, id + " hover swap");
                Capture("hover-" + id + ".png");
                ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
            }
            Click("QuitButton"); yield return null;
            Check(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.text.Contains("게임을 종료하시겠습니까?")), "Quit confirmation text");
            Capture("quit-confirm.png"); Click("취소 Button"); yield return null;
            Check(ui.CurrentPage == "title", "Quit cancellation stays on title");
            Click("SettingsButton"); yield return null; Capture("settings-display.png");
            Click("사운드 Button"); yield return null;
            var sliders = FindObjectsByType<Slider>(FindObjectsSortMode.None);
            Check(sliders.Length == 3, "Three independent audio sliders");
            var master = sliders.First(s => s.name == "전체 음량 Slider");
            float oldMaster = GameSettings.Saved.master; master.value = 33;
            Check(Mathf.Approximately(AudioListener.volume, 0.33f), "Audio previews immediately");
            Capture("settings-audio.png"); Click("취소 Button"); yield return null;
            Check(Mathf.Approximately(AudioListener.volume, oldMaster), "Cancel restores audio");
            Click("SettingsButton"); yield return null; Click("사운드 Button"); yield return null;
            FindObjectsByType<Slider>(FindObjectsSortMode.None).First(s => s.name == "전체 음량 Slider").value = 44;
            Click("적용 Button"); yield return null; GameSettings.Load();
            Check(Mathf.Approximately(GameSettings.Saved.master, 0.44f), "Settings persist and reload");
            Click("SettingsButton"); yield return null; Click("조작 Button"); yield return null; Capture("settings-controls.png");
            Click("취소 Button"); yield return null;
            Click("SettingsButton"); yield return null;
            var draft = (SettingsData)typeof(GameFrontend).GetField("draft", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            draft.mode = (draft.mode + 1) % 3; Click("적용 Button"); yield return null;
            Capture("display-confirm.png");
            typeof(GameFrontend).GetField("previewDeadline", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ui, Time.unscaledTime - 1);
            yield return null; yield return null;
            Check(GameSettings.Live.mode == GameSettings.Saved.mode && ui.CurrentPage == "settings", "Display timeout reverts");
            Click("취소 Button"); yield return null;
            Click("StartButton"); yield return null; Capture("slots-empty.png");
            var newButton = FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b => b.name == "새 게임 Button").OrderBy(b => b.transform.position.y).Last();
            newButton.onClick.Invoke(); yield return null;
            Check(!Find("선택 확정 · 시작 Button").interactable, "Theme confirmation disabled until selection");
            FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.GetComponent<RectTransform>().sizeDelta.y == 250).onClick.Invoke();
            yield return null; Capture("theme-select.png"); Click("선택 확정 · 시작 Button");
            yield return new WaitForSecondsRealtime(1);
            Check(SceneManager.GetActiveScene().name == "inGame" && PlayerHealth.Instance.IsAlive, "New run loads gameplay");
            Check(RunStore.Read(1, out var saved, out _), "Theme confirmation writes slot atomically");
            var spawner = FindFirstObjectByType<EnemySpawner>(); spawner.enabled = false;
            foreach (var enemy in EnemyRegistry.ActiveEnemies.ToArray()) Destroy(enemy.gameObject);
            foreach (var bullet in FindObjectsByType<EnemyProjectileController>(FindObjectsSortMode.None)) Destroy(bullet.gameObject);
            yield return null;
            PlayerHealth.Instance.TakeEnvironmentalDamage(25);
            PlayerHealth.Instance.GetComponent<PlayerExperience>().AddExperience(47);
            Check(RunSession.Active.SaveSafeCheckpoint("검증 안전 구간", out string error), "Safe checkpoint saves live player data: " + error);
            Check(File.Exists(RunStore.PathFor(1) + ".bak"), "Previous good save backup retained");
            ui.ShowPause(); yield return null; Capture("pause.png");
            Check(Time.timeScale == 0, "Pause freezes simulation");
            Click("설정 Button"); yield return null; Click("취소 Button"); yield return null;
            Check(ui.CurrentPage == "pause" && Time.timeScale == 0, "Settings returns to paused game");
            Click("타이틀로 Button"); yield return null; Click("확인 Button"); yield return new WaitForSecondsRealtime(0.5f);
            Check(Find("ContinueButton").interactable, "Continue enabled with valid checkpoint");
            ExecuteEvents.Execute(Find("ContinueButton").gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            yield return null; Capture("hover-Continue.png");
            Click("ContinueButton"); yield return null; Capture("slots-saved.png");
            Click("불러오기 Button"); yield return new WaitForSecondsRealtime(0.4f);
            Check(PlayerHealth.Instance.CurrentHealth == 75 && PlayerHealth.Instance.GetComponent<PlayerExperience>().TotalExperience == 47, "Continue restores HP and XP");
            ui.ShowPause(); yield return null;
            SceneManager.LoadScene("Title"); yield return new WaitForSecondsRealtime(0.4f);
            File.WriteAllText(RunStore.PathFor(2), "{broken");
            Check(!RunStore.Read(2, out _, out _) && File.Exists(RunStore.PathFor(2)), "Corrupt data rejected without deleting original");
            var invalid = JsonUtility.FromJson<RunData>(JsonUtility.ToJson(saved)); invalid.version = 99;
            Check(!RunStore.Validate(invalid, 1, out _), "Unsupported schema rejected");
            invalid.version = 1; invalid.health = float.NaN;
            Check(!RunStore.Validate(invalid, 1, out _), "Non-finite save values rejected");
            ui.ShowSlots(false); yield return null; Capture("slots-corrupt.png");
            ui.ShowTitle(); yield return null;
            Click("StartButton"); yield return null;
            FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b => b.name == "새 게임 Button").OrderByDescending(b => b.transform.position.y).First().onClick.Invoke();
            yield return null;
            Check(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.text.Contains("덮어쓸까요")), "Overwrite asks before replacing slot");
            Click("취소 Button"); yield return null;
            Check(RunStore.Read(1, out _, out _), "Overwrite cancellation preserves slot");
            Check(!failed, "No UI runtime errors");
            PlayerPrefs.DeleteKey(GameSettings.StorageKey); PlayerPrefs.Save();
            Debug.Log("RUSHITADEL_TITLE_UI_VALIDATION_SUCCESS"); EditorApplication.Exit(0);
        }
        private void Capture(string filename)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var canvas = GameFrontend.Instance.GetComponentInChildren<Canvas>();
            var camera = Camera.main;
            if (camera == null) return;
            var target = new RenderTexture(1920, 1080, 24);
            var oldTarget = camera.targetTexture;
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            var previous = RenderTexture.active;
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest {destination = target});
            RenderTexture.active = target;
            var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0,0,1920,1080),0,0); texture.Apply();
            File.WriteAllBytes(Path.Combine(output, filename), texture.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = oldTarget; canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Destroy(texture); Destroy(target);
        }
    }
}
#endif
