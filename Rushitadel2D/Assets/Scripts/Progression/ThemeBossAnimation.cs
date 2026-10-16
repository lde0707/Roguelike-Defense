using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Rushitadel.Progression
{
    public sealed class ThemeBossAnimation : MonoBehaviour
    {
        private sealed class Clip { public Sprite[] idle,attack; }
        private static readonly Dictionary<string,Clip> cache=new();
        private Clip clip;
        private SpriteRenderer renderer2D;
        private CampaignBoss owner;
        private bool wasAttacking;
        private float age;
        private MaterialPropertyBlock properties;
        private void SetFrame(Sprite sprite)
        {
            renderer2D.sprite=sprite;
            // Explicit per-renderer binding prevents the shared material from showing another boss's frame.
            properties??=new MaterialPropertyBlock();
            renderer2D.GetPropertyBlock(properties);
            properties.SetTexture("_MainTex",sprite.texture);
            renderer2D.SetPropertyBlock(properties);
        }
        public int IdleFrameCount=>clip?.idle.Length??0;
        public int AttackFrameCount=>clip?.attack.Length??0;
        public void Configure(CampaignBoss boss,SpriteRenderer target,string type,string idlePrefix,string attackPrefix,float width)
        {
            owner=boss;renderer2D=target;clip=Load(type,idlePrefix,attackPrefix);
            if(clip.idle.Length==0)return;
            SetFrame(clip.idle[0]);renderer2D.color=Color.white;
            renderer2D.transform.localScale=Vector3.one*(width/renderer2D.sprite.bounds.size.x);
        }
        private void Update()
        {
            if(clip==null||clip.idle.Length==0||renderer2D==null)return;
            bool attack=owner!=null&&owner.IsAttacking&&clip.attack.Length>0;
            if(attack!=wasAttacking){age=0;wasAttacking=attack;}
            age+=Time.deltaTime;var frames=attack?clip.attack:clip.idle;
            SetFrame(frames[(int)(age*10)%frames.Length]);
        }
        private static Clip Load(string type,string idlePrefix,string attackPrefix)
        {
            string key=type+idlePrefix+attackPrefix;if(cache.TryGetValue(key,out var existing))return existing;
            var all=Resources.LoadAll<Texture2D>("ThemeContent/"+type+"/Boss");
            Texture2D[] Frames(string prefix)=>string.IsNullOrEmpty(prefix)?Array.Empty<Texture2D>():all.Where(t=>t.name.StartsWith(prefix)&&int.TryParse(t.name.Substring(prefix.Length),out _)).OrderBy(t=>int.Parse(t.name.Substring(prefix.Length))).ToArray();
            var idle=Frames(idlePrefix);var attack=Frames(attackPrefix);
            int left=int.MaxValue,right=-1,bottom=int.MaxValue,top=-1;
            foreach(var texture in idle.Concat(attack))
            {
                var pixels=texture.GetPixels32();
                for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>100)
                {left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
            }
            var clip=new Clip{idle=Array.Empty<Sprite>(),attack=Array.Empty<Sprite>()};
            if(right<left){Debug.LogError("Missing boss animation: "+key);return cache[key]=clip;}
            // A single shared crop/pivot for every frame avoids animation jitter and scale pumping.
            int width=right-left+1,height=top-bottom+1;
            Sprite[] Sprites(Texture2D[] textures)=>textures.Select(t=>
            {
                var cropped=new Texture2D(width,height,TextureFormat.RGBA32,false)
                {name=t.name,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
                cropped.SetPixels(t.GetPixels(left,bottom,width,height));cropped.Apply(false,true);
                return Sprite.Create(cropped,new Rect(0,0,width,height),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);
            }).ToArray();
            clip.idle=Sprites(idle);clip.attack=Sprites(attack);return cache[key]=clip;
        }
    }
}
