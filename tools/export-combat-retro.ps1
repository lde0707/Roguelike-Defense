param([string]$Manifest='enemies.json', [string]$OutputDirectory='output/imagegen/combat-retro-20261007')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives -TypeDefinition @'
using System.Drawing;
public static class RetroBounds {
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
 $dest=Join-Path $root ('Rushitadel2D/Assets/Resources/'+$item.key+'.png')
 $backup=Join-Path $out ('backups/'+$item.key+'.png')
 $original=Join-Path $out ('originals/'+$item.key+'.png')
 foreach($path in @($backup,$original,$dest)){New-Item -ItemType Directory -Force -Path (Split-Path $path -Parent) | Out-Null}
 if((Test-Path $dest) -and !(Test-Path $backup)){Copy-Item -LiteralPath $dest -Destination $backup}
 Copy-Item -LiteralPath $item.source -Destination $original
 $src=[System.Drawing.Bitmap]::new($original)
 $bounds=[RetroBounds]::Get($src)
 $l=$bounds.X;$top=$bounds.Y;$r=$bounds.Right-1;$bottom=$bounds.Bottom-1
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
 $bmp.Save($dest,[System.Drawing.Imaging.ImageFormat]::Png)
 $cx=($i%5)*160;$cy=[int][Math]::Floor($i/5)*130
 $pw=96;$ph=96;if($item.beam){$pw=144;$ph=36}
 $sg.DrawImage($bmp,[System.Drawing.Rectangle]::new(($cx+8),($cy+5),$pw,$ph))
 $label=$item.key.Replace('EnemyProjectiles/','EP/').Replace('ProjectileArt/','P/').Replace('EnemyArt/','E/').Replace('BossArt/','B/')
 $sg.DrawString($label,$font,[System.Drawing.Brushes]::White,[single]($cx+4),[single]($cy+105))
 $bmp.Dispose();$src.Dispose();$i++
}
$sheet.Save((Join-Path $out ($Manifest.Replace('.json','-preview.png'))),[System.Drawing.Imaging.ImageFormat]::Png)
$font.Dispose();$sg.Dispose();$sheet.Dispose()
Write-Output "Exported $i sprites from $Manifest"
