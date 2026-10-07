$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives,System.Private.Windows.GdiPlus,System.Private.Windows.Core -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
public static class TurbineAssetExport {
  public static void Convert(string input,string output,int size) {
    using(var src=new Bitmap(input)) using(var dst=new Bitmap(size,size,PixelFormat.Format32bppArgb)) {
      int l=src.Width,t=src.Height,r=-1,b=-1;
      for(int y=0;y<src.Height;y++)for(int x=0;x<src.Width;x++) if(src.GetPixel(x,y).A>=128) {
        l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);
      }
      if(r<l)throw new Exception("Empty source: "+input);
      int bw=r-l+1,bh=b-t+1;
      double scale=(size==48?42.0:22.0)/Math.Max(bw,bh);
      int w=(int)Math.Round(bw*scale),h=(int)Math.Round(bh*scale),ox=(size-w)/2,oy=(size-h)/2;
      for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
        Color c=src.GetPixel(Math.Min(r,l+(int)((x+.5)*bw/w)),Math.Min(b,t+(int)((y+.5)*bh/h)));
        dst.SetPixel(ox+x,oy+y,c.A<128?Color.Transparent:Color.FromArgb(255,c.R,c.G,c.B));
      }
      dst.Save(output,ImageFormat.Png);
    }
  }
}
'@
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$destination = Join-Path $repo 'Rushitadel2D\Assets\Resources\PlayerForms'
$originalDir = Join-Path $PSScriptRoot 'generated-originals'
$backupDir = Join-Path $PSScriptRoot 'previous-unity-images'
New-Item -ItemType Directory -Force -Path $originalDir,$backupDir | Out-Null
$items = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') | ConvertFrom-Json -NoEnumerate
foreach($item in $items) {
  $name=$item[0]; $source=$item[1]
  $outputPath=Join-Path $destination ($name+'.png')
  $backupPath=Join-Path $backupDir ($name+'.png')
  if((Test-Path -LiteralPath $outputPath) -and !(Test-Path -LiteralPath $backupPath)) { Copy-Item -LiteralPath $outputPath -Destination $backupPath }
  Copy-Item -LiteralPath $source -Destination (Join-Path $originalDir ($name+'.png'))
  $size=if($name -eq 'Hull'){48}else{24}
  [TurbineAssetExport]::Convert($source,$outputPath,$size)
  Write-Output ($name+' '+$size+'x'+$size)
}
$sheet=[System.Drawing.Bitmap]::new(768,864)
$g=[System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(32,36,45))
$font=[System.Drawing.Font]::new('Segoe UI',12)
for($i=0;$i -lt 16;$i++) {
  $name='ROT-'+($i+1).ToString('00')
  $src=[System.Drawing.Bitmap]::new((Join-Path $destination ($name+'.png')))
  $ox=($i%4)*192+24; $oy=[int][Math]::Floor($i/4)*216+8
  for($y=0;$y -lt 24;$y++) { for($x=0;$x -lt 24;$x++) {
    $c=$src.GetPixel($x,$y);if($c.A -eq 0){continue}
    $brush=[System.Drawing.SolidBrush]::new($c);$g.FillRectangle($brush,$ox+$x*6,$oy+$y*6,6,6);$brush.Dispose()
  }
  }
  $g.DrawString($name,$font,[System.Drawing.Brushes]::White,$ox+36,$oy+166)
  $src.Dispose()
}
$sheet.Save((Join-Path $PSScriptRoot 'turrets-preview.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$font.Dispose();$g.Dispose();$sheet.Dispose()
