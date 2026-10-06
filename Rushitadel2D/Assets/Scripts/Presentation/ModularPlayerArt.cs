using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Rushitadel.Combat;
using Rushitadel.Player;
using Rushitadel.Progression;
namespace Rushitadel.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ModularPlayerArt : MonoBehaviour
    {
        private static readonly Dictionary<string,Sprite> sprites=new();
        private readonly List<TurretVisual> turrets=new();
        private SpriteRenderer hull;
        private Material material;
        private Light2D lightSource;
        private int element=-1;
        public Color EmissionColor {get;private set;}
        public int VisibleTurrets=>turrets.Count;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void Reset()=>sprites.Clear();
        public static Sprite LoadSprite(string name,bool mount)
        {
            if(sprites.TryGetValue(name,out var cached)&&cached!=null)return cached;
            var t=Resources.Load<Texture2D>("PlayerForms/"+name);if(t==null)return null;
            // Sprite rect selection only: original PNG pixels remain untouched.
            var pixels=t.GetPixels32();int minX=t.width,minY=t.height,maxX=0,maxY=0;
            for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++)if(pixels[y*t.width+x].a>128){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
            var rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1);
            var result=Sprite.Create(t,rect,new Vector2(.5f,mount?.22f:.5f),Mathf.Max(rect.width,rect.height),0,SpriteMeshType.FullRect);
            sprites[name]=result;return result;
        }
        public void Refresh()
        {
            var rig=GetComponent<PlayerTurretRig>();if(rig==null||rig.RotatingBody==null)return;
            var s=LoadSprite("Hull",false);if(s==null)return;
            if(hull==null)
            {
                var shader=Resources.Load<Shader>("PlayerForms/ElementSprite");
                if(shader==null){Debug.LogError("Missing ElementSprite shader");return;}
                material=new Material(shader);
                foreach(var old in rig.RotatingBody.GetComponentsInChildren<SpriteRenderer>())old.enabled=false;
                var go=new GameObject("Modular Hull");go.transform.SetParent(rig.RotatingBody,false);go.transform.localScale=Vector3.one*1.3f;
                hull=go.AddComponent<SpriteRenderer>();hull.sprite=s;hull.sortingOrder=100;hull.sharedMaterial=material;
                lightSource=go.AddComponent<Light2D>();lightSource.lightType=Light2D.LightType.Point;lightSource.pointLightOuterRadius=1.3f;lightSource.intensity=.35f;
                foreach(var tower in rig.GetComponentsInChildren<TowerController>())
                {
                    var v=tower.gameObject.AddComponent<TurretVisual>();v.Configure(material);turrets.Add(v);
                }
            }
            int next=CampaignDirector.Instance!=null?CampaignDirector.Instance.State.element:0;
            element=next;EmissionColor=TurretForms.ElementColor(element);material.SetColor("_ElementColor",EmissionColor);
            lightSource.color=EmissionColor;
            foreach(var t in turrets)t.Refresh();
        }
        private void LateUpdate()
        {
            if(CampaignDirector.Instance!=null&&element!=CampaignDirector.Instance.State.element)Refresh();
            if(material!=null)material.SetFloat("_Pulse",1+.12f*Mathf.Sin(Time.time*3));
        }
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
    public sealed class TurretVisual : MonoBehaviour
    {
        private TowerController tower;
        private SpriteRenderer art,halo;
        private Light2D lightSource;
        private float flash;
        private Material haloMaterial;
        public int DisplayedForm {get;private set;}=-1;
        public void Configure(Material material)
        {
            tower=GetComponent<TowerController>();
            var g=new GameObject("Transformation Sprite");g.transform.SetParent(transform,false);g.transform.localScale=Vector3.one*.53f;
            art=g.AddComponent<SpriteRenderer>();art.sortingOrder=104;art.sharedMaterial=material;
            var h=new GameObject("Muzzle Emission");h.transform.SetParent(transform,false);h.transform.localPosition=new Vector3(0,.24f,0);h.transform.localScale=Vector3.one*.5f;
            halo=h.AddComponent<SpriteRenderer>();halo.sprite=TransformationShot.Glyph;halo.sortingOrder=105;
            haloMaterial=new Material(Resources.Load<Shader>("PlayerForms/PixelGlow"));halo.sharedMaterial=haloMaterial;
            lightSource=h.AddComponent<Light2D>();lightSource.lightType=Light2D.LightType.Point;lightSource.pointLightOuterRadius=.6f;
            tower.Fired+=OnFire;Refresh();
        }
        public void Refresh()
        {
            int form=CampaignDirector.Form(tower.SlotId);
            if(form!=DisplayedForm){art.sprite=ModularPlayerArt.LoadSprite("ROT-"+Mathf.Max(1,form).ToString("00"),true);DisplayedForm=form;}
            int e=CampaignDirector.Instance!=null?CampaignDirector.Instance.State.element:0;
            halo.color=TurretForms.ElementColor(e);lightSource.color=halo.color;
        }
        private void OnFire()=>flash=1;
        private void LateUpdate()
        {
            if(art==null)return;
            if(DisplayedForm!=CampaignDirector.Form(tower.SlotId))Refresh();
            flash=Mathf.MoveTowards(flash,0,Time.deltaTime*8);
            art.transform.localPosition=new Vector3(0,-flash*.018f,0);
            haloMaterial.SetFloat("_Intensity",.14f+flash*.65f);
            lightSource.intensity=.2f+flash*.65f;
        }
        private void OnDestroy(){if(tower!=null)tower.Fired-=OnFire;if(haloMaterial!=null)Destroy(haloMaterial);}
    }
}
