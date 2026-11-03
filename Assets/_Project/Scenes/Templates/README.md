# 새 맵 만들기

`Map_Template.unity`는 BrokenSeal의 저장된 씬을 복제한 뒤 지역 전용 오브젝트를 제거한 기본 맵입니다. `Map_Template.scenetemplate`는 Unity의 New Scene 창에서 이 씬을 선택할 수 있게 하는 에디터용 자산입니다.

## 권장 방법

1. Unity에서 **File > New Scene** (`Ctrl+N`)을 엽니다.
2. **Map Template**을 선택하고 **Create**를 누릅니다.
3. **File > Save As** (`Ctrl+Shift+S`)로 `Assets/_Project/Scenes/원하는지역/원하는맵이름.unity`에 저장합니다. 예: `OldTemple/AbandonedTemple_01.unity`.
4. `MapContent` 아래에 배경, 발판, 몬스터, NPC, 포탈, 지역 BGM을 배치합니다.
5. 맵 크기에 맞춰 `Floor`, `Wall`, `Wall (1)`의 Box Collider 2D를 조정합니다. 눈에 보이는 바닥 이미지는 `MapContent/Background` 등에 별도로 넣습니다.
6. `CameraCollider`의 Box Collider 2D 크기와 위치를 배경에 맞춥니다. **Is Trigger는 켜둡니다.** `CinemachineCamera > Cinemachine Confiner 2D > Bounding Shape 2D`는 같은 씬의 `CameraCollider`에 연결되어 있습니다.
7. 맵을 게임에서 사용하려면 **File > Build Profiles > Scene List**에 새 씬을 추가합니다. Unity 버전에 따라 **Build Settings > Scenes In Build**로 표시됩니다. 원본 템플릿 씬은 등록하지 않습니다.
8. 입출구 포탈을 배치하고 `PortalManager`의 **Scene Name**에는 목적지 씬 이름, **Target Point Name**에는 목적지 씬에서 도착할 포탈/스폰 오브젝트의 정확한 이름을 입력합니다. 같은 씬 안에서 도착 지점 이름이 중복되지 않게 합니다. 반대쪽 맵에서도 돌아오는 포탈을 연결합니다.

## Project 창에서 복제하는 방법

`Map_Template.unity`를 선택하고 `Ctrl+D`로 복제한 뒤 **먼저 새 이름으로 변경하고**, 원하는 지역 폴더로 옮겨 더블클릭합니다. 이후 과정은 위의 4번부터 동일합니다. `.scenetemplate` 파일을 복제할 필요는 없습니다.

## 포함된 공통 오브젝트

- `Main Camera`: Camera, Audio Listener, URP 카메라 설정, Cinemachine Brain
- `CinemachineCamera`: 카메라 추적, Position Composer, Confiner 2D, CameraFollowDetail
- `CameraCollider`: 카메라 이동 경계용 트리거
- `Floor`, `Wall`, `Wall (1)`: 실제 충돌용 바닥과 좌우 벽
- `EventSystem`: Input System UI 입력
- `MapContent`: Background / Platforms / Monsters / NPCs / Portals / Audio 빈 정리 폴더

지역 배경과 발판 그림, NPC·대화 트리거, 기존 포탈·봉인 연출·목적지, 지역 BGM은 제거했습니다. BrokenSeal 전용 비 효과는 씬 이름이 BrokenSeal일 때만 실행되므로 새 맵에 복제되지 않습니다.

## 플레이 확인

이 템플릿에는 플레이어를 미리 넣지 않았습니다. 기존 맵처럼 캐릭터 선택 후 포탈을 통해 진입하는 구성을 기준으로 합니다. 실행 중 `PlayerLocationSetter`가 현재 캐릭터를 `CinemachineCamera`의 추적 대상으로 연결하므로 템플릿의 Tracking Target은 비워두었습니다. 템플릿만 열고 Play를 누르면 플레이어가 나타나지 않습니다.

새 맵 이름을 미니맵·지역 이름·던전 목록 등에 표시하려면 해당 시스템의 지역 등록도 별도로 추가해야 합니다. 템플릿 복제는 지형과 공통 씬 구성만 준비합니다.

원본 `Map_Template.unity`는 다음 맵에도 사용할 기본 상태로 유지하고, 지역 작업은 복제한 씬에서 진행합니다.
