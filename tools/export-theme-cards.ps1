param([string]$OutputFolder='output/imagegen/theme-cards-20261007',[int]$PixelWidth=480)
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root $OutputFolder
$dest = Join-Path $root 'Rushitadel2D/Assets/Resources/ThemeCards'
New-Item -ItemType Directory -Force -Path $dest | Out-Null
$items = Get-Content (Join-Path $out 'manifest.json') -Raw | ConvertFrom-Json
$sheet = [System.Drawing.Bitmap]::new(960,720)
$sg = [System.Drawing.Graphics]::FromImage($sheet)
$sg.Clear([System.Drawing.Color]::FromArgb(20,25,30))
$i=0
foreach($item in $items) {
    Copy-Item -LiteralPath $item.source -Destination (Join-Path $out ($item.id+'-original.png'))
    $src=[System.Drawing.Image]::FromFile($item.source)
    $bmp=[System.Drawing.Bitmap]::new($PixelWidth,($PixelWidth/2))
    $g=[System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $cropW=[Math]::Min($src.Width,$src.Height*2)
    $cropH=$cropW/2
    $g.DrawImage($src,[System.Drawing.Rectangle]::new(0,0,$PixelWidth,($PixelWidth/2)),[single](($src.Width-$cropW)/2),[single](($src.Height-$cropH)/2),[single]$cropW,[single]$cropH,[System.Drawing.GraphicsUnit]::Pixel)
    $bmp.Save((Join-Path $dest ($item.id+'.png')),[System.Drawing.Imaging.ImageFormat]::Png)
    $sg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $sg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $sg.DrawImage($bmp,[System.Drawing.Rectangle]::new(($i%2)*480,[Math]::Floor($i/2)*240,480,240))
    $g.Dispose();$bmp.Dispose();$src.Dispose();$i++
}
$sheet.Save((Join-Path $out 'preview.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$sg.Dispose();$sheet.Dispose()
