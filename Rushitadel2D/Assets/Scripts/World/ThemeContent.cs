using UnityEngine;
namespace Rushitadel.World
{
    public static class ThemeContent
    {
        public static string Type(PressureTheme theme)=>theme switch
        {PressureTheme.Fire=>"A",PressureTheme.Ice=>"B",PressureTheme.Grass=>"C",_=>"D"};
        private static readonly Sprite[] maps=new Sprite[4];
        public static Sprite Map(PressureTheme theme)
        {
            string type=Type(theme);int index=type[0]-'A';
            if(maps[index]!=null)return maps[index];
            string prefix="ThemeContent/"+type+"/Map/tile_type_"+type;
            var road=Resources.Load<Texture2D>(prefix+"_L");
            var cliff=Resources.Load<Texture2D>(prefix+"_M");
            var upper=Resources.Load<Texture2D>(prefix+"_R");
            if(road==null||cliff==null||upper==null){Debug.LogError("Missing theme tiles: "+type);return null;}
            // 20 source pixels per world unit, four complete tile rows: no vertical stretch or partial seam.
            const int width=384,height=256;
            var output=new Texture2D(width,height,TextureFormat.RGBA32,false){name="Theme "+type+" battlefield",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};
            var l=road.GetPixels32();var m=cliff.GetPixels32();var r=upper.GetPixels32();var pixels=new Color32[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                // Keep the map art fixed; gameplay boundaries sit at the inner cliff face (+/-6.5).
                int mx=x<192?103-x:x-280;
                if(mx>=0&&mx<64)pixels[y*width+x]=m[(y%cliff.height)*cliff.width+mx%cliff.width];
                else if(x<40||x>=344)pixels[y*width+x]=r[(y%upper.height)*upper.width+x%upper.width];
                else pixels[y*width+x]=l[(y%road.height)*road.width+x%road.width];
            }
            output.SetPixels32(pixels);output.Apply(false,true);
            return maps[index]=Sprite.Create(output,new Rect(0,0,width,height),Vector2.one*.5f,20,0,SpriteMeshType.FullRect);
        }
    }
}
