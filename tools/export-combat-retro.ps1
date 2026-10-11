param([string]$Manifest='enemies.json', [string]$OutputDirectory='output/imagegen/combat-retro-20261007', [ValidateSet(1,2)][int]$PixelScale=1, [switch]$RefinedGrid)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives -TypeDefinition @'
using System.Drawing;
public static class RetroBounds {
 // Area reduction first, then exact integer replication. Sampling just one source
 // pixel during a large reduction aliases fine highlights into random speckles.
 public static Bitmap Pixelize(Bitmap src, Rectangle bounds, int width, int height, int pad, int scale, bool beam) {
  int tw=width/scale, th=height/scale, p=pad/scale;
  double fit=System.Math.Min((tw-2*p)/(double)bounds.Width,(th-2*p)/(double)bounds.Height);
  int w=beam?tw:System.Math.Max(1,(int)System.Math.Round(bounds.Width*fit));
  int h=beam?th:System.Math.Max(1,(int)System.Math.Round(bounds.Height*fit));
  int ox=(tw-w)/2, oy=(th-h)/2;
  var output=new Bitmap(width,height);
  for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
   double x0=bounds.X+x*bounds.Width/(double)w, x1=bounds.X+(x+1)*bounds.Width/(double)w;
   double y0=bounds.Y+y*bounds.Height/(double)h, y1=bounds.Y+(y+1)*bounds.Height/(double)h;
   double alpha=0, red=0, green=0, blue=0, total=(x1-x0)*(y1-y0);
   for(int sy=(int)y0;sy<System.Math.Ceiling(y1);sy++)for(int sx=(int)x0;sx<System.Math.Ceiling(x1);sx++) {
    if(sx>=src.Width||sy>=src.Height)continue;
    double coverage=(System.Math.Min(x1,sx+1)-System.Math.Max(x0,sx))*(System.Math.Min(y1,sy+1)-System.Math.Max(y0,sy));
    var c=src.GetPixel(sx,sy); double a=c.A/255.0*coverage;
    alpha+=a; red+=c.R*a; green+=c.G*a; blue+=c.B*a;
   }
   if(alpha<total*.45)continue;
   var color=Color.FromArgb(255,Quantize(red/alpha),Quantize(green/alpha),Quantize(blue/alpha));
   for(int dy=0;dy<scale;dy++)for(int dx=0;dx<scale;dx++)output.SetPixel((ox+x)*scale+dx,(oy+y)*scale+dy,color);
  }
  return output;
 }
 private static int Quantize(double value) { return System.Math.Min(255,(int)System.Math.Round(value/16)*16); }
 public static Rectangle Get(Bitmap b) {
  int l=b.Width,r=-1,t=b.Height,d=-1;
  for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)if(b.GetPixel(x,y).A>100){l=System.Math.Min(l,x);r=System.Math.Max(r,x);t=System.Math.Min(t,y);d=System.Math.Max(d,y);}
  if(r<l)throw new System.Exception("Empty generated sprite");
  return new Rectangle(l,t,r-l+1,d-t+1);
 }
}
'@
$root=Split-Path $PSScriptRoot -Parent
$out=Join-Path $root $OutputDirectory
$items=Get-Content (Join-Path $out $Manifest) -Raw | ConvertFrom-Json
$failed=@($items | Where-Object error)
if($failed.Count){throw ($failed | ConvertTo-Json)}
$sheet=[System.Drawing.Bitmap]::new(800,([int][Math]::Ceiling($items.Count/5.0)*130))
$sg=[System.Drawing.Graphics]::FromImage($sheet)
$sg.Clear([System.Drawing.Color]::FromArgb(28,32,43))
$sg.InterpolationMode='NearestNeighbor';$sg.PixelOffsetMode='Half'
$font=[System.Drawing.Font]::new('Arial',8)
$i=0
foreach($item in $items){
 if($RefinedGrid){
  $PixelScale=2
  $sizes=@{48=80;24=40;32=56;64=108;16=28}
  $item.width=$sizes[[int]$item.width];$item.height=$sizes[[int]$item.height]
  if(!$item.width -or !$item.height){throw 'Unsupported refined grid size'}
 }
 $dest=Join-Path $root ('Rushitadel2D/Assets/Resources/'+$item.key+'.png')
 $backup=Join-Path $out ('backups/'+$item.key+'.png')
 $original=Join-Path $out ('originals/'+$item.key+'.png')
 foreach($path in @($backup,$original,$dest)){New-Item -ItemType Directory -Force -Path (Split-Path $path -Parent) | Out-Null}
 if((Test-Path $dest) -and !(Test-Path $backup)){Copy-Item -LiteralPath $dest -Destination $backup}
 Copy-Item -LiteralPath $item.source -Destination $original
 $src=[System.Drawing.Bitmap]::new($original)
 $bounds=[RetroBounds]::Get($src)
 $l=$bounds.X;$top=$bounds.Y;$r=$bounds.Right-1;$bottom=$bounds.Bottom-1
 if($PixelScale -gt 1){
 $pixelPad=2;if($RefinedGrid){$pixelPad=4}
 $bmp=[RetroBounds]::Pixelize($src,$bounds,[int]$item.width,[int]$item.height,$pixelPad,$PixelScale,[bool]$item.beam)
 } else {
 $bmp=[System.Drawing.Bitmap]::new([int]$item.width,[int]$item.height)
 $g=[System.Drawing.Graphics]::FromImage($bmp);$g.InterpolationMode='NearestNeighbor';$g.PixelOffsetMode='Half'
 $pad=2
 if($item.beam){$pad=0}
 $maxW=$item.width-2*$pad;$maxH=$item.height-2*$pad
 $factor=[Math]::Min($maxW/($r-$l+1),$maxH/($bottom-$top+1))
 $w=[Math]::Max(1,[int][Math]::Round(($r-$l+1)*$factor));$h=[Math]::Max(1,[int][Math]::Round(($bottom-$top+1)*$factor))
 if($item.beam){$w=$item.width;$h=$item.height}
 $g.DrawImage($src,[System.Drawing.Rectangle]::new([int](($item.width-$w)/2),[int](($item.height-$h)/2),$w,$h),$l,$top,($r-$l+1),($bottom-$top+1),[System.Drawing.GraphicsUnit]::Pixel)
 $g.Dispose()
 }
 $bmp.Save($dest,[System.Drawing.Imaging.ImageFormat]::Png)
 if($PixelScale -gt 1){
  $logicalPath=Join-Path $out ('logical/'+$item.key+'.png')
  New-Item -ItemType Directory -Force -Path (Split-Path $logicalPath -Parent) | Out-Null
  $tiny=[System.Drawing.Bitmap]::new([int]($item.width/$PixelScale),[int]($item.height/$PixelScale))
  for($ty=0;$ty -lt $tiny.Height;$ty++){for($tx=0;$tx -lt $tiny.Width;$tx++){
   $color=$bmp.GetPixel($tx*$PixelScale,$ty*$PixelScale)
   $tiny.SetPixel($tx,$ty,$color)
   for($dy=0;$dy -lt $PixelScale;$dy++){for($dx=0;$dx -lt $PixelScale;$dx++){
    if($bmp.GetPixel($tx*$PixelScale+$dx,$ty*$PixelScale+$dy).ToArgb() -ne $color.ToArgb()){throw "Uneven pixel block: $($item.key)"}
   }}
  }}
  $tiny.Save($logicalPath,[System.Drawing.Imaging.ImageFormat]::Png);$tiny.Dispose()
 }
 $cx=($i%5)*160;$cy=[int][Math]::Floor($i/5)*130
 $previewScale=[Math]::Max(1,[Math]::Floor([Math]::Min(144.0/$bmp.Width,96.0/$bmp.Height)))
 $pw=[int]($bmp.Width*$previewScale);$ph=[int]($bmp.Height*$previewScale)
 $sg.DrawImage($bmp,[System.Drawing.Rectangle]::new(($cx+8),($cy+5),$pw,$ph))
 $label=$item.key.Replace('EnemyProjectiles/','EP/').Replace('ProjectileArt/','P/').Replace('EnemyArt/','E/').Replace('BossArt/','B/')
 $sg.DrawString($label,$font,[System.Drawing.Brushes]::White,[single]($cx+4),[single]($cy+105))
 $bmp.Dispose();$src.Dispose();$i++
}
$sheet.Save((Join-Path $out ($Manifest.Replace('.json','-preview.png'))),[System.Drawing.Imaging.ImageFormat]::Png)
$font.Dispose();$sg.Dispose();$sheet.Dispose()
Write-Output "Exported $i sprites from $Manifest"
