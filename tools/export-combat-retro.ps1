param([string]$Manifest='enemies.json', [string]$OutputDirectory='output/imagegen/combat-retro-20261007', [ValidateSet(1,2)][int]$PixelScale=1, [switch]$RefinedGrid, [switch]$FourColors)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives -TypeDefinition @'
using System.Drawing;
public static class RetroBounds {
 public static void LimitPalette(Bitmap image,int scale,string key) {
  string[] hex={"18222F","526579","AAB9B6","987AC8"};
  if(key.Contains("Grass"))hex=new[]{"291C2C","864052","D1BE87","E97D72"};
  else if(key.Contains("Lava"))hex=new[]{"241F2C","73423C","EE743C","FFE0A0"};
  else if(key.Contains("Ice")||key.Contains("Butterfly"))hex=new[]{"20283D","526888","ACE0E8","A18BCE"};
  else if(key.Contains("WaterElectric"))hex=new[]{"172A35","387A87","E9AF55","E8E5BD"};
  var palette=new Color[4];for(int k=0;k<4;k++)palette[k]=Color.FromArgb(255,Color.FromArgb(System.Convert.ToInt32(hex[k],16)));
  int w=image.Width/scale,h=image.Height/scale;var source=new Color[w,h];
  for(int y=0;y<h;y++)for(int x=0;x<w;x++)source[x,y]=image.GetPixel(x*scale,y*scale);
  for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
   Color c=source[x,y];if(c.A==0)continue;
   // One logical pixel of unbroken, fully opaque contour; never fuzzy alpha edges.
   bool edge=x==0||y==0||x==w-1||y==h-1||source[x-1,y].A==0||source[x+1,y].A==0||source[x,y-1].A==0||source[x,y+1].A==0;
   int best=0;double distance=double.MaxValue;
   if(!edge)for(int k=0;k<4;k++) {
    var p=palette[k];double dr=c.R-p.R,dg=c.G-p.G,db=c.B-p.B;double d=dr*dr+dg*dg+db*db;
    if(d<distance){distance=d;best=k;}
   }
   if(!edge && key.Contains("PlayerForms")){
    double light=c.R*.2126+c.G*.7152+c.B*.0722;
    best=(c.B-c.G>16 && c.R-c.G>4)?3:light>108?2:light<43?0:1;
   }
   for(int dy=0;dy<scale;dy++)for(int dx=0;dx<scale;dx++)image.SetPixel(x*scale+dx,y*scale+dy,palette[best]);
  }
  for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++){
   var c=image.GetPixel(x,y);if(c.A==0)continue;
   bool valid=false;foreach(var p in palette)if(c.ToArgb()==p.ToArgb())valid=true;
   if(!valid)throw new System.Exception("Palette overflow or partial alpha: "+key);
  }
 }
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
 if($FourColors){
  $paletteKey=[string]$item.key
  if($item.paletteKey){$paletteKey=[string]$item.paletteKey}
  [RetroBounds]::LimitPalette($bmp,$PixelScale,$paletteKey)
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
