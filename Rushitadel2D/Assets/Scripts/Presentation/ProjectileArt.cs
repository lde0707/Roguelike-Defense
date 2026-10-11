using System.Collections.Generic;
using UnityEngine;
using Rushitadel.Combat;

namespace Rushitadel.Presentation
{
    // Visual children use world sizes. They never resize the projectile or its hitbox.
    public static class ProjectileArt
    {
        private static readonly Dictionary<string, Sprite> sprites = new();
        private static Material material;
        public static Material Material
        {
            get
            {
                if(material==null)
                {
                    material=new Material(Resources.Load<Shader>("PlayerForms/ElementSprite"));
                    material.SetColor("_ElementColor",Color.white);
                }
                return material;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){sprites.Clear();material=null;}

        public static Sprite Load(string name)
        {
            if(sprites.TryGetValue(name,out var cached)&&cached!=null)return cached;
            var texture=Resources.Load<Texture2D>("ProjectileArt/"+name);
            if(texture==null){Debug.LogError("Missing projectile art: "+name);return null;}
            var pixels=texture.GetPixels32();
            int left=texture.width,bottom=texture.height,right=-1,top=-1;
            for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)
                if(pixels[y*texture.width+x].a>64)
                {left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
            if(right<left)return null;
            var rect=new Rect(left,bottom,right-left+1,top-bottom+1);
            var sprite=Sprite.Create(texture,rect,Vector2.one*.5f,rect.height,0,SpriteMeshType.FullRect);
            sprite.name=name;sprites[name]=sprite;return sprite;
        }
        public static ProjectileVisual Set(GameObject target,string name,Vector2 size,Color tint,int mode=0,Vector2 offset=default)
        {
            var view=target.GetComponent<ProjectileVisual>();
            if(view==null)
            {
                foreach(var renderer in target.GetComponentsInChildren<SpriteRenderer>())renderer.enabled=false;
                view=target.AddComponent<ProjectileVisual>();
            }
            view.Configure(name,size,tint,mode,offset);
            return view;
        }
        public static void Transformation(GameObject target,int form,Color tint,bool fragment=false)
        {
            Vector2 size=form switch
            {
                1=>new(.085f,.16f),2=>new(.11f,.13f),3=>new(.18f,.28f),
                4=>Vector2.one*.4f,5=>Vector2.one*.26f,
                6=>new(1.49f,1.8f),7=>new(.12f,.18f),
                8=>new(.8f,.26f),9=>Vector2.one*.34f,10=>Vector2.one*.6f,
                11=>new(2.08f,.46f),12=>new(.14f,.23f),
                13=>new(1,.26f),14=>new(.8f,.26f),15=>new(.7f,.3f),_=>new(.13f,.22f)
            };
            Vector2 offset=form==6?new Vector2(0,.9f):form==11?new Vector2(0,1.77f):Vector2.zero;
            int mode=form==10?1:form==3?2:form==8||form==14?3:form==6||form==11?4:0;
            Set(target,fragment?"SplitFragment":"ROT-"+form.ToString("00"),fragment?new Vector2(.08f,.14f):size,tint,mode,offset);
        }
        public static ProjectileVisual Burst(Vector3 position,string name,Vector2 size,Color tint,float duration,float angle=0)
        {
            var go=new GameObject(name+" effect");go.transform.SetPositionAndRotation(position,Quaternion.Euler(0,0,angle));
            var view=Set(go,name,size,tint,5);view.ExpireAfter(duration);return view;
        }
        public static void Arc(Vector2 from,Vector2 to,Color tint)
        {
            Vector2 delta=to-from;
            Burst((from+to)*.5f,"S3-13",new Vector2(.09f,delta.magnitude),tint,.1f,
                Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg-90);
        }
        public static void Status(GameObject target,string name,float duration,float size)
        {
            var child=target.transform.Find("Status "+name);
            ProjectileVisual view;
            if(child==null)
            {
                var go=new GameObject("Status "+name);go.transform.SetParent(target.transform,false);
                view=go.AddComponent<ProjectileVisual>();
            }
            else view=child.GetComponent<ProjectileVisual>();
            view.Configure(name,Vector2.one*size,Color.white,3,Vector2.zero);
            view.ExpireAfter(duration);
        }
    }

