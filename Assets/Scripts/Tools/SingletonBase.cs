using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                // Look for an existing instance in the scene
                _instance = FindObjectOfType<T>();

                if (_instance == null)
                {
                    Debug.LogError($"Singleton<{typeof(T)}> instance not found in scene.");
                }
            }

            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"Multiple instances of Singleton<{typeof(T)}> detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        _instance = (T)this;
    }
}
