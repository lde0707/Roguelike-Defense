# 전투 아트·보스 패턴 통합

작성일: 2026-10-07. Unity 6000.5.3f1. 실제 실행 코드 기준이며, 원본 기획의 모든 연출을 완성했다는 의미는 아니다.

## 적용 범위

- 기존 플레이어 본체와 기본/ROT-01~16 포탑 외형, 속성별 발광 연결을 유지했다.
- 변형 포탑 공격에 ProjectileArt를 연결했다. 장판·그물 등 영역형 공격은 영역 선을 유지하고 중심에 작은 아이콘을 배치한다.
- 일반 적 탄, 플레이어 기본/우클릭 공격, 포탑 탄, 분열 파편, 폭발·전기 효과를 리소스에 연결했다.
- Resources/BossArt에 기존 탄·레이저·연출 자료 31장과 투명 보스 본체 4장, 나비 드론 1장을 설치했다. 연출 스토리보드 전체를 한 장의 이펙트로 띄우지는 않는다.
- Resources/ProjectileArt에 아군용 리소스 25개, Battlefields에 풀/용암/얼음/물·전기 배경 4개를 설치했다.
- 배경은 테마에 따라 전환하며 기존 월드 크기·스크롤·절벽 경계를 유지한다.
- 각 스테이지 3라운드의 기존 보스 등장 흐름에 새 보스를 연결했다. 타이틀·세이브·증강 선택 흐름은 유지한다.

### 원본이 없는 아군 투사체 처리

받은 저장소에는 아군 투사체 생성 작업 목록만 있고 최종 이미지가 없었다. 따라서 25개가 모두 신규 고유 디자인은 아니다. 기존 보스 탄 원본 중 형태가 맞는 이미지를 아군 색상/크기로 재사용했다. 정확한 대응표는 `tools/install-combat-art.ps1`에 있다. 최종 전용 아군 원본이 생기면 동일한 Resources 이름으로 교체 가능하다.

## 보스 연결

### 후속 판정 동기화

투사체 표시 크기를 판정에도 반영하도록 변경했다. 이동형 아군·적 탄은 표시 크기를 공유하는 캡슐 판정과 연속 CapsuleCast를 사용한다. 원형 탄은 같은 가로/세로의 캡슐(원), 길쭉한 탄은 긴 축을 따라 캡슐이 된다. 투명 여백의 모든 픽셀 윤곽을 그대로 추적하는 방식은 아니다. 적 탄의 절벽 반사 위치도 회전된 캡슐의 수평 반경을 사용한다. 레이저는 확대된 표시 폭의 절반을 판정 반경으로 사용한다. 우클릭 원형 공격도 확대된 원의 반경을 실제 피해·중앙 30% 즉사 범위에 적용한다. 이는 이전의 '표시만 확대' 규칙을 대체한다. 장판/그물의 중심 아이콘은 판정이 아니라 장식이므로 기존 영역 경계를 유지한다.

| 테마 | 보스 | 패턴 5개 |
|---|---|---|
| 풀 | 적월 나비 | 개화 나선, 나비 레이저 릴레이, 날개 가위질, 비늘 장막, 붉은 정원 |
| 불 | 용광로 거신 | 쌍나선 용광로, 절벽 반사 협공, 분열하는 탄벽, 손 레이저 가위질, 용광로 압착 |
| 얼음 | 빙환 수호자 | 엇갈리는 왕복 고리, 부메랑 포대, 반사 회랑, 레이저 릴레이, 빙환 봉쇄 |
| 물·전기 | 수압·전격 쌍성 | 수압 부채·전격 조준, 수압 레이저 릴레이, 도탄 결정 협공, 프리즘 기뢰, 과부하 연계 |

HP 70%/35%에서 패턴 단계를 늘린다. 마지막 단계에서는 복합 공격을 사용한다. 쌍성은 별도 외형·충돌체 두 개와 공유 HP 한 개로 구성한다. 범위공격은 빈 중앙이 아니라 가까운 실제 본체 중심을 기준으로 계산한다.

## 레이저와 탄막 규칙

### 투사체 가독성 보정

투사체 시각 두께를 20% 확대했다. 길쭉한 아군·적·보스 탄은 짧은 축만, 원형 탄은 양 축을 확대한다. 레이저·전기 연결선도 표시 폭만 20% 확대한다. 속도·사거리·피격 판정·폭발/장판 범위는 유지한다. 드론과 보스의 위성 고리는 본체 부품이므로 크기를 유지한다. 공통 배율은 `ProjectileArt.ThicknessMultiplier`에서 조절한다.

후속 식별성 수정: 위 두께 보정 위에 아군 투사체 표시 크기 3배, 적 투사체 2배를 적용했다. 적 레이저는 길이 대신 폭만 2배다. 아군은 청록 몸체·흰 코어·짙은 남색 명암, 적은 적색 몸체·주황 코어·짙은 자주색 명암으로 통일한다. `FactionProjectile.shader`가 원본 PNG의 투명도와 형태를 유지하며 런타임에 색상을 재매핑한다. 원본 PNG 자체를 덮어쓰지 않았다. 속성 포탑 본체 발광은 기존대로 유지하고, 탄의 진영색은 속성과 무관하게 고정한다. 폭발·상태 효과의 범위 표시와 위성 드론/고리 크기는 변경하지 않는다. 표시 크기만 커지므로 실제 판정보다 넓게 보이며, 정밀한 회피 난이도 조정 시 이 차이를 고려해야 한다.

