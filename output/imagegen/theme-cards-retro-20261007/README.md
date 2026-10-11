# 레트로 테마 카드

내장 imagegen으로 풀·용암·얼음·물·물전기 5종을 새로 생성했다. 큰 픽셀 덩어리와 단순한 절벽길 구도를 사용한다.

게임용 PNG는 `Rushitadel2D/Assets/Resources/ThemeCards/`에 같은 파일명으로 교체했다. 기존 .meta/GUID는 유지하므로 새 게임, 다음 테마 선택, Title 미리보기에서 같은 리소스를 사용한다.

최종 크기 160×80, Point 필터. 원본은 이 폴더의 `*-original.png`, 전체 프롬프트는 `manifest.json`에 보관한다. 종전 이미지 원본은 `output/imagegen/theme-cards-20261007/`에 그대로 남아 있다.

재출력: `tools/export-theme-cards.ps1 -OutputFolder output/imagegen/theme-cards-retro-20261007 -PixelWidth 160`.
