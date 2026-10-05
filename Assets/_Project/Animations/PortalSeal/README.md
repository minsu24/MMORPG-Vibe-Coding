# BrokenSeal 포탈 봉인

BrokenSeal의 Out_BrokenSeal_Portal에 LockedPortalSequence가 연결되어 있습니다.
씬에 들어올 때 잠긴 상태로 시작하고, GoldFrogDialogue.asset 대화를 끝까지 완료한 뒤에만 해제됩니다.
강제 종료(EndDialogue), 다른 NPC 대화, 해제 연출 진행 중에는 열리지 않습니다.

PortalManager의 Starts Locked는 최초 상태, IsLocked는 현재 상태입니다.
잠겨 있거나 게임이 일시 정지된 동안에는 ↑ 이동을 차단합니다.
기존 포탈의 목적지와 트리거 콜라이더는 유지합니다.

Art/자물쇠 애니메이션22.png의 6프레임을 윗줄 왼쪽부터 아랫줄 순서로 8fps로 반복 재생하여 봉인을 표시합니다.
잠금 표시의 첫 프레임은 기존 표시와 같은 가로·세로 크기로 맞추며, 이후 프레임의 자연스러운 움직임은 유지합니다.
Art/사슬 봉인 해제와 붕괴 효과 8프레임.png의 0~7 스프라이트를 순서대로 재생합니다.
PortalLocked.anim과 PortalUnlock.anim은 PortalSeal.controller에서 사용합니다.
Unlock은 6fps로 재생하고 마지막 잔광을 페이드 아웃합니다.

잠금 해제 연출:
카메라 줌인(0.9초) → 대기(0.25초) → 해제 애니메이션(1.65초) →
짧은 대기 → 원래 시점 복귀(0.75초) → 잠금 해제.
애니메이션과 카메라는 unscaled time으로 진행하며 플레이어 입력을 잠시 비활성화합니다.
배경 범위를 사용해 카메라가 맵 밖을 보여주지 않게 보정합니다.
정상 완료 또는 연출 중단 시 카메라, Cinemachine Brain, 입력 활성 상태와 일시 정지를 복구합니다.

LockedPortalSequence Inspector에서 줌 크기/이동 시간/대기 시간을 조절할 수 있습니다.
씬이 다시 로드되면 잠금과 금두꺼비 대화 트리거가 초기화됩니다.
재설치 메뉴: Eastern Fantasy > Setup Broken Seal Portal (BrokenSeal을 연 상태에서 실행).
