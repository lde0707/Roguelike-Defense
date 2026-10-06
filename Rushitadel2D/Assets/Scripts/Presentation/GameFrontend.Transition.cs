using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rushitadel.Progression;

namespace Rushitadel.Presentation
{
    public sealed partial class GameFrontend
    {
        public static bool IsTransitioning => Instance != null && Instance.transitioning;
        private bool transitioning;
        private Canvas shutterCanvas;
        private RectTransform[] shutterLeft, shutterRight;
        private Text shutterStatus;
        private float shutterAmount;
        private const int ShutterRows = 24;

        private void CreateShutter()
        {
            var go = new GameObject("Dot Shutter", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            shutterCanvas = go.GetComponent<Canvas>();
            shutterCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            shutterCanvas.sortingOrder = 2000; shutterCanvas.pixelPerfect = true;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            // Transparent blocker catches input even while most of the screen is uncovered.
            var blocker = Image(go.transform, "Transition Input Blocker", 0, 0, 1920, 1080, Color.clear);
            blocker.material = null;
            blocker.rectTransform.anchorMin = Vector2.zero; blocker.rectTransform.anchorMax = Vector2.one;
            blocker.rectTransform.offsetMin = blocker.rectTransform.offsetMax = Vector2.zero;
            shutterLeft = new RectTransform[ShutterRows]; shutterRight = new RectTransform[ShutterRows];
            for (int i = 0; i < ShutterRows; i++)
            {
                var color = i % 4 == 0 ? new Color32(19, 25, 39, 255) : new Color32(17, 23, 36, 255);
                foreach (bool right in new[] {false, true})
                {
                    var tile = Image(go.transform, (right ? "Right " : "Left ") + i, 0, 0, 0, 0, color);
                    tile.material = null; tile.raycastTarget = false;
                    var rect = tile.rectTransform;
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(right ? 1 : 0, 1);
                    if (right) shutterRight[i] = rect; else shutterLeft[i] = rect;
                }
            }
            shutterStatus = Label(go.transform, "출격 준비 중", 0, 0, 640, 64, 28, TextAnchor.MiddleCenter);
            shutterStatus.material = null; shutterStatus.color = new Color32(166, 177, 175, 255);
            CenterPage(shutterStatus.rectTransform); shutterStatus.gameObject.SetActive(false);
            Canvas.ForceUpdateCanvases(); SetShutter(0);
        }

        private void SetShutter(float amount)
        {
            shutterAmount = amount;
            var size = ((RectTransform)shutterCanvas.transform).rect.size;
            float height = size.y / ShutterRows;
            for (int i = 0; i < ShutterRows; i++)
            {
                // A symmetric wave with small deterministic offsets; never consumes gameplay RNG.
                float delay = Mathf.Abs(i - (ShutterRows - 1) * .5f) / ShutterRows * .16f + (i % 3) * .015f;
                float t = Mathf.Clamp01((amount - delay) / (1 - delay));
                t = t * t * (3 - 2 * t);
                float joint = (i % 2 == 0 ? 1 : -1) * 12;
                float leftWidth = amount >= 1 ? size.x * .5f + joint + 1 : Mathf.Floor((size.x * .5f + joint + 1) * t / 8) * 8;
                float rightWidth = amount >= 1 ? size.x * .5f - joint + 1 : Mathf.Floor((size.x * .5f - joint + 1) * t / 8) * 8;
                shutterLeft[i].anchoredPosition = shutterRight[i].anchoredPosition = new Vector2(0, -Mathf.Floor(i * height));
                float rowHeight = Mathf.Ceil((i + 1) * height) - Mathf.Floor(i * height) + 1;
                shutterLeft[i].sizeDelta = new Vector2(leftWidth, rowHeight);
                shutterRight[i].sizeDelta = new Vector2(rightWidth, rowHeight);
            }
        }

        private IEnumerator AnimateShutter(bool closing)
        {
            float duration = closing ? .30f : .42f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                SetShutter(closing ? elapsed / duration : 1 - elapsed / duration);
                yield return null;
            }
            SetShutter(closing ? 1 : 0);
        }

        private IEnumerator TransitionTo(string scene)
        {
            transitioning = true; loading = true; Time.timeScale = 0;
            SetTitleCursor(Vector2.zero, false);
            CreateShutter();
            yield return AnimateShutter(true);
            // Let the completely opaque frame reach the display before changing scenes.
            yield return null;
            AsyncOperation operation = null;
            string failure = null;
            try { operation = SceneManager.LoadSceneAsync(scene); }
            catch (System.Exception ex) { failure = ex.Message; }
            if (operation == null)
            {
                yield return AnimateShutter(false);
                EndShutter();
                Error("화면을 불러오지 못했습니다.\n" + failure);
                yield break;
            }
            float waiting = 0;
            while (!operation.isDone)
            {
                waiting += Time.unscaledDeltaTime;
                if (waiting > .5f)
                {
                    shutterStatus.gameObject.SetActive(true);
                    shutterStatus.text = (scene == "inGame" ? "출격 준비 중" : "타이틀로 이동 중") + new string('.', 1 + (int)(waiting * 3) % 3);
                }
                SetShutter(1); yield return null;
            }
            // Scene-loaded callbacks precede Start; wait for the session/build/HUD restore.
            yield return null;
            float readyDeadline = Time.realtimeSinceStartup + 10;
            while (scene == "inGame" && (RunSession.Active == null || CampaignDirector.Instance == null || !CampaignDirector.Instance.Initialized))
            {
                if (Time.realtimeSinceStartup > readyDeadline) { failure = "전투 초기화에 실패했습니다. 타이틀로 돌아가 다시 시도해 주세요."; break; }
                yield return null;
            }
            shutterStatus.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(.08f);
            yield return AnimateShutter(false);
            EndShutter();
            if (failure != null) { Time.timeScale = 0; Error(failure, () => Load("Title")); }
        }

        private void EndShutter()
        {
            if (shutterCanvas != null) { shutterCanvas.gameObject.SetActive(false); Destroy(shutterCanvas.gameObject); }
            shutterCanvas = null; transitioning = false; loading = false;
            if (SceneManager.GetActiveScene().name == "inGame") CampaignDirector.Instance?.ReopenPage();
            else Time.timeScale = 1;
        }
    }
}
