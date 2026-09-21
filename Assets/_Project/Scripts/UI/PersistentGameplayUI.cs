using UnityEngine;

public sealed class PersistentGameplayUI : MonoBehaviour
{
    private static PersistentGameplayUI instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}