using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemyAttackAudio
{
    public static void PlayFor(EnemyController enemy, string resource, float volume = .3f, float startSeconds = 0f)
    {
        if (enemy == null || !enemy.isActiveAndEnabled || !enemy.CanDealContactDamage) return;
        PlayAt(enemy.gameObject, resource, volume, startSeconds);
    }

    public static void PlayAt(GameObject context, string resource, float volume = .3f, float startSeconds = 0f)
    {
        if (context == null) return;
        var clip = Resources.Load<AudioClip>(resource);
        if (clip == null) return;
        var root = new GameObject(context.name + " " + clip.name + " Audio");
        SceneManager.MoveGameObjectToScene(root, context.scene);
        root.transform.position = context.transform.position;
        var source = root.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = Mathf.Clamp01(volume);
        source.clip = clip;
        float start = Mathf.Clamp(startSeconds, 0f, Mathf.Max(0f, clip.length - .01f));
        source.time = start;
        source.Play();
        Object.Destroy(root, clip.length - start + .1f);
    }
}
