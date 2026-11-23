using EasternFantasy.Inventory;
using EasternFantasy.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EasternFantasy.UI
{
    public sealed partial class QuickSlotBarUI
    {
        private PlayerEntity buffPlayer;
        private RectTransform potionBuffRow;
        private PotionBuffIndicator attackBuffIndicator;
        private PotionBuffIndicator reductionBuffIndicator;

        private void RefreshPotionBuffs(PlayerEntity player)
        {
            ItemDefinition attack = null;
            ItemDefinition reduction = null;
            float attackRemaining = 0f, reductionRemaining = 0f;
            if (player != null && !player.IsDead)
            {
                attack = player.GetActivePotionBuff(ConsumableEffect.IncreaseAttackPower, out attackRemaining);
                reduction = player.GetActivePotionBuff(ConsumableEffect.IncreaseDamageReduction, out reductionRemaining);
            }

            bool visible = attack != null || reduction != null;
            if (potionBuffRow == null)
            {
                if (!visible) return;
                CreatePotionBuffRow();
                if (potionBuffRow == null) return;
            }

            attackBuffIndicator.Refresh(attack, attackRemaining);
            reductionBuffIndicator.Refresh(reduction, reductionRemaining);
            if (potionBuffRow.gameObject.activeSelf != visible)
                potionBuffRow.gameObject.SetActive(visible);
        }

        private void CreatePotionBuffRow()
        {
            var panel = transform.Find("QuickSlots/ConsumablePanel") as RectTransform;
            if (panel == null) return;

            var row = new GameObject("Active Potion Buffs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.layer = panel.gameObject.layer;
            potionBuffRow = (RectTransform)row.transform;
            potionBuffRow.SetParent(panel, false);
            potionBuffRow.anchorMin = potionBuffRow.anchorMax = new Vector2(0f, 1f);
            potionBuffRow.pivot = Vector2.zero;
            potionBuffRow.anchoredPosition = new Vector2(0f, 8f);
            potionBuffRow.sizeDelta = new Vector2(104f, 68f);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var firstSlot = consumableSlots != null && consumableSlots.Length > 0 ? consumableSlots[0] : null;
            var slotImage = firstSlot != null ? firstSlot.GetComponent<Image>() : null;
            Sprite frame = slotImage != null ? slotImage.sprite : null;
            var slotFont = firstSlot != null ? firstSlot.GetComponentInChildren<TMP_Text>(true)?.font : null;
            TMP_FontAsset font = slotFont != null ? slotFont : TMP_Settings.defaultFontAsset;
            attackBuffIndicator = CreatePotionBuffIndicator("Attack Power Buff", frame, font);
            reductionBuffIndicator = CreatePotionBuffIndicator("Damage Reduction Buff", frame, font);
        }

        private PotionBuffIndicator CreatePotionBuffIndicator(string name, Sprite frame, TMP_FontAsset font)
        {
            var background = CreateBuffImage(name, potionBuffRow);
            background.rectTransform.sizeDelta = new Vector2(48f, 68f);
            background.color = new Color(.025f, .032f, .038f, .94f);

            var artwork = CreateBuffImage("Icon", background.rectTransform);
            artwork.rectTransform.anchorMin = artwork.rectTransform.anchorMax = new Vector2(.5f, 1f);
            artwork.rectTransform.anchoredPosition = new Vector2(0f, -24f);
            artwork.rectTransform.sizeDelta = new Vector2(38f, 38f);
            artwork.preserveAspect = true;

            if (frame != null)
            {
                var rim = CreateBuffImage("Frame", background.rectTransform);
                rim.rectTransform.anchorMin = rim.rectTransform.anchorMax = new Vector2(.5f, 1f);
                rim.rectTransform.anchoredPosition = new Vector2(0f, -24f);
                rim.rectTransform.sizeDelta = new Vector2(48f, 48f);
                rim.sprite = frame;
                // The HUD slot artwork has an opaque center, so the rim sits behind the icon.
                rim.transform.SetAsFirstSibling();
            }

            var textObject = new GameObject("Remaining Time", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.layer = background.gameObject.layer;
            var timer = textObject.GetComponent<TextMeshProUGUI>();
            timer.rectTransform.SetParent(background.rectTransform, false);
            timer.rectTransform.anchorMin = timer.rectTransform.anchorMax = new Vector2(.5f, 0f);
            timer.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            timer.rectTransform.sizeDelta = new Vector2(48f, 20f);
            timer.font = font;
            timer.fontSize = 17f;
            timer.alignment = TextAlignmentOptions.Center;
            timer.textWrappingMode = TextWrappingModes.NoWrap;
            timer.raycastTarget = false;
            return new PotionBuffIndicator(background.gameObject, artwork, timer);
        }

        private static Image CreateBuffImage(string name, RectTransform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private sealed class PotionBuffIndicator
        {
            private static readonly Color TimerColor = new Color(1f, .94f, .77f);
            private static readonly Color ExpiringColor = new Color(1f, .60f, .26f);
            private readonly GameObject root;
            private readonly Image icon;
            private readonly TMP_Text timer;
            private int displayedSeconds = -1;

            public PotionBuffIndicator(GameObject root, Image icon, TMP_Text timer)
            {
                this.root = root;
                this.icon = icon;
                this.timer = timer;
            }

            public void Refresh(ItemDefinition item, float remaining)
            {
                bool visible = item != null && remaining > 0f;
                if (root.activeSelf != visible) root.SetActive(visible);
                if (!visible) return;

                Sprite sprite = item.Icon;
                if (icon.sprite != sprite)
                    icon.sprite = sprite;
                if (icon.enabled != (sprite != null))
                    icon.enabled = sprite != null;
                int seconds = Mathf.CeilToInt(remaining);
                if (seconds == displayedSeconds) return;

                displayedSeconds = seconds;
                timer.text = $"{seconds / 60}:{seconds % 60:00}";
                timer.color = seconds <= 5 ? ExpiringColor : TimerColor;
            }
        }
    }
}

