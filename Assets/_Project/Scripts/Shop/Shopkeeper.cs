using EasternFantasy.Dialogue;
using EasternFantasy.Inventory;
using EasternFantasy.UI;
using UnityEngine;

namespace EasternFantasy.Shop
{
    [DisallowMultipleComponent]
    public sealed class Shopkeeper : MonoBehaviour
    {
        [SerializeField] private ItemDefinition[] stock;
        [SerializeField] private DialogueSequence greetingDialogue;
        [SerializeField] private string unvisitedRegion;
        [SerializeField] private DialogueSequence unvisitedRegionDialogue;
        [Header("Optional existing UI prefabs for maps opened directly")]
        [SerializeField] private GameObject shopWindowPrefab;
        [SerializeField] private GameObject dialogueUiPrefab;

        private DialogueManager activeDialogueManager;

        public ItemDefinition[] Stock => stock;

        public void Interact()
        {
            if (ShopWindowUI.Instance != null && ShopWindowUI.Instance.IsOpen) return;
            EnsureUi();
            if (greetingDialogue == null)
            {
                OpenShop();
                return;
            }

            DialogueManager manager = DialogueManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("The scene needs a DialogueManager for the shop greeting.", this);
                return;
            }

            if (activeDialogueManager != null || manager.IsDialogueActive)
                return;

            activeDialogueManager = manager;
            manager.DialogueEnded += OnGreetingEnded;
            if (CanTellRegionRumor && DialogueTrigger.TryPlayOnce(manager, unvisitedRegionDialogue))
                return;
            if (!manager.StartDialogue(greetingDialogue))
                StopWaitingForDialogue();
        }

        private bool CanTellRegionRumor => unvisitedRegionDialogue != null
            && !string.IsNullOrEmpty(unvisitedRegion) && !MapRegionDiscovery.IsVisited(unvisitedRegion);

        private void OnGreetingEnded()
        {
            bool canOpen = isActiveAndEnabled && activeDialogueManager != null
                && activeDialogueManager.isActiveAndEnabled;
            StopWaitingForDialogue();
            if (canOpen)
                OpenShop();
        }

        private void StopWaitingForDialogue()
        {
            if (activeDialogueManager != null)
                activeDialogueManager.DialogueEnded -= OnGreetingEnded;
            activeDialogueManager = null;
        }

        private void OnDisable()
        {
            StopWaitingForDialogue();
        }

        public void OpenShop()
        {
            EnsureUi();
            if (ShopWindowUI.Instance == null)
            {
                Debug.LogWarning("The scene needs a ShopWindowUI.", this);
                return;
            }

            ShopWindowUI.Instance.Open(this);
        }

        private void EnsureUi()
        {
            if (ShopWindowUI.Instance == null && shopWindowPrefab != null) CreateUi(shopWindowPrefab);
            if (DialogueManager.Instance == null && dialogueUiPrefab != null) CreateUi(dialogueUiPrefab);
        }

        private void CreateUi(GameObject prefab)
        {
            var instance = Instantiate(prefab);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, gameObject.scene);
        }
    }
}
