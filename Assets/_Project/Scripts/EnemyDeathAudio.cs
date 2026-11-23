using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class EnemyDeathAudio : MonoBehaviour
{
    private bool played;

    public static void PlayFor(EnemyController enemy, string resource, float volume = .3f)
    {
        if (enemy == null || enemy.HP > 0f) return;
        var feedback = enemy.GetComponent<EnemyDeathAudio>();
        if (feedback == null) feedback = enemy.gameObject.AddComponent<EnemyDeathAudio>();
        if (feedback.played) return;
        var clip = Resources.Load<AudioClip>(resource);
        if (clip == null) return;
        feedback.played = true;
        if (LocalAmbientAudio2D.GetPlayerVolumeAt(enemy.transform.position, volume) <= 0f) return;

        // Scene-owned audio survives the corpse's animation/destruction, but
        // stops naturally when the player leaves the map.
        var root = new GameObject(enemy.GetType().Name + " Death Audio", typeof(AudioSource));
        SceneManager.MoveGameObjectToScene(root, enemy.gameObject.scene);
        root.transform.position = enemy.transform.position;
        var source = root.GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        var distanceAudio = root.AddComponent<LocalAmbientAudio2D>();
        distanceAudio.PrepareOneShot(volume, 1.5f, 10f);
        source.clip = clip;
        source.Play();
        Destroy(root, clip.length + .1f);
    }
}
