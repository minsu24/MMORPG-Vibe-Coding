# 귀혼석 스프라이트 재슬라이싱

- 대상: 서막 마을/절 문 귀혼석의 활성화 및 반복 시트 4개, 총 32프레임.
- 원본 PNG를 수정하지 않고 Sprite Editor의 사각형/피벗 메타데이터를 수정했습니다.
- 각 받침돌의 불투명한 바닥 영역으로 프레임 경계를 찾고, 인접 사각형 사이에 2픽셀 여백을 두었습니다.
- 서막 마을과 절 문 각각 첫 비활성 프레임의 원래 바닥 위치를 기준으로, 활성화/반복 전체 프레임의 바닥 중심을 같은 월드 위치에 맞췄습니다.
- 원본 Sprite ID, 이름, AnimationClip 연결, 시트별 PPU를 유지했습니다. 게임 오브젝트 위치와 스케일은 수정하지 않았습니다.
- 반복 시트의 2048 자동 축소를 해제하고 원본 해상도/무압축/밉맵 없음으로 가져오도록 했습니다.
- Village-Before.png / Village-After.png / Temple-Before.png / Temple-After.png: 각 시트의 모든 프레임 비교. 위 행은 활성화, 아래 행은 반복, 초록 선은 고정 바닥입니다.
- alignment.json: 변경한 사각형, 피벗, 바닥 기준점. Validation.txt: 프레임 겹침·참조 보존·고정 기준점 검증.
- Unity 메뉴: Eastern Fantasy > Soulstone > Align Sprite Frames. 원본 프레임 이름과 ID를 유지하며 다시 정렬합니다.
