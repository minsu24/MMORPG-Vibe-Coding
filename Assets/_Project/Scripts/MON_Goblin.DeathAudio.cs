using UnityEngine;
using UnityEngine.SceneManagement;

public partial class MON_Goblin
{
    [Header("Death disappearance sound")]
    [SerializeField] private AudioClip disappearanceSound;
    [SerializeField, Range(0f, 1f)] private float disappearanceVolume = .3f;
    private bool disappearanceSoundPlayed;

    // Animation event at the first smoke/disappearance frame, not the lethal hit.
    public void PlayGoblinDisappearanceSound()
    {
        if (HP > 0f || disappearanceSoundPlayed) return;
        AudioClip clip = disappearanceSound != null ? disappearanceSound
            : Resources.Load<AudioClip>("Audio/GoblinDeath");
        if (clip == null) return;
        disappearanceSoundPlayed = true;

        // The corpse is removed before this sound's tail finishes. Keep a separate
        // scene-owned source so corpse cleanup cannot truncate the sound.
        var root = new GameObject("Goblin Disappearance Audio", typeof(AudioSource));
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        root.transform.position = transform.position;
        var source = root.GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.clip = clip;
        source.volume = disappearanceVolume;
        source.Play();
        Destroy(root, clip.length + .1f);
    }
}
