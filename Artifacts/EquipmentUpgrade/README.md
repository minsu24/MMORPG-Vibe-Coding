# 장비 승급

PrologueVilage의 대장장이 가까이에서 T로 상호작용한다. 인사 대사를 끝까지 진행하면 승급 창이 열린다. 대화가 취소되면 창은 열리지 않는다. ESC 또는 ×로 닫는다.

제공 이미지 사용:
- Art/UI/EquipmentUpgrade/UpgradeControls.png: 대상 슬롯, 재료 슬롯 상태, 비용 바, 확인 버튼.
- Art/UI/EquipmentUpgrade/UpgradeWindow.png: 메인 창 프레임.
- 원본 PNG를 복사한 후 Unity 스프라이트 영역을 분리했다. 픽셀 이미지를 다시 생성하지 않았다.

보유 장비/재료 선택창에서 클릭하거나 대상·재료 슬롯으로 끌어 넣는다. 등록된 슬롯을 클릭하면 취소된다. 등록 단계에서는 아이템을 소비하지 않는다. 현재 승급에 필요한 재료만 재료 탭에 표시한다.
재료 슬롯: 회색(빈 칸), 금색(등록했지만 수량 부족), 금색 빛(수량 충족). 희미한 아이콘은 필요한 재료 미리보기다.

확인 버튼은 모든 재료가 등록되고 수량과 엽전이 충분할 때 활성화된다. 부족하면 Button.interactable=false이며 별도 포인터 핸들러가 클릭 이유를 안내한다. 엽전 부족 시 '엽전이 부족합니다.'를 표시한다.
실행 전 조건을 재검사하여 재료·엽전·승급을 하나의 처리로 반영하며, 변경 이벤트는 전체 반영 후 전송한다. 장착 중인 아이템의 장착 상태와 세트 소속을 유지한다. 신화 장비는 추가 승급하지 않는다.

## 나중에 조건 입력하기

Assets/_Project/Data/Items/UpgradeRecipes 폴더에 8개 Recipe 에셋이 있다.
- Weapon: 무기, 4개 등급 단계.
- Armor: 모자/상의/하의/장갑/신발, 4개 등급 단계.
- Common=일반, Rare=희귀, Heroic=영웅, Legendary=설화, Mythic=신화.

각 에셋을 선택해:
1. Materials에 ItemDefinition과 Quantity를 입력한다.
2. Cost Yeopjeon에 엽전 비용을 입력한다.
3. Configured를 체크한다.

현재 모든 Recipe는 미설정 상태다. 임의의 재료·비용을 실제 데이터에 넣지 않았다. Inspector에서 설정을 마치면 게임에 적용된다.
최대 4종 재료. 중복 재료 행은 수량을 합산한다. 착용 가능한 장비는 재료로 사용할 수 없으며, Miscellaneous/Consumable 또는 EquipmentSlot.None인 재료 아이템을 사용한다.
장비별 다음 등급은 ItemDefinition.GetGradeVariant에서 가져온다.

프리팹: Assets/_Project/Prefabs/UI/EquipmentUpgradeWindow.prefab.
EquipmentUpgradeWindowUI의 On Confirm Clicked 이벤트: 활성/비활성 확인 클릭 모두 호출.
On Upgrade Succeeded: 성공했을 때 새 등급 ItemDefinition을 전달. 코드에서는 Upgraded 이벤트 사용 가능.

대화 에셋: Data/Tutorial_DIalogue/BlacksmithUpgradeDialogue.asset.
대장장이 캐릭터: Data/Tutorial_DIalogue/BlacksmithDialogueCharacter.asset.
대화 문구 및 초상화는 에셋에서 교체할 수 있다.

Unity 메뉴:
- Eastern Fantasy > Inventory > Setup Equipment Upgrade UI: 아트 분리, 프리팹 생성, 마을 연결. 기존 Recipe와 대화 데이터는 유지한다.
- Eastern Fantasy > Inventory > Validate Equipment Upgrade: 비파괴 편집 모드 검증.

preview.png의 재료와 금액은 렌더링 검증용 임시 데이터다. 실제 Recipe 에셋에는 적용하지 않았다.
장비/재료 탭은 선택 상태에서 밝은 금색 이미지, 미선택 상태에서 파란 이미지로 표시한다. 재료 여부는 별도 ItemDefinition 체크박스가 아닌 Recipe의 Materials 참조로 판정한다.