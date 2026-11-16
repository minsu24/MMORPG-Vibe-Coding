# 사운드 연결 점검 — OldTemple 제외

## 범위와 방법

- `Assets/_Project/Scenes/OldTemple/`의 모든 씬 제외.
- 메뉴·튜토리얼·GapInSpace·GiantFarm의 12개 씬, 세 직업 플레이어 프리팹, 공통 UI/Resources 프리팹의 AudioSource 및 AudioClip 참조 확인.
- 코드의 공통 사운드 로드 경로와 공격·사망·UI·진행 이벤트 연결 확인.
- 멧돼지·거인·동행 도깨비의 애니메이션 이벤트도 확인.
- 정적 연결 점검이며, 전체 플레이 구간을 직접 청취한 음향 품질 평가를 의미하지는 않음.
- 원시 점검 목록: `Bindings.tsv`. 게임 자산과 코드는 변경하지 않음.

## 전용 효과음 연결이 확인되지 않은 곳

| 우선순위 | 대상 | 확인 내용 |
|---|---|---|
| 높음 | GiantFarm_02 멧돼지 사망 | 돌진·공격음은 있으나 `MON_Boar.OnDefeated()`에 사망음 호출이 없고 BoarDead 클립의 이벤트도 비어 있음. |
| 높음 | GiantFarm_03_Boss 거인 사망 | 발걸음·돌진·내려찍기·투척음은 있으나 사망음 호출과 GiantDead 이벤트가 없음. 던전 클리어 음악은 별도로 구현되어 있음. |
| 높음 | 수도승 패링 발동 / 성공 | `MonkSkillCaster`는 패링 시 `BeginParry()`를 호출하고 `MonkEnergy.TryParry()`는 방어 판정만 처리. 전용 사운드 연결이 없음. |
| 높음 | 동행 도깨비 기본 공격 / 액티브 스킬 / 소환 | `GoblinCompanion.prefab`에는 PetCompanionActor만 있고 AudioSource가 없음. 관련 코드에 전용 오디오 호출이 없으며 대기·이동·공격·성장·스킬·축소 클립 이벤트도 비어 있음. 적 피격 시 공통 타격음이 발생할 수 있으므로 전투 전체가 무음이라는 뜻은 아님. |
| 중간 | 퀘스트 수락 / 완료 / 보상 확정 | 퀘스트와 대화 상태 처리는 있으나 해당 시점의 전용 효과음 연결이 확인되지 않음. 대화 타이핑음·레벨업음은 별도임. |
| 중간 | 스킬 습득 / 레벨 상승, 전직 완료 | LearnSkill 및 전직 완료 이벤트에 전용 효과음 연결이 없음. 여기서 스킬 레벨 상승은 캐릭터 레벨업과 구분함. |
| 중간 | 혼석 활성화 | 활성화 애니메이션과 카메라 연출은 있으나 활성화 전용 오디오 호출이 없음. 혼석으로 지역 이동할 때의 포탈음은 연결되어 있음. |
| 중간 | 일반 버튼 클릭 / 확정 / 취소 | 공통 UIHoverAudio는 포인터 진입음만 처리. 공통 클릭음은 확인되지 않음. 구매·판매·강화 결과음과 두루마리 열고 닫는 소리는 이미 별도 구현됨. |

## 연결이 확인된 주요 항목

- 점검한 12개 씬 모두 활성화된 BGM AudioSource에 클립이 지정되어 있고, 반복·시작 시 재생 설정이 켜져 있음.
- 세 직업의 기본 공격, 점프·발걸음·피격·사망·부활, 도사 스킬, 수도승 대시·기력파 차지·공격 적중, 여검사 스킬의 주요 사운드 참조 확인.
- 도깨비·귀신·거미·두꺼비의 주요 공격·사망음, 포탈 진입·도착·봉인 해제, 아이템·엽전·소모품·장비, 상점·강화·던전 결과·레벨업·UI 호버·두루마리·대화 타이핑 사운드 경로 확인.
- 정적 문자열로 지정한 Audio 리소스 중 누락된 파일 경로는 발견하지 못함. 숫자 접미사가 붙는 BoarStep0~3 등의 경로는 접두사 기준으로 구분함.

## 빈 필드와 볼륨 0의 해석

- `PlayerMovement2D.landingSound`: 비어 있어도 `Audio/PlayerLandingShort`를 자동 로드.
- `MON_Goblin.disappearanceSound`: `Audio/GoblinDeath`로 대체.
- `DialogueManager.typingSound`: `Audio/DialogueWritingTick`를 자동 로드.
- 플레이어 루트 AudioSource의 clip 없음: 기본 공격 등에서 PlayOneShot으로 재생하므로 누락으로 분류하지 않음.
- 튜토리얼 벌레 소리와 SpiderCave 물방울: IntermittentAmbientAudio가 간헐 재생하며 런타임 볼륨을 제어.
- 마을 폭포: LocalAmbientAudio2D가 플레이어 거리로 볼륨을 제어.
- BrokenSeal 천둥: 클립을 지정하지 않아도 코드가 원거리 천둥음을 생성.

## 선택적으로 추가할 환경음

Cemetery, Marshland, GapInSpace, GiantFarm 01~03에는 씬에 직접 연결된 별도 환경음 AudioSource가 확인되지 않았으며 BGM은 있음. BrokenSeal에는 런타임 천둥이 있으나 별도 빗소리 루프는 확인되지 않음. 이는 필수 연결 오류보다 환경 연출을 보강할 후보로 보는 것이 적절함.
