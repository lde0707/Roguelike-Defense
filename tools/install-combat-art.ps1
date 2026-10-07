$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$target=Join-Path $repo 'Rushitadel2D/Assets/Resources'
$sets=@{Butterfly='butterfly-boss-20261007';Lava='lava-boss-projectiles-20261007';Ice='ice-ring-boss-20261007';Twins='twin-boss-projectiles-20261007'}
foreach($name in $sets.Keys){
  $dest=Join-Path $target "BossArt/$name"
  New-Item -ItemType Directory -Force $dest | Out-Null
  Get-ChildItem (Join-Path $repo "output/imagegen/$($sets[$name])") -Filter '*.png' | Copy-Item -Destination $dest
}
# The incoming commit contains the friendly-shot job list but not its rendered files.
# Reuse the existing transparent projectile designs; never ship missing-resource references.
$aliases=@{
 PlayerBasic='Twins/02-voltaic-dart';PlayerPower='Twins/08-overload-core';TowerBasic='Lava/02-slag-dart';
 'ROT-01'='Lava/02-slag-dart';'ROT-02'='Butterfly/02-thorn-needle';'ROT-03'='Ice/04-heavy-prism';
 'ROT-04'='Ice/01-ring-boomerang';'ROT-05'='Lava/03-armored-magma';'ROT-06'='Ice/05-frost-dart';
 'ROT-07'='Butterfly/04-seed-pod';'ROT-08'='Twins/05-prism-mine';'ROT-09'='Ice/02-crescent-boomerang';
 'ROT-10'='Ice/01-ring-boomerang';'ROT-11'='Twins/04-tidal-crescent';'ROT-12'='Twins/03-ricochet-crystal';
 'ROT-13'='Butterfly/05-scale-kite';'ROT-14'='Twins/08-overload-core';'ROT-15'='Ice/03-ricochet-orb';'ROT-16'='Twins/02-voltaic-dart';
 SplitFragment='Ice/05-frost-dart';BlastRing='Ice/01-ring-boomerang';WebPoison='Butterfly/04-seed-pod';
 WebBlast='Lava/04-cluster-bomb';WebPierce='Twins/02-voltaic-dart';'S3-13'='Twins/07-lightning-tether'
}
$dest=Join-Path $target 'ProjectileArt'
New-Item -ItemType Directory -Force $dest | Out-Null
foreach($name in $aliases.Keys){Copy-Item -LiteralPath (Join-Path $target "BossArt/$($aliases[$name]).png") -Destination (Join-Path $dest "$name.png")}
Write-Output "Installed $($aliases.Count) friendly projectile resources and four boss effect sets."
