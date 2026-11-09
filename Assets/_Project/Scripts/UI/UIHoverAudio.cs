using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    // Use the top UI raycast and its real pointer handler, so nested icons cannot
    // stack cues and runtime-created buttons/slots work without prefab changes.
    public sealed class UIHoverAudio : MonoBehaviour
    {
        private static UIHoverAudio instance;
        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        private EventSystem eventSystem;
        private PointerEventData pointer;
        private GameObject hovered;
        private AudioSource source;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (instance != null) return;
            var go = new GameObject("UI Hover Audio");
            go.AddComponent<UIHoverAudio>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            source.volume = .25f;
            source.clip = Resources.Load<AudioClip>("Audio/UIHover");
        }

        private void LateUpdate()
        {
            var current = EventSystem.current;
            if (current == null || Mouse.current == null) { SetHovered(null); return; }
            if (eventSystem != current)
            {
                eventSystem = current;
                pointer = new PointerEventData(current);
            }
            pointer.position = Mouse.current.position.ReadValue();
            hits.Clear();
            current.RaycastAll(pointer, hits);
            GameObject target = hits.Count > 0
                ? ExecuteEvents.GetEventHandler<IPointerEnterHandler>(hits[0].gameObject) : null;
            SetHovered(target);
        }

        private void SetHovered(GameObject target)
        {
            if (target != null)
            {
                var selectable = target.GetComponent<Selectable>();
                if (!target.activeInHierarchy || (selectable != null && !selectable.IsInteractable())) target = null;
            }
            if (hovered == target) return;
            hovered = target;
            if (target == null || source.clip == null) return;
            source.Stop();
            source.Play();
        }

        private void OnDisable()
        {
            hovered = null;
            if (source != null) source.Stop();
        }

        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
