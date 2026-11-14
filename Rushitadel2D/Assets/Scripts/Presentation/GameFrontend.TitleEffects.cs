using UnityEngine;
using UnityEngine.InputSystem;

namespace Rushitadel.Presentation
{
    public sealed partial class GameFrontend
    {
        private Material titleCursorMaterial, titleBackgroundMaterial;
        private float titleScroll;
        private const float TitleScrollPixelsPerSecond = 18f;
        private const float CursorRadius = 36f;

        private void EnsureTitleMaterials()
        {
            if (titleCursorMaterial != null) return;
            var shader = Resources.Load<Shader>("TitleUI/TitleEffects");
            if (shader == null || !shader.isSupported) return;
            titleCursorMaterial = new Material(shader) {name = "Title Cursor Inversion"};
            titleBackgroundMaterial = new Material(shader) {name = "Title Background Scroll"};
            titleBackgroundMaterial.SetFloat("_Scrolling", 1);
        }

        private void LateUpdate()
        {
            if(font != null && font.material.mainTexture != null) font.material.mainTexture.filterMode = FilterMode.Point;
            if (titleBackgroundMaterial != null && canvas != null &&
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Title")
            {
                titleScroll = Mathf.Repeat(titleScroll + Time.unscaledDeltaTime * TitleScrollPixelsPerSecond / 1080f, 2f);
                titleBackgroundMaterial.SetFloat("_Scroll", Mathf.Floor(titleScroll * 1080f) / 1080f);
            }
            SetTitleCursor(Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero,
                Mouse.current != null && Application.isFocused);
        }

        private void SetTitleCursor(Vector2 screenPosition, bool visible)
        {
            if (titleCursorMaterial == null || canvas == null) return;
            var rect = (RectTransform)canvas.transform;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            bool inside = RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, camera);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPosition, camera, out var point);
            titleCursorMaterial.SetMatrix("_WorldToCanvas", rect.worldToLocalMatrix);
            titleCursorMaterial.SetVector("_CursorLens", new Vector4(point.x, point.y, CursorRadius,
                visible && inside && page == "title" && modal == null && !transitioning ? 1 : 0));
        }

        private void DisposeTitleMaterials()
        {
            if (titleCursorMaterial != null) Destroy(titleCursorMaterial);
            if (titleBackgroundMaterial != null) Destroy(titleBackgroundMaterial);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) SetTitleCursor(Vector2.zero, false);
        }
    }
}
