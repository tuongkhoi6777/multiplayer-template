using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    public virtual void Awake()
    {
        // Ensure the Singleton pattern is respected
        if (Instance == null)
        {
            Instance = this as T;
            DontDestroyOnLoad(gameObject);  // Keep this instance across scenes

            Init(); // After create instance call init
        }
        else
        {
            Destroy(gameObject);  // Destroy the duplicate instance
        }
    }

    protected virtual void Init() { }
}
