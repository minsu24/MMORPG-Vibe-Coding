using EasternFantasy.Inventory;
using UnityEngine;

namespace EasternFantasy.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerInventory))]
    public sealed class PlayerItemAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip equipSound;
        [SerializeField] private AudioClip potionSound;
        [SerializeField] private AudioClip currencySound;
        [SerializeField] private AudioClip itemSound;
        [SerializeField, Range(0f, 1f)] private float equipVolume = .35f;
        [SerializeField, Range(.5f, 2f)] private float equipPlaybackSpeed = 1.15f;
        [SerializeField, Range(0f, 1f)] private float potionVolume = .3f;
        [SerializeField, Range(0f, 1f)] private float currencyVolume = .08f;
        [SerializeField, Range(0f, 1f)] private float itemVolume = .06f;

        private PlayerInventory inventory;
        private readonly AudioSource[] channels = new AudioSource[4];
        private readonly float[] nextPickupAt = new float[4];

        private void Awake() => inventory = GetComponent<PlayerInventory>();

        private void OnEnable()
        {
            inventory.EquipmentEquipped += OnEquipmentEquipped;
            inventory.ConsumableUsed += OnConsumableUsed;
        }

        private void OnEquipmentEquipped(ItemDefinition item) => Play(0, equipSound, equipVolume);
        private void OnConsumableUsed(ItemDefinition item) => Play(1, potionSound, potionVolume);
        public void PlayCurrencyPickup() => Play(2, currencySound, currencyVolume);
        public void PlayItemPickup() => Play(3, itemSound, itemVolume);

        private void Play(int channel, AudioClip clip, float volume)
        {
            if (!isActiveAndEnabled || clip == null || volume <= 0f) return;
            AudioSource source = channels[channel];
            // Coalesce rapid loot collections rather than stacking notification
            // tails. Pickup sounds never cut off equipment or potion feedback.
            if (channel >= 2 && (Time.unscaledTime < nextPickupAt[channel]
                || (source != null && source.isPlaying))) return;
            if (source == null)
            {
                var child = new GameObject(new[] { "Equip Audio", "Potion Audio", "Coin Pickup Audio", "Item Pickup Audio" }[channel]);
                child.transform.SetParent(transform, false);
                source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                channels[channel] = source;
            }
            source.clip = clip;
            source.volume = volume;
            source.pitch = channel == 0 ? equipPlaybackSpeed : 1f;
            source.Play();
            if (channel >= 2) nextPickupAt[channel] = Time.unscaledTime + .18f;
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.EquipmentEquipped -= OnEquipmentEquipped;
                inventory.ConsumableUsed -= OnConsumableUsed;
            }
            for (int i = 0; i < channels.Length; i++)
            {
                if (channels[i] != null) channels[i].Stop();
                nextPickupAt[i] = float.NegativeInfinity;
            }
        }
    }
}
