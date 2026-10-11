$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$repo=Split-Path $PSScriptRoot -Parent
$root=Join-Path $repo 'output/imagegen/pressure-20261007'
$items=Get-Content "$root/manifest.json" -Raw | ConvertFrom-Json
New-Item -ItemType Directory -Force "$root/originals" | Out-Null
$preview=New-Object System.Drawing.Bitmap(768,384)
$pg=[System.Drawing.Graphics]::FromImage($preview);$pg.Clear([System.Drawing.Color]::FromArgb(255,28,32,42))
$row=0
foreach($item in $items){
 Copy-Item -LiteralPath $item.source -Destination "$root/originals/$($item.id).png"
 $src=[System.Drawing.Bitmap]::FromFile($item.source)
 $dest=Join-Path $repo "Rushitadel2D/Assets/Resources/PressureArt/$($item.id)"
 New-Item -ItemType Directory -Force $dest | Out-Null
 for($i=0;$i -lt 12;$i++){
  $cx=$i%3;$cy=[int][Math]::Floor($i/3)
  $x0=[int][Math]::Floor($cx*$src.Width/3)+3;$x1=[int][Math]::Floor(($cx+1)*$src.Width/3)-4
  $y0=[int][Math]::Floor($cy*$src.Height/4);$y1=[int][Math]::Floor(($cy+1)*$src.Height/4)-4
  $top=$y1;$bottom=$y0
  for($y=$y0;$y -le $y1;$y++){
   $occupied=0
   for($x=$x0;$x -le $x1;$x++){if($src.GetPixel($x,$y).A -ge 192){$occupied++}}
   if($occupied -gt ($x1-$x0)*.03){$top=[Math]::Min($top,$y);$bottom=[Math]::Max($bottom,$y)}
  }
  $frame=New-Object System.Drawing.Bitmap(256,96)
  $g=[System.Drawing.Graphics]::FromImage($frame);$g.Clear([System.Drawing.Color]::Transparent)
  $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
  $g.DrawImage($src,(New-Object System.Drawing.Rectangle(0,0,256,96)),$x0,$top,($x1-$x0+1),($bottom-$top+1),[System.Drawing.GraphicsUnit]::Pixel)
  $frame.Save((Join-Path $dest ('{0:00}.png' -f $i)),[System.Drawing.Imaging.ImageFormat]::Png)
  if($i -in @(0,5,11)){$col=@(0,5,11).IndexOf($i);$pg.DrawImageUnscaled($frame,($col*256),($row*96))}
  $g.Dispose();$frame.Dispose()
 }
 $src.Dispose();$row++
}
$preview.Save("$root/preview.png",[System.Drawing.Imaging.ImageFormat]::Png);$pg.Dispose();$preview.Dispose()
'Exported 48 pressure animation frames at 256x96.'
