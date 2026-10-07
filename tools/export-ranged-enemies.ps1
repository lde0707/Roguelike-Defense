param([string]$Manifest='output/imagegen/ranged-enemies-20261007/manifest.json')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$repo=Split-Path $PSScriptRoot -Parent
$root=Join-Path $repo 'output/imagegen/ranged-enemies-20261007'
$items=Get-Content (Join-Path $repo $Manifest) -Raw | ConvertFrom-Json
$sheet=New-Object System.Drawing.Bitmap(1120,720)
$sg=[System.Drawing.Graphics]::FromImage($sheet)
$sg.Clear([System.Drawing.Color]::FromArgb(255,27,31,41))
$sg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$sg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
$font=New-Object System.Drawing.Font('Malgun Gothic',10)
$brush=[System.Drawing.Brushes]::White
$index=0
foreach($item in $items){
  if(-not (Test-Path -LiteralPath $item.source)){throw "Missing $($item.source)"}
  $dest=Join-Path $root "32x32/$($item.theme)"
  $originals=Join-Path $root "originals/$($item.theme)"
  New-Item -ItemType Directory -Force $dest,$originals | Out-Null
  Copy-Item -LiteralPath $item.source -Destination (Join-Path $originals "$($item.id).png")
  $src=[System.Drawing.Bitmap]::FromFile($item.source)
  $left=$src.Width;$top=$src.Height;$right=-1;$bottom=-1
  for($y=0;$y -lt $src.Height;$y++){
    for($x=0;$x -lt $src.Width;$x++){
      if($src.GetPixel($x,$y).A -gt 64){
        $left=[Math]::Min($left,$x);$top=[Math]::Min($top,$y)
        $right=[Math]::Max($right,$x);$bottom=[Math]::Max($bottom,$y)
      }
    }
  }
  if($right -lt $left){throw "Empty image $($item.id)"}
  $w=$right-$left+1;$h=$bottom-$top+1
  $fit=if($item.id.StartsWith('E')){28}else{26}
  $ratio=$fit/[Math]::Max($w,$h)
  $dw=[Math]::Max(1,[int][Math]::Round($w*$ratio));$dh=[Math]::Max(1,[int][Math]::Round($h*$ratio))
  $dst=New-Object System.Drawing.Bitmap(32,32)
  $g=[System.Drawing.Graphics]::FromImage($dst)
  $g.Clear([System.Drawing.Color]::Transparent)
  $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
  $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
  $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
  $rect=New-Object System.Drawing.Rectangle([int][Math]::Floor((32-$dw)/2),[int][Math]::Floor((32-$dh)/2),$dw,$dh)
  $g.DrawImage($src,$rect,$left,$top,$w,$h,[System.Drawing.GraphicsUnit]::Pixel)
  $dst.Save((Join-Path $dest "$($item.id).png"),[System.Drawing.Imaging.ImageFormat]::Png)
  $col=$index%7;$row=[int][Math]::Floor($index/7)
  if($col -eq 0){
    $mapName=@{Grass='grass';Lava='lava';Ice='ice';WaterElectric='water-electric'}[$item.theme]
    $map=[System.Drawing.Bitmap]::FromFile((Join-Path $repo "Rushitadel2D/Assets/Resources/Battlefields/$mapName.png"))
    $sg.DrawImage($map,(New-Object System.Drawing.Rectangle(0,($row*180),1120,136)),450,160,700,180,[System.Drawing.GraphicsUnit]::Pixel)
    $map.Dispose()
  }
  $sg.DrawImage($dst,(New-Object System.Drawing.Rectangle(($col*160+16),($row*180+8),128,128)))
  $sg.DrawString("$($item.themeKo) $($item.id)",$font,$brush,($col*160+8),($row*180+138))
  $sg.DrawString($item.name,$font,$brush,($col*160+8),($row*180+157))
  $g.Dispose();$dst.Dispose();$src.Dispose();$index++
}
$sheet.Save((Join-Path $root 'catalog-4x.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$sg.Dispose();$sheet.Dispose();$font.Dispose()
Write-Output "Exported $index transparent 32x32 sprites and 4x catalog."
