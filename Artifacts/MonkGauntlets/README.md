# 권갑과 직업별 장착 제한

- 일반 권갑: Assets/_Project/Data/Items/MonkGauntlets.asset
- 상위 등급: Assets/_Project/Data/Items/Grades/MonkGauntlets_{Rare,Heroic,Legendary,Mythic}.asset
- 등급 이미지 연결: Assets/_Project/Data/Items/Grades/MonkGauntletsAppearance.asset
- 이미지: Assets/_Project/Art/Item/MonkGauntlets/
- 미리보기: preview.png (왼쪽부터 일반, 희귀, 영웅, 설화, 신화)

ItemDefinition Inspector의 Allowed Classes 목록으로 장착 가능한 직업을 지정합니다.
빈 목록은 모든 직업 허용입니다. 여러 직업을 등록할 수 있습니다.
부채 5등급은 Dosa, 권갑 5등급은 Monk로 지정했습니다.
검을 만들 때 Swordswoman을 등록하고, 검의 각 등급 ItemDefinition에도 동일하게 지정하세요.
직업은 인벤토리 소유 캐릭터의 PlayerClassRuntime에서 확인합니다.
구매와 소지는 직업과 무관하며 장착만 제한합니다.

권갑은 양손 한 쌍을 무기 1개로 취급합니다. 방어구 장갑 슬롯과 별개입니다.
일반 권갑의 초기 능력치와 가격은 부채와 같습니다 (공격력 +3, 구매 120, 판매 60).
각 등급은 대응하는 부채 등급의 값을 복사했으며 Inspector에서 각각 조정할 수 있습니다.
등급 이미지와 프레임은 별도로 연결되고, 실제 그림의 투명 여백을 잘라 중심 피벗을 적용했습니다.

PrologueVilage 상점의 부채 바로 다음 칸에 일반 권갑을 추가했습니다.
승급 창은 동일한 Weapon 승급 레시피를 사용합니다.
Assets/_Project/Data/Items/UpgradeRecipes/Weapon_* 레시피의 재료와 비용을 편집하면 부채와 권갑 모두 적용됩니다.
기존에 입력된 레시피 값은 변경하지 않았습니다.
권갑은 TrainingSet의 무기 부분을 채우며, 권갑 + 모자/상의/하의/장갑/신발로 6종 이동속도 +10%가 적용됩니다.
등급이 바뀌어도 장착 슬롯과 세트 효과를 유지합니다.
인벤토리, 장비 창, 드래그 아이콘, 툴팁, 상점, 승급 창은 기존 공통 ItemDefinition 경로를 사용합니다.
툴팁과 상점에는 장착 가능한 직업도 표시합니다.

검증:
- Eastern Fantasy > Inventory > Validate Monk Gauntlets
- Validate Item Alignment and Sets (47개 고유 스프라이트 중심 검증)
- Validate Equipment Upgrade
- 기존 ItemGradePreview.Validate

모두 통과했습니다. 테스트 승급 비용은 임시 ScriptableObject에서만 설정했고 실제 레시피를 변경하지 않았습니다.