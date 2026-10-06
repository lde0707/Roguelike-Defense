using Rushitadel.Player;
using Rushitadel.Power;
using Rushitadel.Progression;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Rushitadel.World;

namespace Rushitadel.Presentation
{
    public sealed class GameplayHud : MonoBehaviour
    {
        private PlayerHealth health;
        private PlayerPower power;
        private PlayerExperience experience;
        public bool Paused { get; private set; }
        private void Awake()
        {
            Time.timeScale = GameFrontend.IsTransitioning ? 0f : 1f;
            health = FindFirstObjectByType<PlayerHealth>();
            power = FindFirstObjectByType<PlayerPower>();
            experience = FindFirstObjectByType<PlayerExperience>();
        }
        private void Update()
        {
            if (GameFrontend.Instance == null && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SetPaused(!Paused);
        }
        public void SetPaused(bool value)
        {
            if (health != null && !health.IsAlive) return;
            Paused = value;
            Time.timeScale = value || GameFrontend.IsTransitioning ? 0f : 1f;
        }
        private void OnDestroy() => Time.timeScale = GameFrontend.IsTransitioning ? 0f : 1f;
        private void OnGUI()
        {
            if (CampaignDirector.Instance != null) return;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
            GUI.Box(new Rect(16, 16, 330, 186), "RUSHITADEL - COMBAT PROTOTYPE");
            if (health != null)
            {
                GUI.Label(new Rect(30, 44, 300, 24), $"HP  {health.CurrentHealth:0} / {health.MaximumHealth:0}");
                GUI.Box(new Rect(30, 68, 300 * health.NormalizedHealth, 8), "");
            }
            if (power != null)
                GUI.Label(new Rect(30, 80, 300, 24), $"POWER  {power.CurrentPower:0} / {power.MaximumPower:0}   {power.State}");
            if (experience != null)
                GUI.Label(new Rect(30, 108, 300, 24), $"XP  {experience.CurrentGaugeExperience} / {experience.ExperiencePerThreshold}   TOTAL {experience.TotalExperience}");
            GUI.Label(new Rect(30, 136, 300, 24), "ATTACHED TURRETS:  T1  T2  T3  T4  T5");
            if (power != null)
                GUI.Label(new Rect(30, 164, 310, 24), $"RMB: {power.MaximumPower * 0.7f:0} POWER (70%) / INNER 30% KILL");
            GUI.Box(new Rect(125, 675, 1030, 30), "WASD: Move   Left click: Aim/fire   Turrets: Forward (3 units)   Right click: Area shot + recoil   ESC: Pause");
            var pressure = ForwardPressureController.Instance;
            if (pressure != null)
            {
                GUI.Box(new Rect(420, 12, 440, 28), $"ADVANCE {pressure.DistanceTravelled:0.0}m    FRONT BARRIER - NO PASSAGE");
                Color previous = GUI.color;
                GUI.color = pressure.InDanger ? new Color(1f, 0.45f, 0.3f) : new Color(0.9f, 0.8f, 1f);
                GUI.Box(new Rect(400, 627, 480, 30), pressure.InDanger ?
                    "DANGER - MOVE FORWARD!  Heavy damage every second" :
                    ThemePressureVisual.HazardName(pressure.Theme) + " BEHIND YOU - KEEP MOVING");
                GUI.color = previous;
            }
            bool defeated = health != null && !health.IsAlive;
            if (GameFrontend.Instance != null)
            {
                if (defeated && GameFrontend.Instance.CurrentPage == "game") GameFrontend.Instance.ShowPause();
                return;
            }
            if (!Paused && !defeated) return;
            GUI.Box(new Rect(470, 245, 340, 220), defeated ? "DEFEAT - PLAYER HP 0" : "PAUSED");
            if (!defeated && GUI.Button(new Rect(510, 290, 260, 40), "Resume")) SetPaused(false);
            if (GUI.Button(new Rect(510, 345, 260, 40), "Restart")) SceneManager.LoadScene("inGame");
            if (GUI.Button(new Rect(510, 400, 260, 40), "Title")) SceneManager.LoadScene("Title");
        }
    }
}
