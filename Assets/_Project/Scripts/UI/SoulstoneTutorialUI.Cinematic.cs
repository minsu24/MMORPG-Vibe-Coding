using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasternFantasy.UI
{
    public sealed partial class SoulstoneTutorialUI
    {
        private Coroutine activationSequence;
        private Camera cinematicCamera;
        private CinemachineBrain brain;
        private bool cameraCaptured, brainWasEnabled;
        private Vector3 cameraPosition;
        private Quaternion cameraRotation;
        private float cameraSize;
        private Collider2D cameraBounds;
        private readonly List<Canvas> hiddenCanvases = new List<Canvas>();
        private readonly List<SpriteRenderer> hiddenPlayerRenderers = new List<SpriteRenderer>();
        public bool IsActivationCloseup => cameraCaptured;

        private IEnumerator ActivateAndContinue()
        {
            waitingForAnimation = true;
            guide.gameObject.SetActive(false);
            HideOtherCanvases();
            if (!SoulstoneProgress.IsActivated)
            {
                if (input != null)
                    foreach (var renderer in input.GetComponentsInChildren<SpriteRenderer>())
                        if (renderer.enabled) { hiddenPlayerRenderers.Add(renderer); renderer.enabled = false; }
                cinematicCamera = Camera.main;
                if (cinematicCamera != null)
                {
                    cameraCaptured = true;
                    cameraPosition = cinematicCamera.transform.position;
                    cameraRotation = cinematicCamera.transform.rotation;
                    cameraSize = cinematicCamera.orthographicSize;
                    brain = cinematicCamera.GetComponent<CinemachineBrain>();
                    brainWasEnabled = brain != null && brain.enabled;
                    if (brain != null) brain.enabled = false;
                    foreach (var shape in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
                        if (shape.name == "CameraCollider" && shape.gameObject.scene == SceneManager.GetActiveScene())
                        { cameraBounds = shape; break; }
                    var renderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
                    Vector3 focus = renderer != null ? renderer.bounds.center : input.transform.position;
                    focus.z = cameraPosition.z;
                    // Sprite bounds include transparent padding; cap the view to give the visible stone a clear close-up.
                    float size = Mathf.Min(cameraSize, Mathf.Max(1.4f,
                        Mathf.Min(cameraSize * .42f, renderer != null ? renderer.bounds.extents.y * 1.5f : 2f)));
                    yield return MoveActivationCamera(focus, size, .55f);
                }
                // Start frame zero only once the stone fills the camera, so none of the awakening is missed.
                if (!running) yield break;
                SoulstoneProgress.Activate();
                while (visual != null && visual.IsActivating) yield return null;
                yield return new WaitForSecondsRealtime(.4f);
                if (cameraCaptured && cinematicCamera != null)
                    yield return MoveActivationCamera(cameraPosition, cameraSize, .45f);
                RestoreActivationCamera();
                RestorePlayerRenderers();
            }
            if (running) ContinueInWindow();
            activationSequence = null;
        }
        private IEnumerator MoveActivationCamera(Vector3 destination, float size, float seconds)
        {
            if (cinematicCamera == null) yield break;
            Vector3 from = cinematicCamera.transform.position;
            float fromSize = cinematicCamera.orthographicSize;
            float elapsed = 0;
            while (elapsed < seconds && cinematicCamera != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / seconds));
                Vector3 position = Vector3.Lerp(from, destination, t);
                float currentSize = Mathf.Lerp(fromSize, size, t);
                if (cameraBounds != null)
                {
                    var bounds = cameraBounds.bounds;
                    currentSize = Mathf.Min(currentSize, bounds.extents.y, bounds.extents.x / cinematicCamera.aspect);
                    float halfWidth = currentSize * cinematicCamera.aspect;
                    position.x = Mathf.Clamp(position.x, bounds.min.x + halfWidth, bounds.max.x - halfWidth);
                    position.y = Mathf.Clamp(position.y, bounds.min.y + currentSize, bounds.max.y - currentSize);
                }
                cinematicCamera.transform.position = position;
                cinematicCamera.orthographicSize = currentSize;
                yield return null;
            }
        }
        private void HideOtherCanvases()
        {
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.enabled || canvas.transform.IsChildOf(canvasObject.transform)
                    || canvas.GetComponent<SoulstoneWindowView>() != null) continue;
                hiddenCanvases.Add(canvas); canvas.enabled = false;
            }
        }
        private void LateUpdate()
        {
            // Scene-entry banners may enable themselves after loading; keep the cinematic/window unobstructed.
            if (running)
                foreach (var canvas in hiddenCanvases)
                    if (canvas != null) canvas.enabled = false;
        }
        private void RestoreActivationCamera()
        {
            if (!cameraCaptured) return;
            if (cinematicCamera != null)
            {
                cinematicCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                cinematicCamera.orthographicSize = cameraSize;
            }
            if (brain != null) brain.enabled = brainWasEnabled;
            cameraCaptured = false; cinematicCamera = null; brain = null; cameraBounds = null;
        }
        private void RestorePlayerRenderers()
        {
            foreach (var renderer in hiddenPlayerRenderers) if (renderer != null) renderer.enabled = true;
            hiddenPlayerRenderers.Clear();
        }
        private void StopActivationSequence()
        {
            if (activationSequence != null) StopCoroutine(activationSequence);
            activationSequence = null;
            RestoreActivationCamera(); RestorePlayerRenderers();
            foreach (var canvas in hiddenCanvases) if (canvas != null) canvas.enabled = true;
            hiddenCanvases.Clear();
        }
    }
}
