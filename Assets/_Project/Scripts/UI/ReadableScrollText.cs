using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    public sealed class ReadableScrollText : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        private RectTransform content;
        private ScrollRect scroll;
        private string previousText;
        private Vector2 previousViewport;
        private void Awake() => Initialize();
        private void OnEnable() { Initialize(); Refresh(); }
        public void Initialize()
        {
            if (scroll != null) return;
            scroll = GetComponent<ScrollRect>();
            if (scroll == null || text == null) return;
            content = scroll.content;
        }
        private void LateUpdate()
        {
            if (text != null && scroll != null && (previousText != text.text || previousViewport != scroll.viewport.rect.size)) Refresh();
        }
        public void Refresh()
        {
            if (text == null || content == null) return;
            previousText = text.text;
            previousViewport = scroll.viewport.rect.size;
            float height = Mathf.Max(scroll.viewport.rect.height,
                text.GetPreferredValues(text.text, Mathf.Max(1,scroll.viewport.rect.width - 16f),Mathf.Infinity).y + 12f);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
            text.rectTransform.sizeDelta = new Vector2(-16f,height);
            scroll.StopMovement();
            content.anchoredPosition = Vector2.zero;
        }
    }
}