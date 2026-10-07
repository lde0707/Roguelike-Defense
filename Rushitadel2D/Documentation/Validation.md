# Unity 전투 검증

2026-10-07 변경: 우클릭 강공격의 플레이어 넉백과 시각적 뒤로 밀림을 제거했다. 아래 2026-10-05 검증 기록의 우클릭 반동 항목은 이전 동작이며, 현재 `CombatRulesProbe`는 발사 후 위치가 유지되는지 검사한다. 이번 변경 후 전체 Play Mode 검사는 재실행하지 않았다.

검증일: 2026-10-05. Unity 6000.5.3f1, Batch Editor + 실제 Play Mode. 결과: PASS.

`PortValidation.BuildAndRun`으로 씬·프리팹을 생성하고 `PortValidation.Run`으로 최종 코드를 검증했다. 총 194개 검사가 통과했다. 원본 로그: `.work/forward-20261005/unity-validation-final.log`.

## 전진 압박 통합 검사

- 카메라 독립 전진·진행 거리, 정지한 플레이어의 화면 후퇴, W 전진으로 추격과 거리 벌리기.
- 화면 상단 장벽·하단 위험 구역의 카메라 상대 위치 고정. 상단 이동·반동 통과 차단.
- 후방 연속 접촉 1초마다 최대 HP 50% 피해. 일반 피격 무적과 독립. 이탈 시 타이머 초기화.
- 일시정지 중 카메라·진행 거리·위험 구역 타이머·HP 정지.
- 5개 테마의 명칭·프로토타입 팔레트 전환.
- 반복 배경 이음새 전후와 Y=1080.25·10000.25에서 패널 연결·화면 덮기.
- 카메라가 이동해도 적 탄 월드 좌표·도탄 방향이 임의로 바뀌지 않음.
- 진행한 카메라 전방의 절벽 밖 적 생성, 후방 이탈 적 무보상 정리.
- 새 게임 위치에서 아무 입력 없이 기다리면 실제 후방 위험 피해로 패배. 패배 후 카메라 정지와 재시작 초기화.

## 기존 전투 회귀 검사

5포탑 부착·회전·정면 5유닛 사격, 마우스 기본 공격·우클릭 실제 입력, HP·XP·기력, 절벽 진입/도탄·고속 충돌·적 탄 HP·파괴, 우클릭 최대 기력 70%·짧은 반동·지름 1.5배·중앙 반지름 30% 즉사·최대 HP 비례 외곽 100%→50%, 일시정지·패배·타이틀·새 게임도 함께 검사했다.

선택적 온라인 서비스의 Curl 로그와 Unity Search 인덱스 예외는 게임 오류 집계에서 제외한다. Rushitadel 스크립트 오류·예외·Assert는 실패로 처리한다.

`GameplayPreview.png`는 URP 카메라 캡처이며 화면 공간 HUD는 포함하지 않는다. 최종 테마 아트·장시간 밸런스·스탠드얼론 빌드는 검증 범위 밖이다. 장거리 좌표 검사는 반복 타일 배치의 검사이며 장시간 실기 플레이를 의미하지 않는다.

## 최종 통과 로그

