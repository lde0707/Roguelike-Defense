$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$resources=Join-Path $root 'Rushitadel2D/Assets/Resources'
$aliases=Get-Content (Join-Path $root 'docs/combat-art-aliases.json') -Raw | ConvertFrom-Json -AsHashtable
foreach($target in $aliases.Keys) {
    $source=Join-Path $resources ($aliases[$target]+'.png')
    if(!(Test-Path -LiteralPath $source)){throw "Missing sprite: $source"}
    Copy-Item -LiteralPath $source -Destination (Join-Path $resources ($target+'.png'))
}
Write-Output "Installed $($aliases.Count) shared boss projectile sprites; retained existing GUIDs."
