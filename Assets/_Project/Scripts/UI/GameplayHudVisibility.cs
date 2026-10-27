using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.UI
{
    // Persistent HUDs must survive menu visits without covering their UI.
    [DisallowMultipleComponent]
    public sealed class GameplayHudVisibility : MonoBehaviour
    {
        private CanvasGroup group;
        private float gameplayAlpha;
        private bool gameplayInteractable, gameplayRaycasts;
        public static bool IsMenuScene
        {
            get
            {
                string scene = SceneManager.GetActiveScene().name;
                return scene == "MainScreen" || scene == "ChooseCharactor" || scene == "ChooseCharacter";
            }
        }
        public static void Attach(GameObject root)
        {
            if (root.GetComponent<GameplayHudVisibility>() == null) root.AddComponent<GameplayHudVisibility>();
        }
        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            gameplayAlpha = group.alpha;
            gameplayInteractable = group.interactable; gameplayRaycasts = group.blocksRaycasts;
        }
        private void OnEnable()
        {
            SceneManager.activeSceneChanged += SceneChanged;
            Apply();
        }
        private void SceneChanged(Scene previous, Scene next) => Apply();
        private void Apply()
        {
            group.alpha = IsMenuScene ? 0f : gameplayAlpha;
            group.interactable = !IsMenuScene && gameplayInteractable;
            group.blocksRaycasts = !IsMenuScene && gameplayRaycasts;
        }
        private void OnDisable() => SceneManager.activeSceneChanged -= SceneChanged;
    }
}
