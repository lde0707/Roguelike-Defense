using UnityEngine;

namespace Rushitadel.Progression
{
    // Small, nearest-neighbour sprite glyphs. No bloom, gradients or smoothed edges.
    public static class PixelRunArt
    {
        private static Sprite crystal, boss, sentry;
        public static Sprite Sentry => sentry != null ? sentry : sentry = Make(new[] {
            "...1111...", "..122221..", ".12333321.", "1231441321", "1231441321",
            ".12333321.", "..122221..", ".11.11.11." },
            new Color[] {Color.clear, new Color32(28,31,48,255), new Color32(68,73,98,255), new Color32(131,143,156,255), new Color32(238,89,69,255)});
        public static Sprite Crystal => crystal != null ? crystal : crystal = Make(new[] {
            "....11....", "...1331...", "..133331..", ".13343331.", "1334433321",
            "1233333221", ".12233221.", "..122221..", "...1221...", "....11...." },
            new Color[] {Color.clear, new Color32(15,46,83,255), new Color32(26,110,204,255), new Color32(62,182,243,255), new Color32(193,244,255,255)});
        public static Sprite Boss => boss != null ? boss : boss = Make(new[] {
            "......111111......", "....1122222211....", "...123333333321...", "..1233111133321..",
            ".123321221233321.", "1233212441233321", "1233212441233321", "1233331221333321",
            ".12333322333321.", "..123332233321..", "...1222222221...", "..112113311211..",
            ".1221.1331.1221.", "1221..1331..1221", ".11....11....11." },
            new Color[] {Color.clear, new Color32(20,26,49,255), new Color32(55,72,105,255), new Color32(110,136,153,255), new Color32(242,113,77,255)});
        private static Sprite Make(string[] rows, Color[] palette)
        {
            int width = 0; foreach (string row in rows) width = Mathf.Max(width, row.Length);
            var texture = new Texture2D(width, rows.Length, TextureFormat.RGBA32, false) {filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp};
            var pixels = new Color[width * rows.Length];
            for (int y = 0; y < rows.Length; y++) for (int x = 0; x < rows[y].Length; x++)
                pixels[(rows.Length - y - 1) * width + x] = rows[y][x] == '.' ? Color.clear : palette[rows[y][x] - '0'];
            texture.SetPixels(pixels); texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, rows.Length), new Vector2(.5f,.5f), width);
        }
    }
}
