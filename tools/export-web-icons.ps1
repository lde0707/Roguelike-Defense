Add-Type -AssemblyName System.Drawing
$root=Split-Path $PSScriptRoot -Parent
$out=Join-Path $root 'output/imagegen/web-icons-20261007'
$dest=Join-Path $root 'Rushitadel2D/Assets/Resources/WebIcons'
$items=Get-Content (Join-Path $out 'manifest.json') -Raw | ConvertFrom-Json
$sheet=[System.Drawing.Bitmap]::new(768,512)
$sg=[System.Drawing.Graphics]::FromImage($sheet)
$sg.Clear([System.Drawing.Color]::FromArgb(22,28,35))
$sg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$sg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
$i=0
foreach($item in $items) {
    Copy-Item -LiteralPath $item.source -Destination (Join-Path $out ($item.id+'-original.png'))
    $src=[System.Drawing.Image]::FromFile($item.source)
    $bmp=[System.Drawing.Bitmap]::new(32,32)
    $g=[System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.DrawImage($src,[System.Drawing.Rectangle]::new(0,0,32,32))
    $bmp.Save((Join-Path $dest ($item.id+'.png')),[System.Drawing.Imaging.ImageFormat]::Png)
    $sg.DrawImage($bmp,[System.Drawing.Rectangle]::new(($i%6)*128,[Math]::Floor($i/6)*128,128,128))
    $g.Dispose();$bmp.Dispose();$src.Dispose();$i++
}
$sheet.Save((Join-Path $out 'icons-preview.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$sg.Dispose();$sheet.Dispose()