- 충전 1.25초 → 선행 광선 0.10초 → 실제 발사 → 무해한 소멸 0.30초.
- 충전 입자는 발사구로 모인다. 조준 방향은 충전 시작 시 고정되고, 일부 패턴만 제한된 각도로 회전한다.
- 발사 시 광선 끝이 0.15초에 걸쳐 늘어난다. 아직 도달하지 않은 구간에는 피해가 없다.
- 레이저는 절벽 바깥으로 반사되지 않는다. 전기 연결선은 양 끝 기뢰 중 하나를 파괴하면 끊긴다.
- 동시 레이저 최대 2개, 보스 일반 탄 최대 110개, 일반 탄 수명 7초. 패턴마다 도탄 허용 횟수를 지정한다.
- 기존 적 탄 HP와 파괴 시스템을 사용한다. 분열 부모를 미리 파괴하면 자탄이 나오지 않으며, 플레이어 1.4유닛 이내에서는 분열을 지연한다.
- 나비 드론 HP 100, 얼음 고리 HP 160. 파괴 후 3초 뒤 재생성한다. 위성에는 경험치가 없다.
- 고리는 왕복 부메랑 이동과 정지 레이저를 사용한다. 고리 테두리에 판정을 배치해 중앙 구멍을 유지한다.
- 보스 처치 시 소유 탄·위성·레이저를 정리한다. 일시정지 중 충전·피해·이동 시간을 멈춘다.

## 구현 차이 및 밸런스 주의

- 보스 본체는 정적 스프라이트이며, 용암 손의 독립 골격 애니메이션은 포함하지 않았다. 고리의 세우기는 평면 스프라이트 압축 연출이다.
- 콘셉트의 복잡한 세부 동작 대신 현재 충돌/투사체 시스템에 맞춘 20개 패턴을 구현했다. 다중 분할 부위 HP, 쌍둥이 각각의 독립 HP는 아직 없다.
- 기존 우클릭 중앙 30% 즉사 규칙은 보스에도 그대로 적용된다. 이 때문에 패턴이 어려워도 보스를 즉시 처치할 수 있다. 보스 면역·피해 상한은 임의로 추가하지 않았다.
- 탄속·탄 HP·휴식 시간은 초기 구현값이다. 자동 실행 통과는 회피 가능성이나 최종 난이도 검증을 의미하지 않는다. 정상 성장값으로 스테이지 1~3 연속 플레이 테스트가 필요하다.

## 검사와 재현

`Rushitadel.Editor.BossIntegrationValidation.Run`을 Unity batchmode의 executeMethod로 실행한다. 테스트 세이브는 output/boss-integration-20261007/test-saves에 격리한다.

- 검사 범위: 아군 이미지 로딩, 16종 변형 이미지 연결, 4계열/20패턴 발사, 레이저 상한, 충전 무해, 일시정지, 발사 활성, 처치 정리, 쌍성 범위공격 위치.
- 결과: `output/boss-integration-20261007/result.txt`.
- 실제 패턴 중간 캡처: 같은 폴더의 `*-pattern-1.png`, `*-pattern-3.png`.
- `*-laser.png`는 충전 타이밍 검사에서 인위적으로 만든 발사구 캡처이므로 최종 발사 위치 검토에는 사용하지 않는다.

## 이미지 제작 기록

imagegen 스킬에 따라 내장 이미지 생성/편집 모드로 배경이 합쳐진 참고 이미지에서 보스 본체·드론을 투명 스프라이트로 분리했다. 본체는 잘린 상단을 복원한 리터치 결과이며 픽셀 단위 원본 추출은 아니다. 실행 이미지의 조명 의존으로 색이 검게 보이는 문제를 수정해 Unlit 스프라이트로 표시한다. 최종 이미지는 Resources/BossArt에 저장했다.

공통 프롬프트:

> Use case: background-extraction. Input image is edit target. Produce Unity game sprite cutout on genuinely transparent background. [detail] Keep existing top-down angle, silhouettes and colors. Crisp chunky pixel art matte shading, no new design, no floor, shadow, player, cliff, UI or text. Entire object visible with 8% transparent margins.

detail 값:

- Butterfly: Extract ONLY the large green butterfly boss, no drones. Preserve four leaf-veined matte olive wings, red diamond sockets and central red core. Center full boss.
- Lava: Extract the complete lava robot torso and two separated three-finger hands exactly as shown, restore cut-off top of head/shoulders. Transparent gaps between torso and hands. Center symmetrical full upper body with hands.
- Ice: Extract ONLY the central white ice armored tall boss body. Exclude the two floating rings: these will be separate game objects. Restore cut-off top. Central vertical blue gems, cracked white armor.
- Twins: Extract the TWO blue and gold bosses, both complete, centered side by side with generous transparent gap, same size. Keep top bottom and side gems, remove orbiting energy arcs.

드론 프롬프트:

> Background extraction: isolate ONE of the four SMALL two-wing butterfly drones beside the large boss in the reference. Do NOT draw the large boss. Exactly two stubby olive leaf wings, rounded short body, one red circular emitter at bottom facing downward in top-down view. Preserve chunky pixel art, small simple silhouette, matte green. Center on genuine transparent background, no shadow, no ground, no text. Game sprite.

입력 참고 이미지는 docs/제작자료_아카이브_20261007/이미지/첨부참고의 34cf808d(나비), 47f621d2(용암), a1db60f0(얼음), a262ab9a(쌍성) 식별자가 포함된 파일이다.
