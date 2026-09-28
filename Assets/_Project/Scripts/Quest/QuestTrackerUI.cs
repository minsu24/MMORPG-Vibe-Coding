using System.Text;
using TMPro;
using UnityEngine;

namespace EasternFantasy.Quest
{
    [DisallowMultipleComponent]
    public sealed class QuestTrackerUI : MonoBehaviour
    {
        [SerializeField] private GameObject trackerRoot;
        [SerializeField] private TMP_Text questListText;

        private QuestManager manager;
        private Transform markerTarget;
        private RectTransform edgeMarker;
        private TMP_Text edgeMarkerText;
        private float nextTargetRefresh;

        private void Start()
        {
            manager = QuestManager.Instance;
            if (manager == null)
            {
                Debug.LogError("QuestTrackerUI needs an active QuestManager.", this);
                if (trackerRoot != null) trackerRoot.SetActive(false);
                return;
            }

            manager.QuestsChanged += Refresh;
            Refresh();
            CreateEdgeMarker();
        }

        private void Update()
        {
            if (edgeMarker == null)
                return;
            if (Time.unscaledTime >= nextTargetRefresh)
            {
                nextTargetRefresh = Time.unscaledTime + 0.5f;
                markerTarget = FindMarkerTarget();
            }

            Camera camera = Camera.main;
            if (markerTarget == null || camera == null)
            {
                edgeMarker.gameObject.SetActive(false);
                return;
            }

            Vector3 screenPoint = camera.WorldToScreenPoint(
                markerTarget.position + Vector3.up * 1.3f);
            if (screenPoint.z < 0f)
            {
                screenPoint.x = Screen.width - screenPoint.x;
                screenPoint.y = Screen.height - screenPoint.y;
            }
            RectTransform canvasRect = edgeMarker.parent as RectTransform;
            Canvas canvas = edgeMarker.GetComponentInParent<Canvas>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
                screenPoint, canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? canvas.worldCamera : null, out Vector2 localPoint);
            Vector2 half = canvasRect.rect.size * 0.5f;
            Vector2 limit = new Vector2(Mathf.Max(20f, half.x - 95f),
                Mathf.Max(20f, half.y - 55f));
            if (Mathf.Abs(localPoint.x) < limit.x
                && Mathf.Abs(localPoint.y) < limit.y)
            {
                edgeMarker.gameObject.SetActive(false);
                return;
            }

            edgeMarker.gameObject.SetActive(true);
            edgeMarker.anchoredPosition = new Vector2(
                Mathf.Clamp(localPoint.x, -limit.x, limit.x),
                Mathf.Clamp(localPoint.y, -limit.y, limit.y));
            edgeMarkerText.text = Mathf.Abs(localPoint.x) > Mathf.Abs(localPoint.y)
                ? (localPoint.x < 0f ? "< QUEST" : "QUEST >")
                : (localPoint.y < 0f ? "v QUEST" : "^ QUEST");
        }

        private void CreateEdgeMarker()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;
            GameObject marker = new GameObject("Quest Edge Marker",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            marker.transform.SetParent(canvas.rootCanvas.transform, false);
            edgeMarker = marker.GetComponent<RectTransform>();
            edgeMarker.anchorMin = edgeMarker.anchorMax = new Vector2(0.5f, 0.5f);
            edgeMarker.sizeDelta = new Vector2(185f, 52f);
            edgeMarkerText = marker.GetComponent<TextMeshProUGUI>();
            edgeMarkerText.font = questListText.font;
            edgeMarkerText.fontSize = 25f;
            edgeMarkerText.fontStyle = FontStyles.Bold;
            edgeMarkerText.color = new Color(1f, 0.82f, 0.42f);
            edgeMarkerText.alignment = TextAlignmentOptions.Center;
            edgeMarkerText.raycastTarget = false;
            marker.SetActive(false);
        }

        private Transform FindMarkerTarget()
        {
            if (manager == null)
                return null;
            QuestGiver[] givers = FindObjectsByType<QuestGiver>(FindObjectsSortMode.None);
            foreach (QuestProgress progress in manager.Quests)
            {
                if (progress.Status != QuestStatus.ReadyToTurnIn)
                    continue;
                foreach (QuestGiver giver in givers)
                    if (giver.Quest == progress.Definition && giver.isActiveAndEnabled)
                        return giver.transform;
            }

            foreach (QuestProgress progress in manager.Quests)
            {
                if (progress.Status != QuestStatus.Active)
                    continue;
                MON_Ghost[] ghosts = FindObjectsByType<MON_Ghost>(FindObjectsSortMode.None);
                MON_Ghost nearest = null;
                float nearestDistance = float.PositiveInfinity;
                Camera camera = Camera.main;
                Vector3 origin = camera != null ? camera.transform.position : Vector3.zero;
                foreach (MON_Ghost ghost in ghosts)
                {
                    if (ghost.Quest != progress.Definition || ghost.HP <= 0f)
                        continue;
                    float distance = (ghost.transform.position - origin).sqrMagnitude;
                    if (distance < nearestDistance)
                    {
                        nearest = ghost;
                        nearestDistance = distance;
                    }
                }
                return nearest != null ? nearest.transform : null;
            }
            foreach (QuestGiver giver in givers)
                if (giver.isActiveAndEnabled && giver.CanOfferQuest)
                    return giver.transform;
            return null;
        }

        public void Refresh()
        {
            if (manager == null || trackerRoot == null || questListText == null)
                return;

            var builder = new StringBuilder();
            foreach (QuestProgress progress in manager.Quests)
            {
                if (progress.Status == QuestStatus.Completed)
                    continue;

                if (builder.Length > 0)
                    builder.AppendLine().AppendLine();

                QuestDefinition quest = progress.Definition;
                builder.Append('[').Append(quest.TypeLabel).Append("] ").AppendLine(quest.Title);
                builder.Append(quest.ObjectiveDescription);
                builder.Append("  ").Append(progress.CurrentAmount).Append('/').Append(quest.RequiredAmount);

                if (progress.Status == QuestStatus.ReadyToTurnIn)
                    builder.Append("  COMPLETE");
            }

            bool hasTrackedQuest = builder.Length > 0;
            trackerRoot.SetActive(hasTrackedQuest);
            questListText.text = builder.ToString();
        }

        private void OnDestroy()
        {
            if (manager != null)
                manager.QuestsChanged -= Refresh;
            if (edgeMarker != null)
                Destroy(edgeMarker.gameObject);
        }
    }
}
