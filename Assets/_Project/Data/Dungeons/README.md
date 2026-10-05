# 던전 선택 시스템

GapInSpace의 Dungeon 포탈 가까이에서 기존 상호작용 키 T를 누르면 열립니다.
왼쪽은 선택한 던전 이미지와 정보, 오른쪽은 스크롤 목록과 입장 버튼입니다.
ESC 또는 우측 상단 ×로 닫습니다. 창을 열면 기존 GameTimeController로 일시 정지합니다.

## 던전 데이터
Assets/_Project/Data/Dungeons/GiantGarden.asset에서 이미지, 설명, 입장 레벨,
제한시간(초), 몬스터 수, 클리어 보상 아이템/수량을 편집합니다.
제한시간/몬스터 수의 0과 빈 보상 목록은 미설정으로 표시됩니다.
보상은 현재 안내용 데이터이며 실제 클리어 판정이나 보상 지급은 이 UI에서 수행하지 않습니다.

새 던전은 Create > Eastern Fantasy > Dungeon > Definition으로 생성한 후
Assets/_Project/Prefabs/UI/DungeonWindow.prefab의 Dungeons 배열에 추가합니다.
Scene Name과 Spawn Point Name을 지정하고 대상 씬을 Build Settings에 활성화합니다.
선택 시 이미지/설명/레벨/시간/몬스터/보상이 즉시 갱신됩니다.
잠긴 던전도 정보를 볼 수 있으며 입장 버튼은 비활성화됩니다.

## 씬 연결
거인의 농장 -> GiantGarden -> DungeonSpawn.
GapInSpace와 GiantGarden은 빌드 씬 목록에 등록됩니다.
기존 PlayerLocationSetter/MapTransferData로 플레이어와 카메라 위치를 이동합니다.
GiantGarden은 아직 게임플레이 맵이 없는 씬이므로 지형/몬스터/클리어 판정은 별도로 구성합니다.

## 재설치
GapInSpace를 연 후 Eastern Fantasy > Setup Dungeon System 메뉴를 실행합니다.
제공 PNG는 Art/UI/Dungeon에 원본으로 보관하고 TextureImporter로 필요한 영역을 분할합니다.
기존 GiantGarden.asset 데이터 값은 재설치 시 보존합니다.
설치는 GapInSpace의 현재 편집 내용도 함께 저장합니다.

## 확인
Unity 컴파일 오류 없음.
포탈 근접 상호작용, 선택 정보 갱신, 임시 12개 던전의 스크롤, 닫기와 재개 검증.
GiantGarden 씬 로드, DungeonSpawn 이동 데이터 소비, 플레이어 유지 검증.
