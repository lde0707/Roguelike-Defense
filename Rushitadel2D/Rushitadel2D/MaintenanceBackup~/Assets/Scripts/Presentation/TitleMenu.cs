using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Rushitadel.Presentation
{
    public sealed class TitleMenu : MonoBehaviour
    {
        private bool showSettings;
        private void Awake()
        {
            Time.timeScale = 1f;
            foreach (Button button in FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                switch (button.name)
                {
                    case "NewGameButton": button.onClick.AddListener(() => SceneManager.LoadScene("inGame")); break;
                    // The original project has no save/load implementation.
                    case "ContinueButton": button.interactable = false; break;
                    case "SettingsButton": button.onClick.AddListener(() => showSettings = !showSettings); break;
                    case "QuitButton": button.onClick.AddListener(Quit); break;
                }
            }
            Font font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 24);
            foreach (Text label in FindObjectsByType<Text>(FindObjectsSortMode.None)) label.font = font;
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnGUI()
        {
            if (!showSettings) return;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
            GUI.Box(new Rect(400, 200, 480, 320), "SETTINGS / CONTROLS");
            GUI.Label(new Rect(430, 245, 420, 30), "WASD: Move | Left click: Fire | Right click: Power shot");
            GUI.Label(new Rect(430, 280, 420, 30), "ESC: Pause / Resume");
            GUI.Label(new Rect(430, 315, 420, 30), "Volume");
            AudioListener.volume = GUI.HorizontalSlider(new Rect(430, 355, 420, 24), AudioListener.volume, 0f, 1f);
            if (GUI.Button(new Rect(530, 415, 220, 44), "Close")) showSettings = false;
        }
    }
}
