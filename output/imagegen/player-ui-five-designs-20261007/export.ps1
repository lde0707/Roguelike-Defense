$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$sourceDir = 'C:\Users\doun0\.codex\generated_images\01a113c5-4677-7e83-a135-d348736d4836'
$originalDir = Join-Path $PSScriptRoot 'originals'
New-Item -ItemType Directory -Force -Path $originalDir | Out-Null
Add-Type -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives,System.Private.Windows.GdiPlus,System.Private.Windows.Core -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
public static class AircraftExport {
  public static string Run(string input,string smallPath,string previewPath) {
    using(var src=new Bitmap(input)) {
      int left=src.Width,top=src.Height,right=-1,bottom=-1;
      for(int y=0;y<src.Height;y++) for(int x=0;x<src.Width;x++) {
        if(src.GetPixel(x,y).A<32) continue;
        left=Math.Min(left,x); top=Math.Min(top,y); right=Math.Max(right,x); bottom=Math.Max(bottom,y);
      }
      if(right<left) throw new Exception("Empty sprite");
      int bw=right-left+1,bh=bottom-top+1;
      double ratio=42.0/Math.Max(bw,bh);
      int w=Math.Max(1,(int)Math.Round(bw*ratio)),h=Math.Max(1,(int)Math.Round(bh*ratio));
      int ox=(48-w)/2,oy=(48-h)/2,count=0;
      using(var dst=new Bitmap(48,48,PixelFormat.Format32bppArgb)) {
        for(int y=0;y<h;y++) for(int x=0;x<w;x++) {
          int sx=Math.Min(right,left+(int)Math.Floor((x+.5)*bw/w));
          int sy=Math.Min(bottom,top+(int)Math.Floor((y+.5)*bh/h));
          dst.SetPixel(ox+x,oy+y,src.GetPixel(sx,sy));
        }
        dst.Save(smallPath,ImageFormat.Png);
        using(var preview=new Bitmap(384,384,PixelFormat.Format32bppArgb)) {
          for(int y=0;y<384;y++) for(int x=0;x<384;x++) preview.SetPixel(x,y,dst.GetPixel(x/8,y/8));
          preview.Save(previewPath,ImageFormat.Png);
        }
        for(int y=0;y<48;y++) for(int x=0;x<48;x++) if(dst.GetPixel(x,y).A==0) count++;
      }
      return "48x48 RGBA; transparent pixels="+count+"; content="+w+"x"+h;
    }
  }
}
'@
$items = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') | ConvertFrom-Json
foreach ($item in $items) {
    $name = $item[0]
    $source = Join-Path $sourceDir $item[1]
    Copy-Item -LiteralPath $source -Destination (Join-Path $originalDir ($name+'.png'))
    $result = [AircraftExport]::Run($source,(Join-Path $PSScriptRoot ($name+'-48.png')),(Join-Path $PSScriptRoot ($name+'-preview-8x.png')))
    Write-Output ($name+': '+$result)
}
