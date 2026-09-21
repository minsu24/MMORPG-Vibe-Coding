using UnityEngine;
using UnityEngine.EventSystems;

namespace EasternFantasy.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EventSystem))]
    public sealed class PersistentEventSystem : MonoBehaviour
    {
        // Marker component. EventSystems remain scene-local so scene transitions
        // never create two simultaneously active EventSystems.
    }
}
