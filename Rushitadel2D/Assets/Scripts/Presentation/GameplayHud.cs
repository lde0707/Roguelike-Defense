using Rushitadel.Power;
using Rushitadel.Progression;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Rushitadel.Presentation
{
    public sealed class GameplayHud : MonoBehaviour
    {
        private PlayerPower power;
        private PlayerExperience experience;
        private bool paused;

        private void Awake()
        {
            Time.timeScale = 1f;
            power = FindFirstObjectByType<PlayerPower>();
            experience = FindFirstObjectByType<PlayerExperience>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                paused = !paused;
                Time.timeScale = paused ? 0f : 1f;
            }
        }

        private void OnDestroy() => Time.timeScale = 1f;

        private void OnGUI()
        {
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
            GUI.Box(new Rect(16, 16, 340, 114), "RUSHITADEL 2D");
            if (power != null)
            {
                GUI.Label(new Rect(30, 44, 320, 24), $"POWER  {power.CurrentPower:0} / {power.MaximumPower:0}    {power.State}");
                GUI.Box(new Rect(30, 70, 300 * power.NormalizedPower, 8), "");
            }
            if (experience != null)
                GUI.Label(new Rect(30, 86, 320, 26), $"EXP  {experience.CurrentGaugeExperience} / {experience.ExperiencePerThreshold}    TOTAL {experience.TotalExperience}");
            GUI.Box(new Rect(260, 675, 760, 30), "WASD: Move     Left click: Fire     Right click: Power shot (20)     ESC: Pause");
            if (!paused) return;
            GUI.Box(new Rect(470, 245, 340, 220), "PAUSED");
            if (GUI.Button(new Rect(510, 290, 260, 40), "Resume")) { paused = false; Time.timeScale = 1f; }
            if (GUI.Button(new Rect(510, 345, 260, 40), "Restart")) SceneManager.LoadScene("inGame");
            if (GUI.Button(new Rect(510, 400, 260, 40), "Title")) SceneManager.LoadScene("Title");
        }
    }
}
