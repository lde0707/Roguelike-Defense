using System.Collections.Generic;
using UnityEngine;

namespace Rushitadel.World
{
    public enum PressureTheme { Grass, Fire, Ice, Water, Electric }
    public sealed class ThemePressureVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer fill;
        [SerializeField] private SpriteRenderer[] accents;
        [SerializeField] private Sprite square;
        [SerializeField] private Sprite circle;
        [SerializeField] private bool rear;
        private readonly SpriteRenderer[] tiles=new SpriteRenderer[3];
        private static readonly Dictionary<string,Sprite[]> frames=new();
        private static readonly Dictionary<Texture2D,Material> materials=new();
        private Sprite[] sequence;
        private Vector2 size;
        private float elapsed;
        public int FrameIndex {get;private set;}
        public int FrameCount=>sequence?.Length??0;
        public void Configure(SpriteRenderer background,SpriteRenderer[] details,Sprite quad,Sprite round,bool isRear)
        {fill=background;accents=details;square=quad;circle=round;rear=isRear;}
        public static string ThemeFolder(PressureTheme value)=>value==PressureTheme.Grass?"Grass":value==PressureTheme.Fire?"Fire":value==PressureTheme.Ice?"Ice":"WaterElectric";
        public static string HazardName(PressureTheme value)=>value switch
        {PressureTheme.Grass=>"CREEPING VINES",PressureTheme.Fire=>"LAVA SURGE",PressureTheme.Ice=>"FREEZING GROUND",_=>"ELECTRIC TSUNAMI"};
        public void SetSize(Vector2 value){size=value;Layout();}
        public void ApplyTheme(PressureTheme value)
        {
            if(!rear)return;
            if(fill!=null)fill.enabled=false;
            if(accents!=null)foreach(var accent in accents)if(accent!=null)accent.enabled=false;
            string folder=ThemeFolder(value);
            if(!frames.TryGetValue(folder,out sequence))
            {
                sequence=new Sprite[12];
                for(int i=0;i<12;i++)
                {
                    var tex=Resources.Load<Texture2D>("PressureArt/"+folder+"/"+i.ToString("00"));
                    if(tex==null){Debug.LogError("Missing pressure frame "+folder+"/"+i);continue;}
                    sequence[i]=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),Vector2.one*.5f,64,0,SpriteMeshType.FullRect);
                }
                frames[folder]=sequence;
            }
            for(int i=0;i<tiles.Length;i++)if(tiles[i]==null)
            {
                var go=new GameObject("Ground pursuit tile "+i);go.transform.SetParent(transform,false);
                tiles[i]=go.AddComponent<SpriteRenderer>();tiles[i].sortingOrder=40;
            }
            elapsed=0;FrameIndex=0;Present();
        }
        private void Layout()
        {
            for(int i=0;i<tiles.Length;i++)
            {
                var sr=tiles[i];if(sr==null||sr.sprite==null)continue;
                sr.transform.localPosition=new Vector3(-size.x*.5f+size.x/3*(i+.5f),0,0);
                sr.transform.localScale=new Vector3((size.x/3+.015f)/sr.sprite.bounds.size.x,size.y/sr.sprite.bounds.size.y,1);
            }
        }
        private void Present()
        {
            if(sequence==null)return;
            for(int i=0;i<tiles.Length;i++)
            {
                var sr=tiles[i];sr.sprite=sequence[(FrameIndex+i*4)%12];if(sr.sprite==null)continue;
                var texture=sr.sprite.texture;
                if(!materials.TryGetValue(texture,out var material))materials[texture]=material=new Material(Shader.Find("Sprites/Default")){mainTexture=texture};
                sr.sharedMaterial=material;
            }
            Layout();
        }
        private void Start(){if(rear)ApplyTheme(ForwardPressureController.Instance!=null?ForwardPressureController.Instance.Theme:PressureTheme.Grass);}
        private void Update()
        {
            if(!rear||sequence==null||Time.timeScale<=0)return;
            elapsed+=Time.deltaTime;int next=(int)(elapsed*10)%12;
            if(next==FrameIndex)return;FrameIndex=next;Present();
        }
    }
}
