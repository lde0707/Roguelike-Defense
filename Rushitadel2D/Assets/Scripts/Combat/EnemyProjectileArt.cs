using System.Collections.Generic;
using UnityEngine;
using Rushitadel.World;

namespace Rushitadel.Combat
{
    public static class EnemyProjectileArt
    {
        private static readonly Dictionary<string,Sprite> sprites=new();
        private static readonly Dictionary<string,Material> materials=new();
        public static string PathFor(PressureTheme theme,int variant)=>"EnemyProjectiles/"+EnemyArchetype.ThemeFolder(theme)+"/"+(variant<5?"N0"+(variant+1):"E0"+(variant-4));
        public static Sprite Load(string path)
        {
            if(sprites.TryGetValue(path,out var found))return found;
            var texture=Resources.Load<Texture2D>(path);
            if(texture==null){Debug.LogError("Missing enemy projectile: "+path);return null;}
            return sprites[path]=Sprite.Create(texture,new Rect(2,2,28,28),Vector2.one*.5f,32,0,SpriteMeshType.FullRect);
        }
        public static void Apply(EnemyProjectileController shot,PressureTheme theme,int variant)
        {
            string path=PathFor(theme,variant);
            var sprite=Load(path);if(sprite==null)return;
            foreach(var renderer in shot.GetComponentsInChildren<SpriteRenderer>())renderer.enabled=false;
            var visual=new GameObject("Round enemy projectile");visual.transform.SetParent(shot.transform,false);
            var sr=visual.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=112;
            if(!materials.TryGetValue(path,out var material))materials[path]=material=new Material(Shader.Find("Sprites/Default")){mainTexture=sprite.texture};
            sr.sharedMaterial=material;
            // Keep the previous .32 * 1.2 thickness * 2 hostile enlargement.
            const float diameter=.768f;
            float scale=diameter/(sprite.bounds.size.x*Mathf.Abs(shot.transform.lossyScale.x));
            visual.transform.localScale=Vector3.one*scale;
            shot.FitVisualCollision(Vector2.one*diameter);
        }
    }
}
