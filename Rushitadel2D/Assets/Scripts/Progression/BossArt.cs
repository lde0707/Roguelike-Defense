using System.Collections.Generic;
using UnityEngine;

namespace Rushitadel.Progression
{
    public static class BossArt
    {
        private static readonly Dictionary<string,Sprite> cache=new();
        private static Material material;
        private static Material hostileMaterial;
        public static Material HostileMaterial
        {
            get
            {
                if(hostileMaterial==null)
                {
                    hostileMaterial=new Material(Resources.Load<Shader>("ProjectileArt/FactionProjectile"));
                    hostileMaterial.SetFloat("_Hostile",1);
                }
                return hostileMaterial;
            }
        }
        public static Sprite Load(string path, int half=-1)
        {
            string key=path+half;
            if(cache.TryGetValue(key,out var found)&&found!=null)return found;
            var t=Resources.Load<Texture2D>("BossArt/"+path);
            if(t==null){Debug.LogError("Missing boss art: "+path);return null;}
            var p=t.GetPixels32(); int start=half==1?t.width/2:0,end=half==0?t.width/2:t.width;
            int l=end,r=start-1,b=t.height,u=-1;
            for(int y=0;y<t.height;y++)for(int x=start;x<end;x++)if(p[y*t.width+x].a>100)
            {l=Mathf.Min(l,x);r=Mathf.Max(r,x);b=Mathf.Min(b,y);u=Mathf.Max(u,y);}
            if(r<l)return null;
            return cache[key]=Sprite.Create(t,new Rect(l,b,r-l+1,u-b+1),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);
        }
        public static SpriteRenderer Create(Transform parent,string path,Vector2 position,float width,int half=-1)
        {
            var go=new GameObject(path);go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=Load(path,half);sr.sortingOrder=100;
            if(material==null)material=new Material(Shader.Find("Sprites/Default"));
            sr.sharedMaterial=material;
            if(sr.sprite!=null)go.transform.localScale=Vector3.one*(width/sr.sprite.bounds.size.x);
            return sr;
        }
        public static Sprite Beam(string path)
        {
            string key=path+" beam";if(cache.TryGetValue(key,out var sprite)&&sprite!=null)return sprite;
            var t=Resources.Load<Texture2D>("BossArt/"+path);if(t==null)return null;
            // Slice only the repeatable energy shaft: muzzle/end art is not stretched along the beam.
            var r=t.width==64 && t.height==16?new Rect(0,0,t.width,t.height):t.width>t.height?new Rect(t.width*.22f,t.height*.28f,t.width*.56f,t.height*.44f):
                new Rect(t.width*.45f,t.height*.23f,t.width*.10f,t.height*.55f);
            return cache[key]=Sprite.Create(t,r,Vector2.one*.5f,100,0,SpriteMeshType.FullRect);
        }
        public static void Shot(GameObject go,string path,float diameter,bool thicken=true,bool bossProjectile=false)
        {
            foreach(var s in go.GetComponentsInChildren<SpriteRenderer>())s.enabled=false;
            var art=Create(go.transform,path,Vector2.zero,diameter/Mathf.Abs(go.transform.lossyScale.x));
            if(thicken && art.sprite!=null)
            {
                Vector2 bounds=art.sprite.bounds.size;
                Vector2 expanded=Rushitadel.Presentation.ProjectileArt.Thicker(bounds);
                var scale=art.transform.localScale;
                float multiplier=bossProjectile?.9f:Rushitadel.Presentation.ProjectileArt.HostileSizeMultiplier;
                art.transform.localScale=new Vector3(scale.x*expanded.x/bounds.x*multiplier,scale.y*expanded.y/bounds.y*multiplier,scale.z);
                if(bossProjectile)
                {
                    Vector2 world=Vector2.Scale(bounds,new Vector2(Mathf.Abs(art.transform.lossyScale.x),Mathf.Abs(art.transform.lossyScale.y)));
                    float longest=Mathf.Max(world.x,world.y);
                    if(longest>.75f)art.transform.localScale*=.75f/longest;
                }
                art.sharedMaterial=HostileMaterial;
                Vector2 worldSize=Vector2.Scale(bounds,new Vector2(Mathf.Abs(art.transform.lossyScale.x),Mathf.Abs(art.transform.lossyScale.y)));
                go.GetComponent<Rushitadel.Combat.ProjectileBehaviour>()?.FitVisualCollision(worldSize);
            }
            art.sortingOrder=112;
        }
    }
}
