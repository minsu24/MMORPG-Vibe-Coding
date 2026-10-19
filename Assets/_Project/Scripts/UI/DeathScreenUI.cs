using TMPro;
using UnityEngine;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class DeathScreenUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private GameObject firstDeathGuide;
        [SerializeField] private TMP_Text destination;
        [SerializeField] private RectTransform respawnButton;

        public void Configure(bool isFirstDeath, bool isTutorialScene)
        {
            if (title != null)
                title.text = "가사상태";
            if (firstDeathGuide != null)
                firstDeathGuide.SetActive(isFirstDeath);
            if (respawnButton != null)
                respawnButton.anchoredPosition = new Vector2(0f, isFirstDeath ? -154f : -24f);
            if (destination != null)
            {
                destination.rectTransform.anchoredPosition = new Vector2(0f, isFirstDeath ? -225f : -150f);
                destination.text = isTutorialScene
                    ? "이 수련장에서는 시작 지점에서 부활합니다."
                    : "부활 버튼을 누르면 가까운 마을에서 부활합니다.";
            }
        }
    }
}
