# 귀혼석 UI 편집

- 편집할 프리팹: `Assets/_Project/Prefabs/UI/SoulstoneWindow.prefab`
- Unity 메뉴 `Eastern Fantasy > Soulstone > Open Editable UI Prefab`으로 열 수 있습니다.
- `Modal/Window` 아래 Map Image, Map Name, NPC Panel, Travel, Map List를 선택해 RectTransform과 Image/TMP를 수정합니다.
- 프리팹에는 서막 마을 사진, NPC 3명, 두 지역 목록이 실제 UI 오브젝트로 들어 있습니다. 프리팹에서 저장한 디자인을 게임에서도 그대로 불러옵니다.
- 지역 데이터: `Assets/_Project/Resources/SoulstoneCatalog.asset`. 선택한 지역의 사진·이름·NPC와 버튼 상태는 실행 중 데이터에 맞춰 표시됩니다.
- 프리팹의 SoulstoneWindowView에 연결된 참조를 유지하세요. NPC/지역이 더 늘어나면 기존 행을 복제해 표시합니다.
- `Build Assets and Verify`는 기존 프리팹의 배치를 덮어쓰지 않습니다. 카탈로그·맵 사진을 갱신하고 저장된 프리팹을 렌더링합니다.
- 기본 조작: 귀혼석 근처 G로 열기, G/ESC 또는 닫기 버튼으로 닫기. 현재 지역 이동은 차단합니다.
- 검증: 두 지역 이미지와 글자 넘침 확인, 컴파일 성공, 프리팹 전환 후 G/ESC·입력 잠금·현재 지역 제한·왕복 이동 모두 통과했습니다.
- `Validation-report.txt`는 실제 플레이 검증 결과이며, 검증 저장은 `ValidationSaves`로 분리했습니다.

- 맵 사진은 프레임 안에 채워지며, 모서리 장식의 검은 사각 배경은 UV 메시에서 제외합니다. 원본 PNG는 유지합니다.
- 이동 버튼은 NPC Panel과 같은 스프라이트를 사용하고, 호버 시 밝아지며 조금 확대됩니다. 지역 칸은 투명 여백을 제외한 스프라이트로 목록 안쪽 너비를 채웁니다.
- 카탈로그의 Window Prefab 참조로 이동된 프리팹을 불러옵니다. 새 프리팹의 기본 생성 경로는 Assets/_Project/Prefab/UI입니다.


## 귀혼석 튜토리얼

- 새 캐릭터의 서막 마을 귀혼석은 RespornStone 0프레임에서 멈추며 이동할 수 없습니다.
- 절 문 귀혼석의 상호작용 거리(3.5)에 들어가면 설명 → G 활성화 → 맵 선택 → 이동 클릭 순서로 안내합니다.
- 제공된 keyboard_g.png를 상단에 띄우며 실제 G 입력을 기다립니다. 조작 단계는 다음/Space로 건너뛸 수 없습니다.
- G 입력: InActiveTempleRespornStone 1회 → TempleRespornStone 반복. 연출 후 귀혼석 창이 열립니다.
- 서막 마을 첫 귀환: RespornStone 1회 → ActiveRespornStone 반복. 이후 방문은 활성 반복 상태로 시작합니다.
- 진행 상태는 캐릭터별 저장 데이터의 soulstoneActivated, soulstoneTutorial, soulstoneVillageAwakened에 보관합니다. 이전 저장은 기본 미활성 상태입니다.
- Tutorial-validation.txt: 첫 프레임 유지, 실제 G 입력, 단계 건너뛰기 차단, 클릭 진행, 두 지역 애니메이션 전환, 입력/일시정지 해제, 저장·복원·왕복 이동 검증 결과.
- Tutorial-G.png, Tutorial-MapSelection.png, Tutorial-Travel.png: 플레이 중 촬영한 안내 화면.

## 활성화 클로즈업과 전체 창 안내

- 첫 G 입력 후 안내 카드/HUD/플레이어 그림을 잠시 숨기고, 카메라를 귀혼석으로 확대합니다.
- 확대가 끝난 뒤 활성화 애니메이션을 시작해 처음부터 끝까지 보여 줍니다. 활성 반복 상태를 잠깐 보여 준 후 원래 카메라로 돌아옵니다.
- 이후 귀혼석 창을 원래 크기로 열고, 펫 안내와 같은 방식으로 창 위에서 서막 마을 선택 칸과 이동 버튼을 강조합니다.
- 연출 취소/씬 변경/저장 복원 시 카메라, Cinemachine, 숨긴 HUD/플레이어 표시를 복구합니다.
- Tutorial-ActivationCloseup.png는 실제 플레이 중 촬영한 활성화 클로즈업입니다.

## 귀혼석 머리 위 G키 표시

- 새 프리팹: Assets/_Project/Prefab/UI/SoulstoneInteractionPrompt.prefab
- 기존 NPC InteractionPrompt.prefab의 Variant로, 판/글꼴/크기/월드 공간 Canvas/그리기 순서를 그대로 상속하고 글자만 G로 바꿉니다.
- NPC와 같은 상단 + 0.35 간격으로 표시하며, 대상별 높이를 한 번 정한 뒤 위치를 따라가므로 스프라이트 애니메이션에 흔들리지 않습니다.
- 상호작용 거리 안에서 표시하고, 이동 창/연출/다른 조작 창에서는 숨깁니다. 튜토리얼의 G 입력 대기 단계에서는 머리 위 표시도 함께 띄웁니다.
- 이전 화면 좌표 기반 [G] 귀혼석 글자 안내는 제거했습니다. 상단 튜토리얼 키 이미지 안내는 별도로 유지합니다.
- 귀혼석의 투명한 상단 여백은 높이 계산에서 제외하고, 비활성 프레임의 실제 지붕 위를 기준으로 고정합니다. 카탈로그의 villagePromptTop/templePromptTop에서 기준 높이를 조정할 수 있습니다.
