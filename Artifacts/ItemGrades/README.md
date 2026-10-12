# 아이템 등급

순서: 신화(Mythic) > 설화(Legendary) > 영웅(Heroic) > 희귀(Rare) > 일반(Common).
프레임: 주황 > 노란 > 보라 > 파란 > 회색. 포션 등 Consumable은 등급 없음(None).

- 기존 아이템 자산과 초기 지급/상점/드롭 참조는 일반 등급으로 유지.
- 장비 6종의 상위 등급 자산: Assets/_Project/Data/Items/Grades.
- 분리된 아이콘과 프레임: Assets/_Project/Art/Item/Grades. Common/Frames는 제공 원본을 복사해 스프라이트로 분리. 상위 4종은 built-in imagegen으로 경계와 여백을 보정한 별도 시트.
- ItemDefinition의 Grade와 Grade Appearance가 이미지와 프레임을 함께 선택. 각 Appearance의 Icons/Frames 배열 순서는 일반, 희귀, 영웅, 설화, 신화.
- 런타임 등급 변경: inventory.TryChangeGrade(item, ItemGrade.Mythic). 소유 아이템 1개를 해당 등급 자산으로 교체하고 장착 상태와 슬롯을 유지. 공유 ScriptableObject를 직접 변경하지 않음.
- 인벤토리, 장착 슬롯, 드래그, 상점, 던전 보상 아이콘에 프레임 적용. 툴팁과 상점 이름에 등급 표시.
- 드롭/보상/상점에는 원하는 등급 자산을 지정. 등급별 능력치와 가격은 각 자산에서 설정. 자동 수치 배율과 강화 비용/확률은 이번 범위에 포함하지 않음.

Unity 메뉴: Eastern Fantasy > Inventory > Preview Item Grades.
Setup Item Grades는 이미지 재분리 및 자산 연결용. 기존 아이템의 능력치와 가격을 유지함.
검증: ItemGradePreview.Validate() — 30종 연결, 포션 제외, 등급 값에 따른 이미지/프레임 동시 선택, 고유 ID, 능력치 보존, 장착 중 상승/하향 변경, 공유 자산 격리.

Imagegen 최종 프롬프트(4개 등급에 각각 동일 적용):
Use case: precise-object-edit. Edit target: attached Korean RPG equipment sprite sheet. Preserve all six item designs, colors, patterns, ornamentation and painterly style exactly. Correct ONLY spacing, edges and overlap contamination. Output a transparent 1536x1024 landscape sprite atlas in exactly 3 equal columns and 2 equal rows. Top row: folding fan, gat hat, jacket. Bottom row: pants, gloves pair, shoes pair. Center each complete item inside its own cell, scale to fit within 80% cell width and height, with fully transparent gutters on all sides. All ribbons, tassels, and magical wisps must belong only to their own item and remain completely within their own cell. Repair overlapping edges, remove stray fragments from adjacent objects, keep silhouette complete and unclipped. No frames, no grid lines, no labels, no potions, no checkerboard baked into pixels. Genuine alpha transparency.

실제 출력에서 갓 끈이 등간격 경계를 넘어서므로 ItemGradeSetup은 등급별·열별로 다른 분리 경계를 사용함.

## 아이콘 중앙 정렬과 세트 효과

현재 모든 ItemDefinition의 아이콘 36개와 프레임 5개의 스프라이트 영역을 실제 불투명 픽셀 경계에 맞추고 Pivot을 중앙으로 설정했다. 희미한 효과를 포함한 모든 alpha > 0 픽셀을 보존하며 기존 sprite ID와 아이템 참조를 유지한다. 원본 PNG는 변경하지 않는다. 프레임도 아이콘 RectTransform의 중앙에 맞춘다.

Unity 메뉴 Eastern Fantasy > Inventory > Center All Item Icons에서 다시 적용할 수 있다. 현재 아이템은 모두 Multiple Sprite로 가져온 이미지다. Setup Item Grades도 재분리 후 중앙 정렬을 적용한다.
centered-preview.png는 신화, 설화, 영웅, 희귀, 일반 순서의 정렬 미리보기다.

수련자 세트: Assets/_Project/Data/Items/TrainingSet.asset.
- 부채, 갓, 상의, 하의, 장갑, 신발의 6종 전체 착용 시 이동속도 +10%.
- 일반부터 신화까지 동일한 세트 참조를 사용하므로 등급 혼합 착용도 인정.
- 소지 개수가 아닌 장착 슬롯 개수를 센다. 교체, 해제, 판매/제거, 장착 중 등급 변경 때 즉시 다시 계산.
- 툴팁에서 장착 개수, 각 효과와 활성 여부 표시. 길어진 효과 문구에 맞춰 영역 확장.
- Bonuses의 Required Pieces와 Effects에서 효과 설정. 달성한 여러 단계는 누적 적용.
- Stat: MoveSpeed / Attack / Defense / MaximumHealth / MaximumMana.
- Modifier: Flat(고정 수치) / Percent(퍼센트). Value 10은 Percent일 때 +10%.
- 동일 능력치의 고정 보너스와 퍼센트를 각각 합산한 후 (기본 합계 + 고정 보너스) × (1 + 퍼센트 합계 / 100)로 적용.
- Eligible Slots가 비어 있으면 해당 세트의 모든 장착 슬롯 인정. 방어구 전용 등은 슬롯 목록으로 제한 가능.
- 새 세트는 Create > Eastern Fantasy > Inventory > Equipment Set에서 생성 후 ItemDefinition의 Equipment Set에 연결.
- 새 종류의 능력치를 추가할 때 EquipmentSetStat와 PlayerEntity의 해당 능력치 계산에 연결. 현재 지원하는 5종은 코드 변경 없이 데이터로 추가 가능.
- Setup Training Set은 기존 세트 효과 설정을 덮어쓰지 않고 장비 참조만 다시 연결한다.

검증 메뉴: Eastern Fantasy > Inventory > Validate Item Alignment and Sets.
검증 결과: 41 스프라이트 중앙 정렬, 혼합 등급 6세트, 반복 장착 중복 방지, 등급 변경, 해제/제거, 누적 고정/비율 효과, 공격/방어/최대 체력/마나, 실제 인벤토리 프리팹 툴팁 정상. Unity 스크립트 컴파일 오류 없음.