using UnityEngine;

[DefaultExecutionOrder(-11000)]
public sealed class PersistentGameplayUI : MonoBehaviour
{
    private static PersistentGameplayUI instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // Suppress child Awake methods before their duplicate singleton checks run.
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EasternFantasy.UI.GameplayHudVisibility.Attach(gameObject);
    }
}
