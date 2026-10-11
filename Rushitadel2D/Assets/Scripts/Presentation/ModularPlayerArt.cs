using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Rushitadel.Combat;
using Rushitadel.Player;
using Rushitadel.Progression;

namespace Rushitadel.Presentation
{
    [DefaultExecutionOrder(60), DisallowMultipleComponent]
    public sealed class ModularPlayerArt : MonoBehaviour
    {
        private static readonly Dictionary<string, Sprite> sprites = new();
        private readonly List<TurretVisual> turrets = new();
        private MaterialPropertyBlock hullProperties;
        private SpriteRenderer hull, shadow;
        private readonly SpriteRenderer[] echoes = new SpriteRenderer[2];
        private Material material;
        private Light2D lightSource;
        private PlayerTurretRig rig;
        private PlayerFlightEffects flight;
        private int element = -1;
        public Color EmissionColor { get; private set; }
        public int VisibleTurrets => turrets.Count;
        public PlayerFlightEffects Flight => flight;
        public SpriteRenderer HullRenderer => hull;
        public const float HullScale = 1.46f;
        // Mount-ring centre on the 20px turret canvas: logical texel (9,15) from the top-left.
        private static readonly Vector2 MountPivot = new(9.5f / 20f, 4.5f / 20f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => sprites.Clear();

        public static Sprite LoadSprite(string name, bool mount)
        {
            string key = name + (mount ? "/mount" : "/hull");
            if (sprites.TryGetValue(key, out var cached) && cached != null) return cached;
            var texture = Resources.Load<Texture2D>("PlayerForms/" + name);
            if (texture == null) return null;
            Sprite result;
            if (mount)
            {
                // Every turret canvas seats its mount ring on the same texel, so all forms
                // sit on the hull socket regardless of muzzle length. Pixels per unit follow
                // the hull so turret and hull pixels render at an identical size.
                var hullTexture = Resources.Load<Texture2D>("PlayerForms/Hull");
                float unitsPerHullTexel = HullScale / (hullTexture != null ? hullTexture.height : 80f);
                result = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), MountPivot,
                    1f / unitsPerHullTexel, 0, SpriteMeshType.FullRect);
            }
            else
            {
                // A fixed full hull canvas preserves the reactor pivot.
                result = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f),
                    texture.height, 0, SpriteMeshType.FullRect);
            }
            sprites[key]=result;
            return result;
        }

        public void Refresh()
        {
            rig=GetComponent<PlayerTurretRig>();
            if(rig==null || rig.RotatingBody==null) return;
            var sprite=LoadSprite("Hull",false);
            if(sprite==null) return;
            if(hull==null)
            {
                var shader=Resources.Load<Shader>("PlayerForms/ElementSprite");
                if(shader==null) { Debug.LogError("Missing ElementSprite shader"); return; }
                material=new Material(shader);
                hullProperties=new MaterialPropertyBlock();
                foreach(var old in rig.RotatingBody.GetComponentsInChildren<SpriteRenderer>()) old.enabled=false;
                hull=MakeHull("Modular Hull",sprite,100);
                shadow=MakeHull("Hover shadow",sprite,97);
                shadow.color=new Color(0,0,0,.16f);
                for(int i=0;i<echoes.Length;i++)
                {
                    echoes[i]=MakeHull("Rotor afterimage "+i,sprite,98+i);
                    echoes[i].color=new Color(.5f,.5f,.55f,i==0?.055f:.025f);
                }
                lightSource=hull.gameObject.AddComponent<Light2D>();
                lightSource.lightType=Light2D.LightType.Point;
                lightSource.pointLightOuterRadius=.8f;
                flight=GetComponent<PlayerFlightEffects>();
                if(flight==null) flight=gameObject.AddComponent<PlayerFlightEffects>();
                flight.Configure(this);
                foreach(var tower in rig.GetComponentsInChildren<TowerController>())
                {
                    var visual=tower.GetComponent<TurretVisual>();
                    if(visual==null) visual=tower.gameObject.AddComponent<TurretVisual>();
                    visual.Configure(material,this); turrets.Add(visual);
                }
            }
            element=CampaignDirector.Instance!=null?CampaignDirector.Instance.State.element:0;
            EmissionColor=new Color(.65f,.55f,.8f);
            material.SetColor("_ElementColor",EmissionColor);
            lightSource.color=EmissionColor;
            foreach(var turret in turrets) turret.Refresh();
        }

        private SpriteRenderer MakeHull(string name,Sprite sprite,int order)
        {
            var go=new GameObject(name);
            go.transform.SetParent(rig.RotatingBody,false);
            go.transform.localScale=Vector3.one*HullScale;
            var renderer=go.AddComponent<SpriteRenderer>();
            renderer.sprite=sprite; renderer.sortingOrder=order; renderer.sharedMaterial=material;
            return renderer;
        }

        private void LateUpdate()
        {
            if(CampaignDirector.Instance!=null && element!=CampaignDirector.Instance.State.element) Refresh();
            if(hull==null || flight==null) return;
            var localOffset=rig.RotatingBody.InverseTransformVector(flight.VisualOffsetWorld);
            hull.transform.localPosition=localOffset;
            hull.transform.localScale=Vector3.one*(HullScale*flight.HoverScale);
            hullProperties.SetFloat("_Pulse",flight.CorePulse);
            hullProperties.SetFloat("_Flash",flight.ShotFlash);
            hull.SetPropertyBlock(hullProperties);
            shadow.transform.position=transform.position+new Vector3(0,-.075f,0);
            for(int i=0;i<echoes.Length;i++)
            {
                echoes[i].transform.localPosition=localOffset;
                echoes[i].color=new Color(.5f,.5f,.55f,(i==0?.055f:.025f)*(RandomPlayerWeb.Has(23)?2.5f:1));
                echoes[i].transform.localRotation=Quaternion.Euler(0,0,2.8f*(i+1));
            }
            lightSource.intensity=.06f+flight.ShotFlash*.12f;
        }

        private void OnDestroy() { if(material!=null) Destroy(material); }
    }

    [DefaultExecutionOrder(65), DisallowMultipleComponent]
    public sealed class TurretVisual : MonoBehaviour
    {
        private TowerController tower;
        private ModularPlayerArt owner;
        private SpriteRenderer art;
        private MaterialPropertyBlock properties;
        private float recoil, velocity, flash;
        private bool initialized;
        public int DisplayedForm { get; private set; }=-1;
        public float Recoil => recoil;

        public void Configure(Material material,ModularPlayerArt playerArt)
        {
            if(initialized) return;
            properties=new MaterialPropertyBlock();
            owner=playerArt; tower=GetComponent<TowerController>();
            var go=new GameObject("Transformation Sprite");
            go.transform.SetParent(transform,false);
            go.transform.localScale=Vector3.one;
            art=go.AddComponent<SpriteRenderer>();
            art.sortingOrder=104; art.sharedMaterial=material;
            tower.Fired+=OnFire; initialized=true; Refresh();
        }

        public void Refresh()
        {
            if(!initialized) return;
            int form=CampaignDirector.Form(tower.SlotId);
            if(form!=DisplayedForm)
            {
                art.sprite=ModularPlayerArt.LoadSprite(form==0?"Default":"ROT-"+form.ToString("00"),true);
                DisplayedForm=form;
            }
        }

        private void OnFire()
        {
            int form=CampaignDirector.Form(tower.SlotId);
            flash=1;
            bool heavy=form==3 || form==5;
            bool support=form==8 || form==14 || form==6 || form==15;
            recoil=Mathf.Min(.065f,recoil+(support?.008f:heavy?.045f:.025f));
            velocity=0;
            owner.Flight.TurretShot(tower,form);
        }

        private void LateUpdate()
        {
            if(!initialized) return;
            if(DisplayedForm!=CampaignDirector.Form(tower.SlotId)) Refresh();
            if(Time.timeScale>0)
            {
                recoil=Mathf.SmoothDamp(recoil,0,ref velocity,.065f,Mathf.Infinity,Time.deltaTime);
                flash=Mathf.MoveTowards(flash,0,Time.deltaTime*10);
            }
            art.transform.localPosition=transform.InverseTransformVector(owner.Flight.VisualOffsetWorld)+Vector3.down*recoil;
            properties.SetFloat("_Pulse",1+flash*.7f);
            properties.SetFloat("_Flash",flash*.65f);
            art.SetPropertyBlock(properties);
        }

        private void OnDestroy() { if(tower!=null) tower.Fired-=OnFire; }
    }
}
