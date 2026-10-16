using UnityEngine;

namespace Rushitadel.World
{
    /// <summary>World-fixed tiles recycled beyond the view; camera motion supplies the downward screen scroll.</summary>
    public sealed class LoopingBattlefield : MonoBehaviour
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private SpriteRenderer[] panels;
        [SerializeField, Min(0.1f)] private float panelHeight = 10.8f;
        public SpriteRenderer[] Panels => panels;
        public float PanelHeight => panelHeight;
        private int currentTheme=-1;
        private readonly Sprite[] themeSprites=new Sprite[4];
        public void Configure(Camera camera, SpriteRenderer[] tiles, float height)
        {
            gameplayCamera = camera;
            panels = tiles;
            panelHeight = height;
            RefreshLayout();
        }
        private void LateUpdate()
        {
            var pressure=ForwardPressureController.Instance;
            if(pressure!=null)
            {
                int theme=Mathf.Min((int)pressure.Theme,3);
                if(theme!=currentTheme && panels!=null)
                {
                    if(themeSprites[theme]==null)
                    {
                        themeSprites[theme]=ThemeContent.Map(pressure.Theme);
                    }
                    if(themeSprites[theme]!=null)foreach(var panel in panels)
                    {
                        panelHeight=12.8f;
                        panel.sprite=themeSprites[theme];panel.color=Color.white;
                        panel.transform.localScale=new Vector3(19.2f/panel.sprite.bounds.size.x,panelHeight/panel.sprite.bounds.size.y,1);
                    }
                    currentTheme=theme;
                }
            }
            RefreshLayout();
        }
        public void RefreshLayout()
        {
            if (gameplayCamera == null || panels == null) return;
            float bottom = gameplayCamera.transform.position.y - gameplayCamera.orthographicSize;
            float first = Mathf.Floor((bottom + panelHeight * 0.5f - transform.position.y) / panelHeight) * panelHeight;
            for (int i = 0; i < panels.Length; i++)
                panels[i].transform.localPosition = new Vector3(0f, first + i * panelHeight, 0f);
        }
    }
}
