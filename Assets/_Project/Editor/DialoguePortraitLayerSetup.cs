using EasternFantasy.Dialogue;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EasternFantasy.Editor
{
    public static class DialoguePortraitLayerSetup
    {
        [MenuItem("Eastern Fantasy/Move Dialogue Portraits Behind Panel")]
        public static void Apply()
        {
            DialogueManager manager = Object.FindFirstObjectByType<DialogueManager>();
            if (manager == null)
                throw new MissingReferenceException("The active scene needs a DialogueManager.");

            SerializedObject managerData = new SerializedObject(manager);
            GameObject dialogueRoot = (GameObject)managerData.FindProperty("dialogueRoot").objectReferenceValue;
            Image playerPortrait = (Image)managerData.FindProperty("playerPortrait").objectReferenceValue;
            Image partnerPortrait = (Image)managerData.FindProperty("partnerPortrait").objectReferenceValue;

            if (dialogueRoot == null || playerPortrait == null || partnerPortrait == null)
                throw new MissingReferenceException("Dialogue UI portrait references are incomplete.");

            Transform canvas = dialogueRoot.transform.parent;
            Transform existing = canvas.Find("DialoguePortraitLayer");
            GameObject layer;

            if (existing != null)
            {
                layer = existing.gameObject;
            }
            else
            {
                layer = new GameObject("DialoguePortraitLayer", typeof(RectTransform));
                layer.layer = LayerMask.NameToLayer("UI");
                layer.transform.SetParent(canvas, false);

                RectTransform rect = layer.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            // Earlier Canvas siblings render first, so the dialogue panel covers this layer.
            layer.transform.SetSiblingIndex(dialogueRoot.transform.GetSiblingIndex());
            playerPortrait.transform.SetParent(layer.transform, true);
            partnerPortrait.transform.SetParent(layer.transform, true);

            managerData.FindProperty("portraitRoot").objectReferenceValue = layer;
            managerData.ApplyModifiedPropertiesWithoutUndo();
            layer.SetActive(false);

            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Dialogue portraits now render behind the dialogue panel.");
        }
    }
}
