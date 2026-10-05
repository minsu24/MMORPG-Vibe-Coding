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

        private DialogueManager activeDialogueManager;

        public ItemDefinition[] Stock => stock;

        public void Interact()
        {
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
            if (!manager.StartDialogue(greetingDialogue))
                StopWaitingForDialogue();
        }

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
            if (ShopWindowUI.Instance == null)
            {
                Debug.LogWarning("The scene needs a ShopWindowUI.", this);
                return;
            }

            ShopWindowUI.Instance.Open(this);
        }
    }
}
