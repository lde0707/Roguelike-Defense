using System.Collections.Generic;
using UnityEngine;
using Rushitadel.World;

namespace Rushitadel.Combat
{
    public sealed class EnemyArchetype : MonoBehaviour
    {
        public int Variant {get;private set;}
        public int Stage {get;private set;}
        public bool Elite=>Variant>=5;
        public float BulletHealth=24;
        public string ProjectileAsset {get;private set;}
        public static string ThemeFolder(PressureTheme theme)=>theme switch {
            PressureTheme.Grass=>"Grass",PressureTheme.Fire=>"Lava",PressureTheme.Ice=>"Ice",_=>"WaterElectric"};
        private static readonly Dictionary<string,Sprite> sprites=new();
        private static readonly Dictionary<Texture2D,Material> materials=new();
        public static Sprite Load(PressureTheme theme,int variant)
        {
            string path=ThemeFolder(theme)+"/"+(variant<5?"N0"+(variant+1):"E0"+(variant-4));
            if(sprites.TryGetValue(path,out var sprite)&&sprite!=null)return sprite;
            var tex=Resources.Load<Texture2D>("EnemyArt/"+path);
            if(tex==null){Debug.LogError("Missing enemy art "+path);return null;}
            return sprites[path]=Sprite.Create(tex,new Rect(0,0,32,32),Vector2.one*.5f,32);
        }
        public void Configure(int variant,int stage,PressureTheme theme)
        {
            Variant=Mathf.Clamp(variant,0,6);Stage=Mathf.Clamp(stage,1,3);
            float weight=Variant==2?1.35f:Variant==3?.9f:1;
            float hp=(Elite?600+180*(Stage-1):180+60*(Stage-1))*weight;
            GetComponent<EnemyController>().ConfigureRun(hp,Elite?35:20,true);
            BulletHealth=(Elite?70+20*(Stage-1):24+8*(Stage-1))*(Variant==2?1.6f:1);
            string[] prefix={"Butterfly/01-pollen-pearl","Lava/01-ember-orb","Ice/03-ricochet-orb","Twins/01-pressure-orb"};
            ProjectileAsset=prefix[theme==PressureTheme.Grass?0:theme==PressureTheme.Fire?1:theme==PressureTheme.Ice?2:3];
            transform.localScale=Vector3.one*(Elite?1.22f:1f);
            var sr=GetComponent<SpriteRenderer>();sr.sprite=Load(theme,Variant);sr.color=Color.white;sr.sortingOrder=100;
            var texture=sr.sprite.texture;
            if(!materials.TryGetValue(texture,out var material))
                materials[texture]=material=new Material(Shader.Find("Sprites/Default")){mainTexture=texture};
            sr.sharedMaterial=material;
            foreach(var c in GetComponents<Collider2D>())c.enabled=false;
            var body=gameObject.AddComponent<BoxCollider2D>();body.size=new Vector2(.75f,.78f);body.isTrigger=true;
            gameObject.name=ThemeFolder(theme)+" "+(Elite?"Elite ":"Ranged ")+Variant;
        }
    }
}
