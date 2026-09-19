# 유지보수 가이드

## 어디에서 수정하나요?

| 변경 목적 | 수정 위치 |
| --- | --- |
| 플레이어 이동 속도, 일반 사격 속도·피해 | Assets/Data/Player/Player.asset |
| 파워샷 비용·범위·시각 배율 | Assets/Data/Player/PowerShot.asset |
| 적 체력·이동 속도·정지 거리 | Assets/Data/Combat/Enemy.asset |
| 중앙·외곽 타워 사거리·피해·발사 간격 | Assets/Data/Combat/MainTower.asset, OuterTower.asset |
| 탄환 이동 속도·수명·유도탄 명중 거리 | Assets/Data/Combat/HomingProjectile.asset, AreaProjectile.asset |
| 일반탄 도착 지점의 광역 판정 반경 | Assets/Data/Combat/MouseWorldInteractionSettings.asset |
| 파워 최대치·소모/회복 속도·회복 중 적 배율 | Assets/Data/Power/PowerCycle.asset |
| 경험치 게이지 기준, 드롭 경험치·수집 속도·거리 | Assets/Data/Progression/Experience.asset, Pickup.asset |
| 웨이브 간격·기본 적 수·좌우 생성 위치와 범위 | Assets/Data/Spawning/Wave.asset |
| 외형·충돌체·컴포넌트·어떤 데이터를 사용할지 | Assets/Prefabs 하위 프리팹 |
| 오브젝트 배치·카메라·프리팹 인스턴스 간 연결 | Assets/Scenes/inGame.unity |
| 게임 규칙과 알고리즘 | Assets/Scripts의 해당 기능 폴더 |

## 책임 구분

**Data**는 여러 인스턴스가 공유하는 ScriptableObject 설정입니다. private SerializeField와 읽기 전용 프로퍼티로 외부 쓰기를 제한합니다. 현재 체력·파워·경험치·타이머를 여기에 저장하지 않습니다. 같은 Enemy 데이터를 사용하는 두 적도 현재 체력은 각각 가집니다.

**Prefabs**는 재사용할 오브젝트의 조합입니다. Actors에는 Player/Enemy, Towers에는 MainTower/OuterTower, Projectiles에는 두 탄환, Pickups에는 경험치, Systems에는 GameplaySystems, Environment에는 Citadel이 있습니다. 씬의 플레이어·7개 타워·게임 시스템·성채는 연결된 프리팹 인스턴스입니다.

**Scenes**는 인스턴스 위치와 카메라를 배치합니다. Gameplay Bindings 오브젝트가 씬의 PlayerPower와 EnemySpawner를 연결합니다. 플레이어 프리팹에 씬 카메라를 저장하지 않고 런타임에 MainCamera를 확인합니다. 프리팹의 내부 shotPos, 파워 컴포넌트, 시각 오브젝트 연결은 프리팹 내부에서 유지합니다.

**Art**에는 일반 스프라이트와 머티리얼이 있습니다. 기존 Image/Animations는 원본 에셋과 메타 GUID를 유지합니다. 더 이상 Generated 폴더에 밸런스 데이터와 시각 에셋을 섞지 않습니다.

## 기능 확장

1. 밸런스만 다른 적·타워는 해당 Data 에셋을 복제하여 값을 수정합니다.
2. 외형/컴포넌트가 다르면 기존 프리팹을 기반으로 별도 프리팹 또는 Prefab Variant를 만듭니다.
3. 새 프리팹의 Definition에 새 Data 에셋을 연결합니다. 연결할 탄환/적 필드는 해당 컨트롤러가 있는 프리팹만 받습니다.
4. 씬에서 인스턴스를 배치하거나 EnemySpawner의 적 프리팹을 교체합니다.
5. Rushitadel 2D → Validate Gameplay Content를 실행하고 해당 동작을 Play Mode에서 확인합니다.

공유 Data를 수정하면 이를 참조하는 모든 인스턴스가 함께 변경됩니다. 한 종류만 다르게 만들 때는 Data를 복제하세요. 런타임 버프나 업그레이드는 공유 Data를 덮어쓰지 말고 해당 캐릭터/타워의 실행 상태 또는 별도 modifier로 적용하세요.

## 에디터 도구와 검증

이전 Rebuild Gameplay Scene 메뉴는 제거했습니다. 수정한 씬·프리팹을 코드의 기본값으로 재생성하지 않습니다. Validate Gameplay Content는 데이터 범위, Definition/프리팹 참조, 누락된 스크립트를 검사하며 원본 에셋을 변경하지 않습니다. 데이터 값 정규화 검사는 임시 복제본에만 적용합니다.

Definition이 빠진 컴포넌트는 시작 시 대상 이름과 함께 오류를 남기고 비활성화합니다. 새 데이터 종류는 GameplayDefinition의 검증 계약을 구현합니다. 탄환 공통 동작은 ProjectileBehaviour에 있고, 피해·파워·스폰 배율은 기존 인터페이스 계약으로 구분합니다.

PortValidation.Run 배치 검증은 콘텐츠 검사, 프리팹 연결, 이동·전투·경험치·파워·타이틀 흐름을 확인합니다. 테스트에서 회복 속도를 높일 때도 공유 에셋을 수정하지 않고 임시 복제본을 사용합니다.

마이그레이션은 저장된 기존 수치를 읽어 Data를 생성하고 에셋 이동 시 GUID를 보존했습니다. 현재 저장된 카메라 설정과 배치는 유지했습니다. 구조 변경 전 파일은 프로젝트의 MaintenanceBackup~ 폴더에 보관합니다.