```text
PASS: Five attached turrets
PASS: No 3D rigidbody
PASS: No 3D collider
PASS: Player movement
PASS: Mouse aimed player attack
PASS: Power shot
PASS: No missing script: Main Camera
PASS: No missing script: Cliff Boundaries
PASS: No missing script: Left Invisible Wall
PASS: No missing script: Right Invisible Wall
PASS: No missing script: Rear Theme Hazard
PASS: No missing script: Zone Fill
PASS: No missing script: Theme Detail 0
PASS: No missing script: Theme Detail 1
PASS: No missing script: Theme Detail 2
PASS: No missing script: Theme Detail 3
PASS: No missing script: Theme Detail 4
PASS: No missing script: Theme Detail 5
PASS: No missing script: Theme Detail 6
PASS: No missing script: Theme Detail 7
PASS: No missing script: Theme Detail 8
PASS: No missing script: Theme Detail 9
PASS: No missing script: Theme Detail 10
PASS: No missing script: Theme Detail 11
PASS: No missing script: Theme Detail 12
PASS: No missing script: Theme Detail 13
PASS: No missing script: Theme Detail 14
PASS: No missing script: Theme Detail 15
PASS: No missing script: Front Theme Barrier
PASS: No missing script: Zone Fill
PASS: No missing script: Theme Detail 0
PASS: No missing script: Theme Detail 1
PASS: No missing script: Theme Detail 2
PASS: No missing script: Theme Detail 3
PASS: No missing script: Theme Detail 4
PASS: No missing script: Theme Detail 5
PASS: No missing script: Theme Detail 6
PASS: No missing script: Theme Detail 7
PASS: No missing script: Theme Detail 8
PASS: No missing script: Theme Detail 9
PASS: No missing script: Theme Detail 10
PASS: No missing script: Theme Detail 11
PASS: No missing script: Theme Detail 12
PASS: No missing script: Theme Detail 13
PASS: No missing script: Theme Detail 14
PASS: No missing script: Theme Detail 15
PASS: No missing script: Looping Battlefield
PASS: No missing script: Ground Segment 0
PASS: No missing script: Ground Segment 1
PASS: No missing script: Ground Segment 2
PASS: No missing script: Player
PASS: No missing script: Rotating Body
PASS: No missing script: Hull
PASS: No missing script: Armor
PASS: No missing script: Core Ring
PASS: No missing script: Power Core
PASS: No missing script: T1
PASS: No missing script: Mount
PASS: No missing script: Barrel
PASS: No missing script: Emitter
PASS: No missing script: Muzzle
PASS: No missing script: T2
PASS: No missing script: Mount
PASS: No missing script: Barrel
PASS: No missing script: Emitter
PASS: No missing script: Muzzle
PASS: No missing script: T3
PASS: No missing script: Mount
PASS: No missing script: Barrel
PASS: No missing script: Emitter
PASS: No missing script: Muzzle
PASS: No missing script: T4
PASS: No missing script: Mount
PASS: No missing script: Barrel
PASS: No missing script: Emitter
PASS: No missing script: Muzzle
PASS: No missing script: T5
PASS: No missing script: Mount
PASS: No missing script: Barrel
PASS: No missing script: Emitter
PASS: No missing script: Muzzle
PASS: No missing script: Power Shot Origin
PASS: No missing script: Gameplay Systems
PASS: Title and gameplay in build
PASS: Automatic enemy spawning
PASS: Outside ranged enemies automatically launch hostile projectiles
PASS: Five attached stable slots T1-T5
PASS: One player survival HP owner
PASS: Automatic body rotation
PASS: Normalized WASD movement in XY
PASS: Turrets move with player without slot drift
PASS: All five turret ranges are 5 world units
PASS: Turret fires forward with no enemies present
PASS: Already fired shell keeps its direction while the rig rotates
PASS: Forward shell hits an enemy on its path and ignores enemy behind turret
PASS: Swept collision hits the first enemy once without tunnelling
PASS: Shell stops at exactly 5 units and cannot hit enemy beyond range
PASS: Mouse aimed attack fires and respects cooldown
PASS: Player aim follows cursor independently of turret rotation
PASS: Basic mouse attack does not spend power
PASS: Player basic projectile deals collision damage
PASS: Actual left mouse input fires a player projectile
PASS: Enemy pursues player instead of turret slots
PASS: Enemy contact damages player HP
PASS: Hit invulnerability prevents same-frame damage stacking
PASS: Healing clamps at maximum HP
PASS: Power can reach zero
PASS: Zero power locks power attack
PASS: Recovery remains locked below 60 percent
PASS: Power unlocks at threshold and clamps at maximum
PASS: Cliffs have two solid invisible player walls
PASS: Cliff collision faces match the current battlefield path edges
PASS: Player collider cannot cross the right cliff
PASS: Player collider cannot cross the left cliff
PASS: Recoil also stops at the invisible cliff wall
PASS: Enemy bullet enters from left without reflection
PASS: The same bullet reflects only when leaving the far cliff
PASS: Reflected bullet does not stick to or retrigger the wall
PASS: Enemy bullet enters from right without reflection
PASS: The same bullet reflects only when leaving the far cliff
PASS: Reflected bullet does not stick to or retrigger the wall
PASS: Fast bullet can enter and ricochet repeatedly within one frame
PASS: Bullet outside the lane moving away does not ricochet
PASS: Parallel bullet has no false wall collision
PASS: Ricochet preserves tangential velocity and reflects the normal component
PASS: Hostile projectile has HP and survives partial damage
PASS: Destroyed projectile is removed once and grants no enemy XP
PASS: Friendly shot crosses the cliff to damage outside enemies
PASS: Swept friendly shell destroys hostile bullet on first contact
PASS: Moving hostile bullet detects an incoming friendly shell and loses its damage amount
PASS: Attached automatic turret can destroy an enemy projectile
PASS: Player can aim basic fire at an enemy projectile
PASS: Player basic attack destroys enemy projectile HP
PASS: Enemy projectile sweeps into player, damages once and disappears
PASS: Radial damage is 100 percent in inner 30 percent, falling to 50 percent at rim
PASS: Power area diameter is 150 percent of player body diameter
PASS: Right click spends exactly 70 percent of maximum power (140 of 200)
PASS: Visible power orb matches its actual damage radius
PASS: Insufficient power blocks another shot without spending
PASS: Right click produces a brief small recoil opposite aim
PASS: Inner target instantly dies; outer target takes maximum-HP proportional falloff damage
PASS: Power shot deals exactly 50 percent max HP at rim and nothing outside
PASS: Power area destroys central enemy bullets and damages edge bullet HP
PASS: Recoil ends without persistent drift
PASS: Actual right mouse input launches the area attack and consumes 70 of 100 power
PASS: Exactly 70 percent power can fire and enters recovery at zero
PASS: Recovery unlock at 60 percent does not bypass 70 percent firing cost
PASS: Pause freezes enemy projectile movement and lifetime
PASS: Hostile projectile expires to keep repeated ricochets bounded
PASS: Enemy death awards and collects XP once
PASS: Pause freezes combat and resources
PASS: Pause prevents XP collection even inside collection radius
PASS: Pause blocks power spending
PASS: Pause blocks basic mouse attack
PASS: Pause blocks right mouse attack
PASS: Resume restores time
PASS: Resume collects pending XP once
PASS: Camera advances steadily in world space and records forward distance
PASS: Idle player stays in the world but falls behind on screen
PASS: Ground is world-fixed: camera motion creates downward screen scroll
PASS: Rear pursuit and front barrier stay at fixed camera-relative positions
PASS: Rear danger is a damage volume; front barrier is solid
PASS: Forward input overtakes camera speed and escapes the rear threat
PASS: Player cannot pass the moving front barrier
PASS: Recoil cannot push player through the front barrier
PASS: Rear hazard warns immediately and waits one second before first damage
PASS: Rear hazard ticks for 50 percent maximum HP and ignores normal hit invulnerability
PASS: Leaving the hazard clears its exposure timer
PASS: Pause freezes camera advance, pursuit exposure, damage and distance
PASS: Theme pursuit profile available: Grass
PASS: Theme pursuit profile available: Fire
PASS: Theme pursuit profile available: Ice
PASS: Theme pursuit profile available: Water
PASS: Theme pursuit profile available: Electric
PASS: All five themes use distinct hazard palettes
PASS: Repeated background covers view without gaps at world Y=10.79
PASS: Repeated background covers view without gaps at world Y=10.81
PASS: Repeated background covers view without gaps at world Y=1080.25
PASS: Repeated background covers view without gaps at world Y=10000.25
PASS: Camera advance does not add world velocity or false ricochets to enemy bullets
PASS: Enemies spawn ahead of the advancing camera outside the cliffs
PASS: Enemies passed behind camera are culled without XP so spawn capacity recovers
PASS: Environmental HP zero causes one defeat and cannot resume or heal
PASS: Defeat blocks basic mouse attack
PASS: Defeat blocks right mouse attack
PASS: Title restores time and menu
PASS: Continue unavailable without checkpoint persistence
PASS: New game resets five-turret registry
PASS: New game resets HP
PASS: New game resets pursuit distance and hazard timer
PASS: Standing still from new-game spawn naturally leads to pursuit-hazard defeat
PASS: Defeat freezes the advancing camera
PASS: Restart after pursuit defeat resets player, camera advance and distance
PASS: No runtime errors
RUSHITADEL_2D_VALIDATION_SUCCESS
```
