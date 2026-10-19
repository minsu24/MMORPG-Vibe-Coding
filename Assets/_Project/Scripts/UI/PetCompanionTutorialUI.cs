using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    // A read-only walkthrough: it never spends items or presses gameplay buttons.
    public sealed class PetCompanionTutorialUI : MonoBehaviour
    {
        private static readonly string[] Titles = {
            "동행요괴와 인연 맺기", "1. 전용 영혼으로 잠금 해제", "2. 동행요괴 소환", "3. 장착과 장착 해제", "4. 영혼 조각으로 성장", "5. 혼령 기술 등록" };
        private static readonly string[] Messages = {
            "동행요괴는 함께 싸우는 든든한 동료입니다.\n이 창에서 요괴를 소환하고, 장착하고, 성장시킬 수 있어요.\n강조되는 곳을 보며 하나씩 알아볼까요?",
            "해당 요괴의 전용 영혼을 가지고 있으면 목록의 잠금이 해제됩니다.\n도깨비는 ‘도깨비의 영혼’이 필요해요.\n잠금 해제된 요괴를 목록에서 선택하세요.",
            "‘소환’ 버튼을 누르면 전용 영혼 1개를 사용해 요괴와 인연을 맺습니다.\n처음 소환한 요괴는 자동으로 장착됩니다.\n이 안내에서는 재료를 사용하지 않으니 편하게 다음으로 넘어가세요.",
            "장착한 요괴는 창을 닫으면 플레이어 곁에 나타납니다.\n‘장착 해제’를 누르면 사라지고, 다시 ‘장착’하면 돌아와요.\n장착을 해제해도 소환 기록과 성장 레벨은 유지됩니다.",
            "요괴들이 공통으로 드랍하는 ‘영혼 조각’(SoulFragment)은 성장 재료입니다.\n소환한 뒤 이 버튼이 ‘성장’으로 바뀌어요. 성장하면 레벨과 공격력이 증가합니다.\n성장 비용은 현재 레벨에 따라 늘어나며, 아래 재료 칸에서 확인할 수 있어요.",
            "혼령 기술 아이콘을 드래그해 플레이어 스킬 퀵슬롯에 놓으세요.\n드래그하는 동안 퀵슬롯이 나타납니다. 등록한 단축키로 기술을 사용할 수 있어요.\n도깨비 기술은 도깨비를 장착하고 전투 중인 대상이 있을 때 사용할 수 있습니다." };

        private RectTransform canvasRect, card;
        private readonly RectTransform[] shades = new RectTransform[4];
        private readonly Image[] borders = new Image[4];
        private RectTransform[] targets;
        private TMP_Text title, body, nextLabel, counter, heading, controlsHint;
        private string[] activeTitles = Titles, activeMessages = Messages;
        private Action<int> stepChanged;
        private bool dockRight;
        private GameObject skipButton;
        private Button previousButton;
        private Sprite[] skin;
        private const float CardWidth = 900f, CardHeight = 480f;
        private static readonly Color Ink = new Color(.22f, .14f, .075f);
        private Sprite Art(string name) => Array.Find(skin, sprite => sprite.name == name);
        private Action finished;
        private int step;
        public bool IsShowing => gameObject.activeSelf;
        public int CurrentStep => step;
        public int StepCount => activeTitles.Length;

        public void Show(TMP_FontAsset font, RectTransform[] focusTargets, Action onFinished)
        {
            ShowCustom(font, "동행요괴 튜토리얼", Titles, Messages, focusTargets, null, onFinished, false);
        }
        public void ShowCustom(TMP_FontAsset font, string headingText, string[] titles, string[] messages,
            RectTransform[] focusTargets, Action<int> onStepChanged, Action onFinished, bool placeOnRight = true, bool allowSkip = true)
        {
            if (titles == null || messages == null || titles.Length == 0 || titles.Length != messages.Length)
                throw new ArgumentException("Tutorial titles and messages must have the same nonzero length.");
            activeTitles = titles; activeMessages = messages; stepChanged = onStepChanged; dockRight = placeOnRight;
            targets = focusTargets; finished = onFinished; step = 0;
            if (card == null) Build(font);
            skipButton.SetActive(allowSkip);
            Place((RectTransform)previousButton.transform, new Rect(10, 0, allowSkip ? 210 : 265, 78));
            Place((RectTransform)nextLabel.transform.parent, new Rect(allowSkip ? 240 : 315, 0, allowSkip ? 210 : 265, 78));
            heading.text = headingText;
            controlsHint.text = !allowSkip ? "Space 또는 다음 버튼으로 진행" : placeOnRight ? "Space로 다음 · ESC로 안내 닫기" : "Space로 다음 · ESC로 창 닫기";
            gameObject.SetActive(true);
            Display();
        }
        public void Hide()
        {
            finished = null;
            gameObject.SetActive(false);
        }
        public void Advance()
        {
            if (!IsShowing) return;
            if (++step >= activeTitles.Length) { Complete(); return; }
            Display();
        }
        public void Previous()
        {
            if (!IsShowing || step <= 0) return;
            step--;
            Display();
        }
        public void Complete()
        {
            if (!IsShowing) return;
            var callback = finished; finished = null;
            gameObject.SetActive(false);
            callback?.Invoke();
        }
        private void Build(TMP_FontAsset font)
        {
            gameObject.layer = LayerMask.NameToLayer("UI");
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            canvasRect = root;
            var canvas = gameObject.AddComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = 5500;
            gameObject.AddComponent<GraphicRaycaster>();
            var block = gameObject.AddComponent<Image>(); block.color = new Color(0f, 0f, 0f, .001f); block.raycastTarget = true;
            for (int i = 0; i < 4; i++)
            {
                shades[i] = Box("Dim" + i, root, new Color(.025f, .015f, .02f, .66f));
                borders[i] = Box("FocusBorder" + i, root, new Color(1f, .76f, .24f)).GetComponent<Image>();
            }
            skin = Resources.LoadAll<Sprite>("UI/Tutorial/TutorialUI");
            // The supplied atlas includes the title plaque, parchment, buttons and decoration.
            card = new GameObject("GuideDialogue", typeof(RectTransform)).GetComponent<RectTransform>();
            card.gameObject.layer = gameObject.layer; card.SetParent(root, false);
            Picture("Parchment", card, "Panel", new Rect(0, 90, CardWidth, 380));
            heading = Label("TutorialHeading", card, font, 30f);
            heading.text = "동행요괴 튜토리얼"; heading.alignment = TextAlignmentOptions.Center;
            Place(heading.rectTransform, new Rect(205, 400, 490, 54));
            title = Label("Title", card, font, 24f); title.color = Ink;
            Place(title.rectTransform, new Rect(64, 345, 772, 42));
            Picture("Divider", card, "Divider", new Rect(60, 307, 780, 44));
            body = Label("Explanation", card, font, 21f); body.color = Ink;
            Place(body.rectTransform, new Rect(64, 153, 772, 144));
            controlsHint = Label("ControlsHint", card, font, 17f); controlsHint.color = Ink;
            controlsHint.text = "Space로 다음 · ESC로 창 닫기"; controlsHint.alignment = TextAlignmentOptions.Center;
            Place(controlsHint.rectTransform, new Rect(64, 112, 772, 30));
            Picture("CounterPlate", card, "Counter", new Rect(700, 8, 190, 60));
            counter = Label("Counter", card, font, 18f); counter.alignment = TextAlignmentOptions.Center;
            Place(counter.rectTransform, new Rect(715, 18, 160, 40));
            skipButton = MakeButton("Skip", "안내 닫기", "DarkButton", font, new Rect(470, 0, 210, 78), Complete).transform.parent.gameObject;
            previousButton = MakeButton("Previous", "이전", "DarkButton", font, new Rect(10, 0, 265, 78), Previous).transform.parent.GetComponent<Button>();
            nextLabel = MakeButton("Next", "다음", "LightButton", font, new Rect(315, 0, 265, 78), Advance);
        }
        private RectTransform Picture(string name, Transform parent, string spriteName, Rect bounds)
        {
            var rect = Box(name, parent, Color.white); Place(rect, bounds);
            rect.GetComponent<Image>().sprite = Art(spriteName);
            return rect;
        }
        private TMP_Text MakeButton(string name, string caption, string spriteName, TMP_FontAsset font, Rect bounds, UnityEngine.Events.UnityAction click)
        {
            var rect = Picture(name, card, spriteName, bounds);
            rect.GetComponent<Image>().raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.onClick.AddListener(click);
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { highlightedSprite = Art("LightButton"), pressedSprite = Art("DarkButton") };
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var text = Label("Label", rect, font, 22f); text.text = caption; text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(20f, 10f); text.rectTransform.offsetMax = new Vector2(-20f, -10f);
            return text;
        }
        private void Display()
        {
            stepChanged?.Invoke(step);
            title.text = activeTitles[step]; body.text = activeMessages[step];
            counter.text = dockRight ? (step + 1) + " / " + activeTitles.Length
                : step == 0 ? "도움말" : step + " / " + (activeTitles.Length - 1);
            previousButton.interactable = step > 0;
            previousButton.GetComponent<Image>().color = step > 0 ? Color.white : new Color(.55f, .55f, .55f, .7f);
            nextLabel.text = step == activeTitles.Length - 1 ? "안내 완료" : "다음";
            RefreshLayout();
        }
        private void LateUpdate()
        {
            if (card == null) return;
            RefreshLayout();
            float alpha = .65f + .35f * Mathf.Sin(Time.unscaledTime * 4f);
            foreach (var border in borders) border.color = new Color(1f, .76f, .24f, alpha);
        }
        private void RefreshLayout()
        {
            float width = canvasRect.rect.width, height = canvasRect.rect.height;
            Rect focus = new Rect();
            bool hasFocus = targets != null && step < targets.Length && targets[step] != null;
            if (hasFocus)
            {
                var corners = new Vector3[4]; targets[step].GetWorldCorners(corners);
                Vector2 min = canvasRect.InverseTransformPoint(corners[0]);
                Vector2 max = canvasRect.InverseTransformPoint(corners[2]);
                min -= canvasRect.rect.min; max -= canvasRect.rect.min;
                min -= Vector2.one * 12f; max += Vector2.one * 12f;
                min = Vector2.Max(Vector2.zero, min); max = Vector2.Min(new Vector2(width, height), max);
                focus = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                Place(shades[0], new Rect(0, 0, width, focus.yMin));
                Place(shades[1], new Rect(0, focus.yMax, width, Mathf.Max(0, height-focus.yMax)));
                Place(shades[2], new Rect(0, focus.yMin, focus.xMin, focus.height));
                Place(shades[3], new Rect(focus.xMax, focus.yMin, Mathf.Max(0, width-focus.xMax), focus.height));
                Place(borders[0].rectTransform, new Rect(focus.xMin, focus.yMin, focus.width, 3));
                Place(borders[1].rectTransform, new Rect(focus.xMin, focus.yMax-3, focus.width, 3));
                Place(borders[2].rectTransform, new Rect(focus.xMin, focus.yMin, 3, focus.height));
                Place(borders[3].rectTransform, new Rect(focus.xMax-3, focus.yMin, 3, focus.height));
            }
            else
            {
                Place(shades[0], new Rect(0, 0, width, height));
                for (int i = 1; i < 4; i++) Place(shades[i], new Rect());
            }
            foreach (var border in borders) border.gameObject.SetActive(hasFocus);
            if (dockRight)
            {
                float guideScale = Mathf.Min(1f, (width * .48f - 32f) / CardWidth, (height - 80f) / CardHeight);
                card.localScale = Vector3.one * guideScale;
                Place(card, new Rect(width - CardWidth * guideScale - 24f,
                    (height - CardHeight * guideScale) * .5f, CardWidth, CardHeight));
                return;
            }
            // Keep the dialogue away from the highlighted controls, including the bottom action buttons.
            float availableBelow = hasFocus ? focus.yMin - 48f : height - 48f;
            float availableAbove = hasFocus ? height - focus.yMax - 48f : height - 48f;
            bool placeAbove = hasFocus && availableAbove > availableBelow;
            float availableHeight = Mathf.Max(availableBelow, availableAbove);
            float scale = Mathf.Min(1f, (width - 40f) / CardWidth, availableHeight / CardHeight);
            card.localScale = Vector3.one * scale;
            float y = !hasFocus ? (height - CardHeight * scale) * .5f
                : placeAbove ? height - CardHeight * scale - 24f : 24f;
            Place(card, new Rect((width - CardWidth * scale) * .5f, y, CardWidth, CardHeight));
        }
        private static RectTransform Box(string name, Transform parent, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image)); obj.layer = LayerMask.NameToLayer("UI"); obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return (RectTransform)obj.transform;
        }
        private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, float size)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); obj.layer = LayerMask.NameToLayer("UI"); obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<TMP_Text>(); text.font = font; text.fontSize = size; text.color = new Color(1f, .91f, .73f);
            text.alignment = TextAlignmentOptions.TopLeft; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }
        private static void Place(RectTransform rect, Rect bounds)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = bounds.position; rect.sizeDelta = bounds.size;
        }
    }
}
