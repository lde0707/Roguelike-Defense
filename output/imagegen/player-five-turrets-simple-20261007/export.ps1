Add-Type -AssemblyName System.Drawing
$sourceDir = 'C:\Users\doun0\.codex\generated_images\01a113c5-4677-7e83-a135-d348736d4836'
$items = @(
    @('01-mint-disc','exec-3ece2616-158e-4fb7-b596-463d5ae37c61.png'),
    @('02-sand-pentagon','exec-139224ea-d6a4-43e0-a775-ccc7c99880ec.png'),
    @('03-coral-petal','exec-49e01318-a59f-47be-801b-431dad1613e8.png'),
    @('04-blue-ring','exec-65da8c18-4a74-4944-92c6-91f8b0c89879.png'),
    @('05-ivory-shield','exec-24037849-02ca-4e8a-a140-bf80053b3792.png')
)
$originalDir = Join-Path $PSScriptRoot 'originals'
New-Item -ItemType Directory -Force -Path $originalDir | Out-Null
$preview = [System.Drawing.Bitmap]::new(1440,360)
$pg = [System.Drawing.Graphics]::FromImage($preview)
$pg.Clear([System.Drawing.Color]::FromArgb(29,33,45))
$font = [System.Drawing.Font]::new('Segoe UI',14)
$sheet = [System.Drawing.Bitmap]::new(240,48)
for ($i=0; $i -lt $items.Count; $i++) {
    $name = $items[$i][0]
    $sourcePath = Join-Path $sourceDir $items[$i][1]
    Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $originalDir ($name+'.png'))
    $src = [System.Drawing.Bitmap]::new($sourcePath)
    $small = [System.Drawing.Bitmap]::new(48,48)
    $transparent = 0
    for ($y=0; $y -lt 48; $y++) {
        for ($x=0; $x -lt 48; $x++) {
            $c = $src.GetPixel([int][Math]::Floor(($x+0.5)*$src.Width/48),[int][Math]::Floor(($y+0.5)*$src.Height/48))
            $small.SetPixel($x,$y,$c)
            $sheet.SetPixel($i*48+$x,$y,$c)
            if ($c.A -eq 0) { $transparent++ }
            if ($c.A -gt 0) {
                $brush = [System.Drawing.SolidBrush]::new($c)
                $pg.FillRectangle($brush,$i*288+$x*6,16+$y*6,6,6)
                $brush.Dispose()
            }
        }
    }
    $small.Save((Join-Path $PSScriptRoot ($name+'-48.png')),[System.Drawing.Imaging.ImageFormat]::Png)
    $pg.DrawString($name,$font,[System.Drawing.Brushes]::White,$i*288+24,322)
    [PSCustomObject]@{Name=$name;Width=$small.Width;Height=$small.Height;TransparentPixels=$transparent}
    $small.Dispose()
    $src.Dispose()
}
$sheet.Save((Join-Path $PSScriptRoot 'player-ships-sheet-240x48.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview.Save((Join-Path $PSScriptRoot 'preview-6x.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
$font.Dispose()
$pg.Dispose()
$preview.Dispose()

