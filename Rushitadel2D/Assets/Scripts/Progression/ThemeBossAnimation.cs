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
        private float actionAge,windup,hold,recovery,peak,end;
        private bool action;
        public bool IsActionPlaying=>action;
        // The middle of A/C/D is the extended pose. B's first attack frame is the upright ring.
        public void Cue(float chargeSeconds,float holdSeconds,float recoverySeconds,float peakFrame=.5f,float endFrame=1)
        {
            windup=Mathf.Max(.01f,chargeSeconds);hold=Mathf.Max(0,holdSeconds);recovery=Mathf.Max(.01f,recoverySeconds);
            peak=Mathf.Clamp01(peakFrame);end=Mathf.Clamp01(endFrame);actionAge=0;action=true;
            ApplyAction();
        }
        private void ApplyAction()
        {
            if(clip==null||clip.idle.Length==0)return;
            var frames=clip.attack.Length>0?clip.attack:clip.idle;
            float frame=actionAge<windup?Mathf.Lerp(0,peak,actionAge/windup):
                actionAge<windup+hold?peak:Mathf.Lerp(peak,end,(actionAge-windup-hold)/recovery);
            SetFrame(frames[Mathf.Clamp(Mathf.RoundToInt(frame*(frames.Length-1)),0,frames.Length-1)]);
        }
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
            if(action)
            {
                actionAge+=Time.deltaTime;ApplyAction();
                if(actionAge>=windup+hold+recovery){action=false;age=0;}
                return;
            }
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
