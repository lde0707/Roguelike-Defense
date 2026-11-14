# Rushitadel 실행 안내

<!-- WINDOWS-RELEASE-START -->
## Windows에서 바로 플레이

**[Rushitadel · Windows 실행 파일 다운로드](https://github.com/lde0707/portfolio-game-builds/releases/tag/rushitadel-2026-11-14-r2)**

포탑과 직접 공격을 조합해 적의 공격을 버티는 액션 디펜스 게임.

1. 위 Releases 페이지의 `Rushitadel-Windows-x64.zip`을 내려받습니다. `Source code` 파일은 실행 파일이 아닙니다.
2. ZIP 전체를 한 폴더에 압축 해제합니다.
3. `Rushitadel.exe`를 실행합니다. Unity 설치는 필요 없습니다.

`Rushitadel_Data`, `UnityPlayer.dll`, 나머지 동봉 폴더를 함께 유지해야 합니다. Windows 64비트용이며 모바일·macOS에서는 실행되지 않습니다.

**조작:** WASD 이동 / 마우스 조준 / 좌클릭 원거리 공격 / 우클릭 동력 공격 / Esc 일시정지.

[실행·검증 범위](PLAY.md) · [QA 포트폴리오](https://leedoeon.vercel.app/qa/index.html)
<!-- WINDOWS-RELEASE-END -->


## 배포 버전과 확인 범위

- Windows x64 / 검증일 2026-11-14
- 이동·공격, 경험치 획득, 라운드 전환, 포탑 변형 선택, 일시정지와 타이틀 복귀를 확인했습니다. 상단 이동·반동의 화면 이탈 수정도 같은 입력으로 재검증했습니다.
- 모든 테마·보스 완주와 이전 버전 전체 저장 파일 호환성은 확인하지 않았습니다.
- [실제 입력 검증 기록](PLAYTEST.md)

## 소스에서 열기

Unity Hub에서 `Rushitadel2D` 폴더를 엽니다. `ProjectSettings/ProjectVersion.txt`와 같은 버전의 에디터 및 Windows Build Support가 필요합니다.
현재 Build Settings에 등록된 씬으로 Windows x64를 빌드합니다. `Library`, `Temp`, `Logs`, `Builds`는 재생성 파일이므로 Git에서 제외합니다.

## 문제가 있을 때

ZIP 전체를 다시 압축 해제하고 EXE와 Data 폴더가 같은 위치인지 확인하세요. 실행 오류를 보고할 때 OS·발생 화면·재현 순서를 함께 남겨주세요.
게임의 조작·기능 범위는 실제 빌드와 최신 코드가 기준이며, 아래 기획·과거 개발 문서에는 아직 구현되지 않은 내용이 포함될 수 있습니다.
