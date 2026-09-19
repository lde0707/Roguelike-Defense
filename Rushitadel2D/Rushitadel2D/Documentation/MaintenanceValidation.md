# 유지보수 구조 변경 검증

Unity 6000.5.3f1 / 실제 Play Mode / PASS

데이터 에셋 12개, 프리팹 연결, 공유 데이터 불변성, 씬의 기존 타워·플레이어 배치와 카메라 배경색 보존을 확인했습니다.

새 테스트 프로젝트에서 발생한 UnityEditor.Search 검색 인덱스 오류는 게임 오류와 분리했습니다. 게임 스크립트 예외는 검증 실패로 처리합니다.

```text
CONTENT_VALIDATION_SUCCESS: 12 definitions, all prefabs and loaded scenes.
PASS: Player is a connected prefab instance
PASS: Tower is a connected prefab instance: Tower 3
PASS: Tower is a connected prefab instance: Tower 2
PASS: Tower is a connected prefab instance: mainTower
PASS: Tower is a connected prefab instance: Tower 1
PASS: Tower is a connected prefab instance: Tower 4
PASS: Tower is a connected prefab instance: Tower 6
PASS: Tower is a connected prefab instance: Tower 5
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
PASS: No missing script: Gameplay Bindings
PASS: Title and gameplay in build
PASS: Automatic spawning and live registry
PASS: Automatic tower combat and XP collection
PASS: Scene bindings connect the spawner to player power
PASS: WASD moves in XY only
PASS: Diagonal speed is normalized
PASS: Enemy instances share the same immutable definition
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
PASS: Shared power asset is unchanged during gameplay
PASS: Title menu keeps four original buttons
PASS: Unavailable continue is disabled
PASS: Title starts a fresh game with clean registries
PASS: No runtime errors
RUSHITADEL_2D_VALIDATION_SUCCESS
LAYOUT_PRESERVED: Tower 2 (4.40, 4.25, 0.00)
LAYOUT_PRESERVED: Tower 1 (-3.10, 4.25, 0.00)
LAYOUT_PRESERVED: Tower 5 (-3.10, -0.55, 0.00)
LAYOUT_PRESERVED: Tower 4 (4.40, 2.30, 0.00)
LAYOUT_PRESERVED: Tower 3 (-3.10, 2.30, 0.00)
LAYOUT_PRESERVED: Tower 6 (4.40, -0.55, 0.00)
LAYOUT_PRESERVED: mainTower (0.60, 1.65, 0.00)
MAINTENANCE_LAYOUT_SUCCESS
```
