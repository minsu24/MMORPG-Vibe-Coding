# 영혼 조각

- ItemDefinition: Assets/_Project/Data/Items/SoulFragment.asset
- 이미지: Assets/_Project/Art/Item/SoulFragment.png (제공 원본 복사, 스프라이트 영역만 투명 여백에 맞춰 분리)
- 분류: 기타 / 일반 등급 / 최대 중첩 99 / 승급 재료.
- 드롭 설정: Assets/_Project/Resources/CommonMonsterLoot.asset
- 모든 EnemyController의 사망 처리에서 독립적으로 50% 판정하여 1개 생성. 기존 몬스터별 드롭은 유지.
- 현재 멧돼지, 거인, 두꺼비, 유령, 도깨비, 거미가 모두 공통 EnemyController를 사용한다. 이후 이 클래스를 사용하는 몬스터도 동일하게 적용된다.
- 동일 몬스터는 isDefeated 검사로 사망 처리가 한 번만 실행된다.
- 바닥에 떨어지는 기존 WorldItemPickup을 사용하며, 착지 위치를 찾지 못한 경우 사망 위치에 생성한다.
- 승급 Recipe의 Materials.Item에 SoulFragment를 지정하여 사용한다. 단계별 수량, 비용, Configured 값은 변경하지 않았다.
- 메뉴: Eastern Fantasy > Inventory > Setup Soul Fragment.

검증: Unity 컴파일 오류 없음. 기타 분류, 중앙 스프라이트/일반 프레임, 99개 중첩, Resources 공통 드롭 연결, 50% 경계 판정, 승급 Recipe 재료 등록, 기존 등급·세트 정렬 검증 통과.