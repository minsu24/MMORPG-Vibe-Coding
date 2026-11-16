using EasternFantasy.CharacterSelection;
using EasternFantasy.Player;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    public sealed partial class DungeonWindowUI
    {
        private RectTransform tutorialCanvasRect;
        private RectTransform[] tutorialTargets;
        private readonly System.Collections.Generic.List<GameObject> tutorialClickAreas = new System.Collections.Generic.List<GameObject>();
        private CharacterClassId tutorialClass;

        private void BeginTutorial()
        {
            if (tutorialCompletedClasses.Contains(CharacterSelectionState.SelectedClass) || IsTutorialRunning || entries.Count == 0) return;
            tutorialClass = CharacterSelectionState.SelectedClass;
            tutorialInput = player.GetComponent<PlayerInputReader>();
            if (tutorialInput != null) { tutorialInputWasEnabled = tutorialInput.enabled; tutorialInput.enabled = false; }
            tutorialCanvas = new GameObject("DungeonTutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = tutorialCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 7000;
            var scaler = tutorialCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            tutorialCanvasRect = (RectTransform)tutorialCanvas.transform;
            tutorialPanel = list.transform.parent as RectTransform;
            tutorialPosition = tutorialPanel.localPosition; tutorialScale = tutorialPanel.localScale;
            tutorialTargets = new RectTransform[5];
            tutorialTargets[0] = list.viewport != null ? list.viewport : (RectTransform)list.transform;
            tutorialTargets[1] = AddTutorialClickArea(preview.rectTransform.parent, 1);
            // The three condition panels share one confirmation area.
            tutorialTargets[2] = AddTutorialClickArea(tutorialPanel, 2);
            var conditions = tutorialTargets[2];
            conditions.anchorMin = conditions.anchorMax = conditions.pivot = new Vector2(0, 1);
            conditions.anchoredPosition = new Vector2(465, -650);
            conditions.sizeDelta = new Vector2(605, 85);
            tutorialTargets[3] = AddTutorialClickArea(tutorialPanel.Find("RewardsPanel"), 3);
            tutorialTargets[4] = (RectTransform)enterButton.transform;
            Canvas.ForceUpdateCanvases();
            PlaceTutorialPanel();
            var overlay = new GameObject("DungeonGuide", typeof(RectTransform), typeof(PetCompanionTutorialUI));
            overlay.transform.SetParent(tutorialCanvas.transform, false);
            tutorial = overlay.GetComponent<PetCompanionTutorialUI>();
            tutorial.ShowCustom(dungeonName.font, "던전 튜토리얼",
                new[] { "1. 던전 직접 선택하기", "2. 선택한 던전 확인", "3. 입장 조건 확인", "4. 클리어 보상 확인", "5. 직접 던전 입장하기" },
                new[] {
                    "오른쪽 목록에서 도전할 던전을 직접 클릭하세요.\n목록이 길면 스크롤해서 다른 던전도 살펴볼 수 있어요.",
                    "선택한 던전의 그림과 설명이 표시됩니다.\n큰 미리보기 화면을 클릭해 확인하세요.",
                    "입장 레벨과 제한시간을 확인한 뒤 강조된 정보 칸을 클릭하세요.\n던전 안에서는 메뉴를 열어도 제한시간이 흘러요.",
                    "아래 아이템은 이 던전에서 얻을 수 있는 보상입니다.\n강조된 보상 칸을 클릭해 확인하세요.",
                    "준비가 됐다면 ‘던전 입장’을 클릭하세요. 실제 던전으로 이동합니다.\n다음부터는 O키로 창을 열고, O키나 Esc로 닫을 수 있어요." },
                tutorialTargets, PrepareTutorialStep, CompleteTutorial, true, false);
            tutorial.GetComponent<Canvas>().sortingOrder = 7100;
            tutorial.SetCompactBottomLayout();
        }

        private RectTransform AddTutorialClickArea(Transform parent, int step)
        {
            var area = new GameObject("DungeonTutorialAction" + step, typeof(RectTransform), typeof(Image), typeof(Button));
            area.layer = LayerMask.NameToLayer("UI");
            area.transform.SetParent(parent, false);
            var rect = (RectTransform)area.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            area.GetComponent<Image>().color = Color.clear;
            var button = area.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => { if (IsTutorialRunning && tutorial.CurrentStep == step) tutorial.CompleteAction(); });
            tutorialClickAreas.Add(area);
            return rect;
        }

        private void PrepareTutorialStep(int step)
        {
            tutorial.ClearKeyHint();
            tutorial.SetFocusInteraction(true);
            if (step == 4 && !CanEnter(Selected))
            {
                // A locked or unconfigured dungeon must not trap the player in the guide.
                tutorial.SetFocusInteraction(false);
                tutorial.SetControlsHint("아직 입장할 수 없어요. Space 또는 안내 완료 후 입장 조건을 확인하세요.");
                return;
            }
            string[] instructions = {
                "오른쪽 던전 목록을 직접 클릭하세요.", "던전 미리보기 화면을 클릭하세요.",
                "입장 조건 정보 칸을 클릭하세요.", "클리어 보상 칸을 클릭하세요.", "던전 입장 버튼을 직접 클릭하세요." };
            tutorial.RequireAction(instructions[step]);
        }

        private void LateUpdate()
        {
            if (IsTutorialRunning) PlaceTutorialPanel();
        }

        private void PlaceTutorialPanel()
        {
            if (tutorialCanvasRect == null || tutorialPanel == null) return;
            tutorialPanel.localPosition = tutorialPosition; tutorialPanel.localScale = tutorialScale;
            var corners = new Vector3[4]; tutorialPanel.GetWorldCorners(corners);
            Vector2 min = tutorialCanvasRect.InverseTransformPoint(corners[0]);
            Vector2 max = tutorialCanvasRect.InverseTransformPoint(corners[2]);
            float reserved = Mathf.Min(280f, tutorialCanvasRect.rect.height * .3f);
            float fit = Mathf.Min(1f, (tutorialCanvasRect.rect.width - 48f) / Mathf.Max(1f, max.x - min.x),
                (tutorialCanvasRect.rect.height - reserved - 24f) / Mathf.Max(1f, max.y - min.y));
            tutorialPanel.localScale *= fit;
            Vector2 center = tutorialCanvasRect.rect.min + new Vector2(tutorialCanvasRect.rect.width * .5f,
                reserved + (tutorialCanvasRect.rect.height - reserved) * .5f);
            tutorialPanel.position += tutorialCanvasRect.TransformPoint(center) - tutorialPanel.TransformPoint(tutorialPanel.rect.center);
        }

        private void CompleteTutorial()
        {
            tutorialCompletedClasses.Add(tutorialClass);
            EndTutorial();
        }

        private void EndTutorial()
        {
            if (tutorialCanvas == null) return;
            if (tutorial != null) tutorial.Hide();
            foreach (var area in tutorialClickAreas)
                if (area != null) { area.SetActive(false); Destroy(area); }
            tutorialClickAreas.Clear();
            if (tutorialPanel != null) { tutorialPanel.localPosition = tutorialPosition; tutorialPanel.localScale = tutorialScale; }
            if (tutorialInput != null) tutorialInput.enabled = tutorialInputWasEnabled;
            if (player != null) player.ClearInput();
            Destroy(tutorialCanvas);
            tutorialCanvas = null; tutorial = null; tutorialCanvasRect = null; tutorialTargets = null;
        }
    }
}
