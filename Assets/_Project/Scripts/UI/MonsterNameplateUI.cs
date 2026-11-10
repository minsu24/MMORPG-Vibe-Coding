using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    // Kept separate from the monster hierarchy so facing flips and scale never affect the text.
    [DisallowMultipleComponent]
    public sealed class MonsterNameplateUI : MonoBehaviour
    {
        private static Canvas canvas;
        private static MonsterNameplateView prefab;
        private EnemyController owner;
        private MonsterNameplateView view;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { canvas = null; prefab = null; }

        public void Initialize(EnemyController enemy) { owner = enemy; }

        private static Canvas GetCanvas()
        {
            if (canvas != null) return canvas;
            var root = new GameObject("MonsterNameplates", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            DontDestroyOnLoad(root);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            return canvas;
        }

        private void LateUpdate()
        {
            var camera = Camera.main;
            bool visible = owner != null && owner.ShouldShowNameplate && camera != null;
            Vector3 screen = visible ? camera.WorldToScreenPoint(owner.NameplatePosition) : Vector3.zero;
            visible &= screen.z > 0f && screen.x >= 0 && screen.x <= Screen.width && screen.y >= 0 && screen.y <= Screen.height;
            if (!visible) { Hide(); return; }
            if (view == null)
            {
                if (prefab == null) prefab = Resources.Load<MonsterNameplateView>("UI/MonsterNameplate");
                if (prefab == null) return;
                view = Instantiate(prefab, GetCanvas().transform);
                view.SetMonster(owner);
            }
            view.gameObject.SetActive(true);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screen, null, out var position);
            ((RectTransform)view.transform).anchoredPosition = position;
        }

        private void Hide() { if (view != null) view.gameObject.SetActive(false); }
        private void OnDisable() { Hide(); }
        private void OnDestroy() { if (view != null) Destroy(view.gameObject); }
    }
}
