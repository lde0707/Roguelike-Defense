$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives,System.Private.Windows.GdiPlus,System.Private.Windows.Core -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
public static class ProjectileExport {
 public static void Convert(string input,string output,int size) {
  using(var src=new Bitmap(input)) using(var dst=new Bitmap(size,size,PixelFormat.Format32bppArgb)) {
   int l=src.Width,t=src.Height,r=-1,b=-1;
   for(int y=0;y<src.Height;y++)for(int x=0;x<src.Width;x++)if(src.GetPixel(x,y).A>=96){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);}
   if(r<l)throw new Exception("Empty sprite: "+input);
   int bw=r-l+1,bh=b-t+1;double ratio=(size-4.0)/Math.Max(bw,bh);
   int w=Math.Max(1,(int)Math.Round(bw*ratio)),h=Math.Max(1,(int)Math.Round(bh*ratio));
   // Keep a centre sample for narrow bullets; even widths can skip the bright core.
   if(w%2==0 && w<=size/2 && w<size-4)w++;
   int ox=(size-w)/2,oy=(size-h)/2;
   for(int y=0;y<h;y++)for(int x=0;x<w;x++){
    Color c=src.GetPixel(Math.Min(r,l+(int)((x+.5)*bw/w)),Math.Min(b,t+(int)((y+.5)*bh/h)));
    int a=c.A<64?0:c.A<160?128:255;
    dst.SetPixel(ox+x,oy+y,Color.FromArgb(a,c.R,c.G,c.B));
   }
   dst.Save(output,ImageFormat.Png);
  }
 }
}
'@
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$destination=Join-Path $repo 'Rushitadel2D/Assets/Resources/ProjectileArt'
$originals=Join-Path $PSScriptRoot 'generated-originals'
New-Item -ItemType Directory -Force -Path $destination,$originals | Out-Null
$manifest=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') | ConvertFrom-Json -NoEnumerate
$jobs=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'jobs.json') | ConvertFrom-Json -NoEnumerate
foreach($item in $manifest){
 $job=$jobs | Where-Object {$_[0] -eq $item[0]}
 Copy-Item -LiteralPath $item[1] -Destination (Join-Path $originals ($item[0]+'.png'))
 [ProjectileExport]::Convert($item[1],(Join-Path $destination ($item[0]+'.png')),[int]$job[2])
}
# Three readable overview pages; labels are metadata, never baked into a sprite.
$groups=@(@($jobs | Select-Object -First 19),@($jobs | Select-Object -Skip 19 -First 16),@($jobs | Select-Object -Skip 35))
$page=0
foreach($group in $groups){
 $rows=[int][Math]::Ceiling($group.Count/4.0)
 $sheet=[Drawing.Bitmap]::new(960,$rows*200);$g=[Drawing.Graphics]::FromImage($sheet)
 $g.Clear([Drawing.Color]::FromArgb(29,34,43));$font=[Drawing.Font]::new('Malgun Gothic',11)
 for($i=0;$i -lt $group.Count;$i++){
  $job=$group[$i];$spritePath=Join-Path $destination ($job[0]+'.png')
  if(!(Test-Path -LiteralPath $spritePath)){continue}
  $src=[Drawing.Bitmap]::new($spritePath)
  $scale=if($src.Width -le 16){8}elseif($src.Width -le 24){5}else{4}
  $ox=($i%4)*240+[int]((240-$src.Width*$scale)/2);$oy=[int][Math]::Floor($i/4)*200+8+[int]((128-$src.Height*$scale)/2)
  for($y=0;$y -lt $src.Height;$y++){for($x=0;$x -lt $src.Width;$x++){
   $color=$src.GetPixel($x,$y);if($color.A -eq 0){continue}
   $brush=[Drawing.SolidBrush]::new($color);$g.FillRectangle($brush,$ox+$x*$scale,$oy+$y*$scale,$scale,$scale);$brush.Dispose()
  }}
  $tx=($i%4)*240+12;$ty=[int][Math]::Floor($i/4)*200+143
  $g.DrawString(($job[0]+'  '+$job[2]+'px'),$font,[Drawing.Brushes]::White,$tx,$ty)
  $g.DrawString($job[1],$font,[Drawing.Brushes]::LightGray,$tx,$ty+24)
  $src.Dispose()
 }
 $page++;$sheet.Save((Join-Path $PSScriptRoot ('preview-'+$page+'.png')),[Drawing.Imaging.ImageFormat]::Png)
 $g.Dispose();$font.Dispose();$sheet.Dispose()
}
Write-Output ('Exported '+$manifest.Count+' sprites')
