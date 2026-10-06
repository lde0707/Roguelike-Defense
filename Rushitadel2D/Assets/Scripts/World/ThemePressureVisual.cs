using UnityEngine;

namespace Rushitadel.World
{
    public enum PressureTheme { Grass, Fire, Ice, Water, Electric }

    /// <summary>Replaceable prototype silhouettes, not final environment VFX.</summary>
    public sealed class ThemePressureVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer fill;
        [SerializeField] private SpriteRenderer[] accents;
        [SerializeField] private Sprite square;
        [SerializeField] private Sprite circle;
        [SerializeField] private bool rear;
        private Color accentColor;
        private PressureTheme theme;
        public void Configure(SpriteRenderer background, SpriteRenderer[] details, Sprite quad, Sprite round, bool isRear)
        { fill = background; accents = details; square = quad; circle = round; rear = isRear; }

        public void SetSize(Vector2 size)
        {
            fill.transform.localScale = new Vector3(size.x / fill.sprite.bounds.size.x, size.y / fill.sprite.bounds.size.y, 1f);
            for (int i = 0; i < accents.Length; i++)
                accents[i].transform.localPosition = new Vector3(-size.x * 0.5f + (i + 0.5f) * size.x / accents.Length,
                    rear ? size.y * 0.5f - 0.22f : 0f, 0f);
        }

        public static string HazardName(PressureTheme value) => value switch
        {
            PressureTheme.Grass => "TOXIC MIST", PressureTheme.Fire => "LAVA SURGE",
            PressureTheme.Ice => "BLIZZARD", PressureTheme.Water => "TIDAL WAVE", _ => "ELECTRIC STORM"
        };

        public void ApplyTheme(PressureTheme value)
        {
            theme = value;
            Color tint = value switch
            {
                PressureTheme.Grass => new Color(0.45f, 0.8f, 0.12f),
                PressureTheme.Fire => new Color(1f, 0.3f, 0.06f),
                PressureTheme.Ice => new Color(0.7f, 0.9f, 1f),
                PressureTheme.Water => new Color(0.1f, 0.65f, 0.9f),
                _ => new Color(0.85f, 0.7f, 1f)
            };
            fill.color = rear ? new Color(tint.r * 0.5f, tint.g * 0.5f, tint.b * 0.5f, 0.85f) :
                new Color(tint.r * 0.3f, tint.g * 0.3f, tint.b * 0.3f, 1f);
            accentColor = tint;
            foreach (SpriteRenderer detail in accents)
            {
                detail.color = tint;
                bool rounded = value == PressureTheme.Water || value == PressureTheme.Grass;
                detail.sprite = rounded ? circle : square;
                detail.transform.localRotation = Quaternion.Euler(0f, 0f, rounded ? 0f : 35f);
                detail.transform.localScale = value switch
                {
                    PressureTheme.Water => new Vector3(0.65f, 0.2f, 1f),
                    PressureTheme.Grass => new Vector3(0.55f, 0.3f, 1f),
                    PressureTheme.Ice => new Vector3(0.22f, 0.36f, 1f),
                    PressureTheme.Fire => new Vector3(0.3f, 0.3f, 1f),
                    _ => new Vector3(0.55f, 0.07f, 1f)
                };
            }
        }

        private void Start() => ApplyTheme(ForwardPressureController.Instance != null ? ForwardPressureController.Instance.Theme : theme);
        private void Update()
        {
            if (!rear || Time.timeScale <= 0f) return;
            for (int i = 0; i < accents.Length; i++)
            {
                Color tint = accentColor;
                tint.a = 0.65f + 0.35f * Mathf.Sin(Time.time * 2f + i * 0.6f);
                accents[i].color = tint;
            }
        }
    }
}
