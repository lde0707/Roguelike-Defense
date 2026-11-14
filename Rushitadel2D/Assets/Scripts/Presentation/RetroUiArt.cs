using System.Collections.Generic;
using UnityEngine;

namespace Rushitadel.Presentation
{
    // Generated atlases stay intact. Regions are sliced at runtime without duplicating textures.
    internal static class RetroUiArt
    {
        private static readonly Dictionary<string, Sprite> Cache = new();
        private static Sprite Region(string atlas, string key, float x, float y, float w, float h, float border = 0)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            var texture = Resources.Load<Texture2D>("RetroUI/" + atlas);
            if (texture == null) return null;
            texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
            var rect = new Rect(Mathf.Round(x * texture.width), Mathf.Round((1-y-h)*texture.height),
                Mathf.Round(w*texture.width), Mathf.Round(h*texture.height));
            var sprite = Sprite.Create(texture, rect, Vector2.one*.5f, 100, 0, SpriteMeshType.FullRect,
                Vector4.one * border * texture.width);
            sprite.name = "Retro " + key;
            Cache[key] = sprite;
            return sprite;
        }
        public static Sprite Panel => Region("Frames", "Panel", .047f,.059f,.403f,.377f,.060f);
        public static Sprite Button => Region("Frames", "Button", .501f,.170f,.438f,.178f,.048f);
        public static Sprite Result(bool won) => won
            ? Region("Frames", "Victory", .030f,.482f,.438f,.447f)
            : Region("Frames", "Defeat", .535f,.475f,.391f,.451f);
        public static Sprite Theme(int index) => Region("Themes", "Theme"+index,
            (index%3)/3f, (index/3)/2f, 1/3f, 1/2f);
        public static Sprite Icon(int index) => Region("Icons", "Icon"+index,
            (index%4)/4f, (index/4)/4f, .25f,.25f);
        public static Sprite WebIcon(string name)
        {
            int index = name switch {
                "Survival"=>0,"Mobility"=>1,"Firepower"=>2,"RapidTraining"=>3,
                "Pierce" or "PierceExtend" or "PierceStable"=>4,"ExtraTrajectory"=>5,
                "PowerAmp"=>6,"EfficientCharge"=>7,"HealLink"=>8,"Command"=>9,
                "BlastArea"=>10,"Venom"=>11,"Scatter"=>13,"ResidualBlast"=>14,
                "StatusTransfer"=>15,_=>12 };
            return Icon(index);
        }
    }
}
