using EasternFantasy.EquipmentUpgrade;
using EasternFantasy.Inventory;
using UnityEngine;

namespace EasternFantasy.UI
{
    public sealed partial class EquipmentUpgradeWindowUI
    {
        // The supplied sound builds for 1.7 seconds before its bright impact.
        private const float UpgradeImpactTime = 1.72f;
        private const float UpgradePresentationDuration = 3.15f;
        private AudioSource upgradeAudio;
        private AudioClip upgradeSuccessClip, upgradeInsufficientClip;
        private UpgradeEnergyGraphic upgradeEnergy;
        private ItemDefinition pendingUpgradeTarget;
        private EquipmentUpgradeRecipe pendingUpgradeRecipe;
        private float upgradeElapsed;
        private bool upgradeApplied;

        private void PrepareUpgradeAudio()
        {
            if (upgradeAudio == null)
            {
                upgradeAudio = gameObject.AddComponent<AudioSource>();
                upgradeAudio.playOnAwake = false;
                upgradeAudio.loop = false;
                upgradeAudio.spatialBlend = 0f;
                upgradeAudio.ignoreListenerPause = true;
                upgradeAudio.volume = .65f;
            }
            if (upgradeSuccessClip == null) upgradeSuccessClip = Resources.Load<AudioClip>("Audio/UpgradeSuccess");
            if (upgradeInsufficientClip == null) upgradeInsufficientClip = Resources.Load<AudioClip>("Audio/UpgradeInsufficient");
        }

        private void ShowUpgradeError(string error)
        {
            messageText.text = error;
            if (string.IsNullOrEmpty(error) || (!error.Contains("부족") && error != "필요한 재료를 모두 넣어 주세요.")) return;
            PrepareUpgradeAudio();
            upgradeAudio.Stop();
            upgradeAudio.clip = upgradeInsufficientClip;
            if (upgradeInsufficientClip != null) upgradeAudio.Play();
        }

        private void BeginUpgradePresentation()
        {
            PrepareUpgradeAudio();
            pendingUpgradeTarget = target;
            pendingUpgradeRecipe = currentRecipe;
            upgradeElapsed = 0f;
            upgradeApplied = false;
            if (windowGroup != null) windowGroup.interactable = false;
            confirmButton.interactable = false;
            messageText.text = "장비에 기운이 모이고 있습니다…";
            var go = new GameObject("Upgrade Gathering and Radiance", typeof(RectTransform), typeof(CanvasRenderer), typeof(UpgradeEnergyGraphic));
            go.layer = targetIcon.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(targetIcon.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = targetIcon.rectTransform.rect.center;
            rect.sizeDelta = new Vector2(620f, 500f);
            upgradeEnergy = go.GetComponent<UpgradeEnergyGraphic>();
            upgradeEnergy.raycastTarget = false;
            upgradeEnergy.DrawAt(0f, UpgradeImpactTime, UpgradePresentationDuration);
            upgradeAudio.Stop();
            upgradeAudio.clip = upgradeSuccessClip;
            if (upgradeSuccessClip != null) upgradeAudio.Play();
        }

        private void TickUpgradePresentation(float deltaTime)
        {
            if (!committing || pendingUpgradeTarget == null) return;
            if (inventory == null || currency == null) { CancelUpgradePresentation(); CloseInstant(); return; }
            upgradeElapsed += Mathf.Max(0f, deltaTime);
            if (!upgradeApplied && upgradeElapsed >= UpgradeImpactTime)
            {
                // Revalidate and consume exactly once at the visual/audio impact.
                upgradeApplied = true;
                if (!inventory.TryUpgradeEquipment(pendingUpgradeTarget, pendingUpgradeRecipe, currency, out var result, out string error))
                {
                    upgradeAudio.Stop();
                    FinishUpgradePresentation();
                    ShowUpgradeError(error);
                    return;
                }
                target = result;
                insertedMaterials.Clear();
                committing = false;
                Refresh();
                committing = true;
                confirmButton.interactable = false;
                messageText.text = result.GradedDisplayName + " 승급 완료!";
                onUpgradeSucceeded.Invoke(result);
                Upgraded?.Invoke(result);
                // A success observer may unload or disable this window.
                if (!committing || !IsOpen) return;
            }
            if (upgradeEnergy != null) upgradeEnergy.DrawAt(upgradeElapsed, UpgradeImpactTime, UpgradePresentationDuration);
            if (upgradeElapsed >= UpgradePresentationDuration) FinishUpgradePresentation();
        }

        private void FinishUpgradePresentation()
        {
            RemoveUpgradeEnergy();
            pendingUpgradeTarget = null;
            pendingUpgradeRecipe = null;
            committing = false;
            if (windowGroup != null && IsOpen && !closing) windowGroup.interactable = true;
            Refresh();
        }

        private void CancelUpgradePresentation()
        {
            if (upgradeAudio != null) upgradeAudio.Stop();
            RemoveUpgradeEnergy();
            pendingUpgradeTarget = null;
            pendingUpgradeRecipe = null;
            committing = false;
        }

        private void RemoveUpgradeEnergy()
        {
            if (upgradeEnergy != null)
            {
                upgradeEnergy.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(upgradeEnergy.gameObject);
                else DestroyImmediate(upgradeEnergy.gameObject);
            }
            upgradeEnergy = null;
        }
    }
}
