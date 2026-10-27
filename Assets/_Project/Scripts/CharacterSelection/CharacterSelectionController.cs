using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.CharacterSelection
{
    [DisallowMultipleComponent]
    public sealed class CharacterSelectionController : MonoBehaviour
    {
        [SerializeField] private CharacterSelectionCard[] cards;
        [SerializeField] private TMP_Text classNameText;
        [SerializeField] private TMP_Text roleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private string gameplaySceneName = "MovementPrototype";
        [SerializeField] private string mainMenuSceneName = "MainScreen";

        private void Start()
        {
            ArrangeLayout();
            foreach (CharacterSelectionCard card in cards)
                if (card != null)
                    card.Initialize(this);

            if (cards.Length > 0 && cards[0] != null)
                ShowDetails(cards[0].Definition);
        }

        private void ArrangeLayout()
        {
            // Leave the authored card placement and artwork unchanged.
            var details = classNameText.transform.parent as RectTransform;
            if (details == null) return;
            Place(details, new Vector2(0f, -435f), new Vector2(1760f, 170f));
            Place(classNameText.rectTransform, new Vector2(-710f, 30f), new Vector2(250f, 58f));
            Place(roleText.rectTransform, new Vector2(-710f, -25f), new Vector2(250f, 38f));
            Place(descriptionText.rectTransform, new Vector2(-230f, 5f), new Vector2(600f, 120f));
            Place(statsText.rectTransform, new Vector2(465f, 26f), new Vector2(700f, 54f));
            Place(statusText.rectTransform, new Vector2(465f, -40f), new Vector2(700f, 50f));
            classNameText.alignment = roleText.alignment = TextAlignmentOptions.Center;
            classNameText.margin = roleText.margin = Vector4.zero;
            Fit(descriptionText, 24f, 21f);
            Fit(statsText, 20f, 18f);
            Fit(statusText, 18f, 15f);
        }
        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.localScale = Vector3.one; rect.anchoredPosition = position; rect.sizeDelta = size;
        }
        private static void Fit(TMP_Text text, float maximum, float minimum)
        {
            text.enableAutoSizing = true; text.fontSizeMax = maximum; text.fontSizeMin = minimum;
            text.margin = Vector4.zero;
        }

        public void ShowDetails(CharacterClassDefinition definition)
        {
            if (definition == null)
                return;

            classNameText.text = definition.DisplayName;
            roleText.text = definition.CombatRole;
            descriptionText.text = definition.Description;
            statsText.text =
                $"공격력  {BuildStat(definition.Attack)}    " +
                $"방어력  {BuildStat(definition.Defense)}    " +
                $"기동성  {BuildStat(definition.Mobility)}    " +
                $"사거리  {BuildStat(definition.Range)}";

            bool created = CharacterSelectionState.HasCharacter(definition.ClassId);
            statusText.text = definition.Playable
                ? created ? "사용 가능 · 생성된 캐릭터 · 선택하면 바로 입장" : "사용 가능 · 새 캐릭터 · 선택하면 바로 생성 및 입장"
                : "준비 중 · 캐릭터 전투 프리팹이 아직 구현되지 않았습니다";
        }

        public void SelectClass(CharacterClassDefinition definition)
        {
            if (definition == null || PortalSceneTransition.IsTransitioning)
                return;

            ShowDetails(definition);
            if (!definition.Playable)
                return;

            EasternFantasy.Saving.SaveGameService.PrepareSelection();
            CharacterSelectionState.CreateOrSelect(definition.ClassId);
            Time.timeScale = 1f;
            PortalSceneTransition.TryLoad(gameplaySceneName, null);
        }

        public void ReturnToMainMenu()
        {
            if (PortalSceneTransition.IsTransitioning) return;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private static string BuildStat(int value)
        {
            value = Mathf.Clamp(value, 1, 5);
            return new string('●', value) + new string('○', 5 - value);
        }
    }
}
