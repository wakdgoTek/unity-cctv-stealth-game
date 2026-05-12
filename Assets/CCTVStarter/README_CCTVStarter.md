# CCTV Starter

Unity 6000.3.13f1에서 바로 테스트할 수 있는 CCTV 감지 샘플입니다.

## 빠른 테스트

1. Unity로 돌아가서 스크립트 컴파일이 끝날 때까지 기다립니다.
2. 상단 메뉴에서 `Tools > CCTV Starter > Create Demo Scene`을 누릅니다.
3. Play 버튼을 누릅니다.
4. 방향키 또는 WASD로 Player를 움직입니다.

Player가 CCTV 시야 안에 1.2초 이상 보이면 파란색에서 빨간색으로 바뀝니다.
벽 뒤에 있으면 Raycast가 막혀서 감지되지 않습니다.

## 미니게임 만들기

상단 메뉴에서 `Tools > CCTV Starter > Create Stealth Mini Game`을 누르면 큰 잠입 맵이 자동 생성됩니다.

- WASD: 이동
- 마우스: 1인칭 시점
- 초록 구역: 목적지
- CCTV에 들키면 시작 위치로 돌아갑니다.
- 목적지에 도착하면 성공입니다.
- R: 다시 시작
- CCTV는 위치가 고정된 상태로 시점만 좌우로 회전합니다.
- 맵은 3개 보안 구역, 엄폐 벽, 기둥, 상자 더미, 경고 라인, 조명으로 구성됩니다.
- CCTV 10대가 서로 다른 각도와 속도로 감시해서 이전보다 난이도가 높습니다.

## GitHub에 올릴 때

프로젝트 루트에서 `Assets`, `Packages`, `ProjectSettings`, `.gitignore`를 GitHub에 올리면 됩니다.
`Library`, `Temp`, `Logs`, `UserSettings`는 `.gitignore`에 의해 제외됩니다.

## 직접 쓰는 방법

- 감지 대상 오브젝트에는 `CctvDetectionTarget`을 붙입니다.
- CCTV 오브젝트에는 `CctvDetector`를 붙입니다.
- `Obstacle Mask`에는 벽, 바닥 같은 장애물 레이어를 넣습니다.
- CCTV의 `forward` 방향이 감지 방향입니다.
- 회전 CCTV가 필요하면 `CctvPatrol`을 같이 붙입니다.
