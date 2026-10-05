# Unity 2D migration validation

Unity: 6000.5.3f1
Mode: batch editor with graphics, actual Play Mode
Result: PASS

A UnityEditor.Search.SearchDatabase startup indexing exception and optional-service Curl errors were excluded from gameplay failure detection. Gameplay/runtime exceptions remain fatal.

GameplayPreview.png uses the URP SingleCameraRequest path; the camera capture excludes the screen-space HUD.

```text
PASS: Seven towers
PASS: No 3D rigidbody
PASS: No 3D collider
PASS: Player movement
PASS: Power shot
PASS: No missing script: Main Camera
PASS: No missing script: Citadel
PASS: No missing script: Tower
PASS: No missing script: mainTower
PASS: No missing script: Tower 1
PASS: No missing script: Tower 2
PASS: No missing script: Tower 3
PASS: No missing script: Tower 4
PASS: No missing script: Tower 5
PASS: No missing script: Tower 6
PASS: No missing script: Player
PASS: No missing script: playerShotPos
PASS: No missing script: Player Visual
PASS: No missing script: Gameplay Systems
PASS: Title and gameplay in build
PASS: Automatic spawning and live registry
PASS: Automatic tower combat and XP collection
PASS: WASD moves in XY only
PASS: Diagonal speed is normalized
PASS: Area damage respects vertical distance in XY
PASS: Normal shot deals 25 only inside its destination radius
PASS: Can deplete remaining power
PASS: Recovery increases spawn pressure by 1.5
PASS: Insufficient power blocks shot
PASS: Recovery reaches 60 and resumes depletion
PASS: Actual power shot costs 20
PASS: Power explosion eliminates enemies within 2.5 units
PASS: Two kills collect 40 experience
PASS: Tower projectile retargets when original target disappears
PASS: XP crosses multiple thresholds
PASS: Title menu keeps four original buttons
PASS: Unavailable continue is disabled
PASS: Title starts a fresh game with clean registries
PASS: No runtime errors
RUSHITADEL_2D_VALIDATION_SUCCESS
```
