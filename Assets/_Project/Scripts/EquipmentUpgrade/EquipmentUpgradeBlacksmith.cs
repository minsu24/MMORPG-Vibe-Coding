using EasternFantasy.Dialogue;
using EasternFantasy.UI;
using UnityEngine;

namespace EasternFantasy.EquipmentUpgrade
{
    [DisallowMultipleComponent]
    public sealed class EquipmentUpgradeBlacksmith : MonoBehaviour
    {
        [SerializeField] private DialogueSequence greetingDialogue;
        [Header("Optional existing UI prefabs for maps opened directly")]
        [SerializeField] private GameObject upgradeWindowPrefab;
        [SerializeField] private GameObject dialogueUiPrefab;
        private DialogueManager manager;
        public DialogueSequence GreetingDialogue => greetingDialogue;

        private void Awake() => GroundContactShadow.AttachToNpc(gameObject);

        public void Interact()
        {
            // Reuse live services when arriving from another map; create the same UI only if absent.
            if (EquipmentUpgradeWindowUI.Instance == null && upgradeWindowPrefab != null)
                CreateUi(upgradeWindowPrefab);
            if (DialogueManager.Instance == null && dialogueUiPrefab != null)
                CreateUi(dialogueUiPrefab);
            if (EquipmentUpgradeWindowUI.Instance == null || EquipmentUpgradeWindowUI.Instance.IsOpen) return;
            var current = DialogueManager.Instance;
            if (greetingDialogue == null || current == null || current.IsDialogueActive || manager != null) return;
            manager = current;
            manager.DialogueCompleted += OnCompleted;
            manager.DialogueEnded += OnEnded;
            if (!manager.StartDialogue(greetingDialogue)) StopWaiting();
        }
        private void CreateUi(GameObject prefab)
        {
            var instance = Instantiate(prefab);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, gameObject.scene);
        }
        private void OnCompleted(DialogueSequence sequence)
        {
            if (sequence != greetingDialogue) return;
            StopAllCoroutines();
            StopWaiting();
            if (isActiveAndEnabled) EquipmentUpgradeWindowUI.Instance?.Open();
        }
        private void OnEnded()
        {
            // Completed is raised after Ended. Delay cancellation to distinguish normal completion.
            StartCoroutine(CancelAfterEnd());
        }
        private System.Collections.IEnumerator CancelAfterEnd()
        {
            yield return null;
            StopWaiting();
        }
        private void StopWaiting()
        {
            if (manager != null)
            {
                manager.DialogueCompleted -= OnCompleted;
                manager.DialogueEnded -= OnEnded;
            }
            manager = null;
        }
        private void OnDisable() { StopAllCoroutines(); StopWaiting(); }
    }
}
