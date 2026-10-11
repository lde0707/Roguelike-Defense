# 테마 선택 카드

내장 imagegen으로 생성한 픽셀 지형 일러스트 5종. 원본과 최종 프롬프트는 이 폴더의 `*-original.png`, `manifest.json`에 보관한다.

게임용 파일: `Rushitadel2D/Assets/Resources/ThemeCards/`의 Grass, Fire, Ice, Water, WaterElectric PNG.

- 크기: 480 × 240, 불투명 PNG, Point 필터, 무압축.
- 용도: 새 게임 테마 선택과 라운드 진행 중 다음 테마 선택 카드.
- 공통 구도: 중앙 절벽길, 양옆 높은 지형, 아래쪽 어두운 제목 여백.
- 캐릭터와 글자는 이미지에 포함하지 않는다. 제목, 선택 테두리와 마우스 반응은 UI로 표시한다.
- 재출력: `tools/export-theme-cards.ps1` (원본 중앙 크롭 및 최근접 리샘플).

색 구분: 풀은 올리브/세이지, 용암은 숯색/주황, 얼음은 옅은 청백색, 물은 청록색, 물·전기는 남청색/금색 전기.
