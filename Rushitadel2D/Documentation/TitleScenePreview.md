# 최신 Title 씬 확인

## 사용법

1. Assets/Scenes/Title.unity를 연다. 이미 열려 있었다면 디스크 변경을 다시 불러온다. 미저장 편집 내용은 먼저 별도로 보존한다.
2. Hierarchy의 `LATEST TITLE UI - Select Preview Page`를 선택한다.
3. Inspector 버튼으로 타이틀 / 새 게임 슬롯 / 이어하기 슬롯 / 설정 표시·사운드·조작 / 종료 확인 / 시작 테마 선택의 8개 화면을 선택한다. Game 뷰에서 배치를 확인하고 하위 Image/Button/Text의 이미지와 hover/pressed 상태를 Inspector에서 확인한다.
4. Play에서 실제 클릭, 저장 불러오기, 설정 적용, 드롭다운 스크롤, 배경 스크롤, 커서 색 반전, 도트 셔터 전환을 확인한다.

## 구조와 한계

이전 타이틀 Canvas를 제거하고 최신 GameFrontend 생성 코드를 이용해 8개 미리보기를 씬에 저장했다. 미리보기 루트는 EditorOnly이며 Play에서는 꺼진다. 실제 동작은 기존 GameFrontend 하나가 담당하므로 입력 시스템이나 UI가 중복되지 않는다.

미리보기는 **런타임 코드에서 생성한 편집용 스냅샷**이다. 여기서 Image 위치나 버튼을 직접 바꿔도 실행 UI에 반영되는 구조는 아니다. 실제 UI를 수정하려면 GameFrontend의 레이아웃/동작 또는 Resources/TitleUI 이미지를 수정한 뒤 `Rushitadel > Refresh Title Scene Preview`를 실행한다. 저장 슬롯 내용도 생성 시점 스냅샷이며, 실행할 때 실제 저장 상태를 다시 읽는다. 편집 모드에서 저장/삭제/종료 기능을 실행하지 않는다.

## 검증

별도 Unity 복사본에서 `RUSHITADEL_TITLE_UI_VALIDATION_SUCCESS` 확인. 시작, 이어하기와 저장 검증, 설정 및 드롭다운, 종료 확인/취소, hover, 스크롤, 커서 반전, 셔터 전환을 검사했다. 로그: output/title-scene-validation.log. 화면 캡처: output/title-scene-20261007.

기존 UI 검사 도구의 오래된 범위탄 판정 기대값을 현재 규칙(보이는 반지름과 일치)으로 갱신했고, 새 출력 폴더를 자동 생성하도록 보완했다. Unity 자체 검색 인덱스 오류는 게임 UI 오류와 구분해 제외한다.