    [DefaultExecutionOrder(80),DisallowMultipleComponent]
    public sealed class ProjectileVisual : MonoBehaviour
    {
        private SpriteRenderer image,chargedOutline;
        private MaterialPropertyBlock properties;
        private Vector2 size,offset;
        private Color tint;
        private float age,remaining=-1,duration,scale=1,opacity=1;
        private int mode;
        public string AssetName {get;private set;}
        public SpriteRenderer Renderer=>image;

        public void Configure(string name,Vector2 worldSize,Color color,int animation,Vector2 worldOffset)
        {
            if(image==null)
            {
                var child=new GameObject("Projectile art");child.transform.SetParent(transform,false);
                image=child.AddComponent<SpriteRenderer>();image.sharedMaterial=ProjectileArt.Material;image.sortingOrder=111;
                properties=new MaterialPropertyBlock();
            }
            image.sprite=ProjectileArt.Load(name);
            // The source crescent opens upward; flip its bow to face outward.
            image.flipY=name=="ROT-11"||name=="S3-14";
            AssetName=name;size=worldSize;offset=worldOffset;tint=color;mode=animation;
            Present();
        }
        public void SmallerCopy(){scale=.72f;opacity=.72f;Present();}
        public void Overcharge()
        {
            if(chargedOutline!=null)return;
            var child=new GameObject("Overcharged rim");child.transform.SetParent(image.transform,false);
            chargedOutline=child.AddComponent<SpriteRenderer>();
            chargedOutline.sprite=image.sprite;chargedOutline.sharedMaterial=ProjectileArt.Material;
            chargedOutline.color=new Color(1,.85f,.5f,.25f);chargedOutline.sortingOrder=110;
            child.transform.localScale=Vector3.one*1.22f;
        }
        public void ExpireAfter(float seconds){age=0;remaining=duration=seconds;}
        private void LateUpdate()
        {
            if(Time.timeScale>0)
            {
                age+=Time.deltaTime;
                if(remaining>=0){remaining-=Time.deltaTime;if(remaining<=0){Destroy(gameObject);return;}}
            }
            Present();
        }
        private void Present()
        {
            if(image==null||image.sprite==null)return;
            Vector3 parent=transform.lossyScale;
            float animatedScale=mode==2?1+.035f*Mathf.Sin(age*60):mode==5?Mathf.Lerp(.8f,1,Mathf.Clamp01(age/.1f)):1;
            Vector2 bounds=image.sprite.bounds.size;
            image.transform.localScale=new Vector3(size.x/(Mathf.Max(.0001f,Mathf.Abs(parent.x))*bounds.x),
                size.y/(Mathf.Max(.0001f,Mathf.Abs(parent.y))*bounds.y),1)*scale*animatedScale;
            image.transform.localPosition=new Vector3(offset.x/Mathf.Max(.0001f,parent.x),offset.y/Mathf.Max(.0001f,parent.y),0);
            image.transform.localRotation=Quaternion.Euler(0,0,mode==1?age*-500:0);
            float alpha=mode==3?.8f+.12f*Mathf.Sin(age*12):mode==4?.78f:1;
            if(remaining>=0)alpha*=Mathf.Clamp01(remaining/Mathf.Min(.16f,duration));
            image.color=new Color(1,1,1,alpha*opacity);
            properties.SetColor("_ElementColor",tint);
            properties.SetFloat("_Pulse",1+(mode==3?.06f*Mathf.Sin(age*10):0));
            image.SetPropertyBlock(properties);
        }
    }
}
