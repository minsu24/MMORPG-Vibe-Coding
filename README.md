# 동양 판타지 2D RPG 프로토타입

## 실행

Unity 6000.3.23f1에서 `Assets/_Project/Scenes/MovementPrototype.unity`를 열고 Play를 누릅니다. Game 뷰를 클릭해 키보드 입력을 받도록 합니다.

- 이동: A / D 또는 좌우 방향키
- 점프: Space
- 공중 이동 가능, 공중 추가 점프 불가
- Space를 계속 누르고 있어도 착지 후 자동으로 점프하지 않음

현재 캐릭터는 이동·충돌 확인을 위한 임시 도형입니다. 바닥과 세 개의 발판, 좌우 경계벽이 있습니다.

## 프로젝트 기준

- URP 17.3.0 / Renderer2D 사용
- Input System 1.20.0, 기존 `Assets/Settings/InputSystem_Actions.inputactions`의 Player/Move 및 Player/Jump 재사용
- 기준 해상도 1920 × 1080
- 맵은 Sprite와 Collider2D로 구성
- 기존 SampleScene 및 Settings 경로 유지

## 주요 파일

- `Assets/_Project/Scenes/MovementPrototype.unity`: 이동 테스트 씬
- `Assets/_Project/Prefabs/Player/Player.prefab`: 플레이어 프리팹
- `Assets/_Project/Data/Player/DefaultMovement.asset`: 이동 속도 6, 점프 속도 11, 중력 배율 3
- `Assets/_Project/Scripts/Player/PlayerInputReader.cs`: 입력 처리
- `Assets/_Project/Scripts/Player/PlayerMovement2D.cs`: 이동, 바닥 감지, 점프
- `Assets/_Project/Scripts/Player/PlayerMovementSettings.cs`: 이동 설정 데이터
- `Assets/_Project/Editor/MovementPrototypeSetup.cs`: 테스트 씬 최초 생성용 도구. 기존 씬이 있으면 덮어쓰지 않음

## 검증

Unity Play Mode에서 이동 컴포넌트에 입력값을 전달하고 0.02초 간격의 Physics2D 시뮬레이션으로 확인했습니다.

- 0.4초 동안 좌우 각각 약 2.4 유닛 이동
- 입력 해제 시 수평 속도 0
- 점프 및 공중 수평 이동
- 공중 추가 점프 거부
- 착지 후 재점프
- 화면 캡처에서 플레이어와 바닥·발판 표시 확인

에디터 일시정지 상태에서 자동 키보드 이벤트 전달은 확인되지 않아, 위 수치 검증은 입력 컴포넌트 이후의 이동·물리 경로를 대상으로 했습니다. 실제 조작 감각은 Game 뷰에서 확인합니다.

## Git

프로젝트 루트에 독립된 로컬 저장소를 초기화했습니다. Unity 생성 폴더는 `.gitignore`로 제외합니다. 초기 커밋과 GitHub 원격 저장소 연결은 아직 하지 않았습니다.
