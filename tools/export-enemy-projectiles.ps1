param([string]$Manifest='output/imagegen/enemy-projectiles-20261007/manifest.json')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$repo=Split-Path $PSScriptRoot -Parent
$root=Join-Path $repo 'output/imagegen/enemy-projectiles-20261007'
$items=Get-Content (Join-Path $repo $Manifest) -Raw | ConvertFrom-Json
$palettes=@{
 Grass=@('#331324','#75445b','#bd6178','#efa577','#ffe1a0')
 Lava=@('#291b28','#754039','#c95a37','#ff993e','#fff0ad')
 Ice=@('#2c2145','#69597f','#ba80a5','#efaac9','#ffdcba')
 WaterElectric=@('#291d39','#725071','#be6565','#efac54','#ffe7a0')
}
$themes=@('Grass','Lava','Ice','WaterElectric')
$sheet=New-Object System.Drawing.Bitmap(896,512)
$sg=[System.Drawing.Graphics]::FromImage($sheet)
$sg.Clear([System.Drawing.Color]::FromArgb(255,30,34,44))
$sg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$sg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
$font=New-Object System.Drawing.Font('Arial',9)
$index=0
foreach($item in $items){
 $src=[System.Drawing.Bitmap]::FromFile($item.source)
 $l=$src.Width;$t=$src.Height;$r=-1;$b=-1
 for($y=0;$y -lt $src.Height;$y++){for($x=0;$x -lt $src.Width;$x++){if($src.GetPixel($x,$y).A -gt 128){$l=[Math]::Min($l,$x);$r=[Math]::Max($r,$x);$t=[Math]::Min($t,$y);$b=[Math]::Max($b,$y)}}}
 if($r -lt $l){throw "Empty $($item.id)"}
 $gray=New-Object System.Drawing.Bitmap(32,32)
 $g=[System.Drawing.Graphics]::FromImage($gray)
 $g.Clear([System.Drawing.Color]::Transparent)
 $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
 $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
 $g.DrawImage($src,(New-Object System.Drawing.Rectangle(2,2,28,28)),$l,$t,($r-$l+1),($b-$t+1),[System.Drawing.GraphicsUnit]::Pixel)
 for($row=0;$row -lt 4;$row++){
  $theme=$themes[$row];$colors=@($palettes[$theme] | ForEach-Object {[System.Drawing.ColorTranslator]::FromHtml($_)})
  $dst=New-Object System.Drawing.Bitmap(32,32)
  for($y=0;$y -lt 32;$y++){for($x=0;$x -lt 32;$x++){
   $p=$gray.GetPixel($x,$y)
   if($p.A -ge 128){$level=[Math]::Min(4,[int][Math]::Floor(($p.R*.2126+$p.G*.7152+$p.B*.0722)/256*5));$dst.SetPixel($x,$y,$colors[$level])}
  }}
  $dest=Join-Path $repo "Rushitadel2D/Assets/Resources/EnemyProjectiles/$theme"
  New-Item -ItemType Directory -Force $dest | Out-Null
  $dst.Save((Join-Path $dest "$($item.id).png"),[System.Drawing.Imaging.ImageFormat]::Png)
  $sg.DrawImage($dst,(New-Object System.Drawing.Rectangle(($index*128+16),($row*128),96,96)))
  $sg.DrawString("$theme $($item.id)",$font,[System.Drawing.Brushes]::White,($index*128+10),($row*128+102))
  $dst.Dispose()
 }
 $g.Dispose();$gray.Dispose();$src.Dispose();$index++
}
$sheet.Save((Join-Path $root 'catalog.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$sg.Dispose();$sheet.Dispose();$font.Dispose()
Write-Output 'Exported 28 round enemy projectile sprites (32x32).'
