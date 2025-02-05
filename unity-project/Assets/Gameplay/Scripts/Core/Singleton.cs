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

            Init(); // Since singleton only init one time
        }
        else
        {
            Destroy(gameObject);  // Destroy the duplicate instance
        }
    }

    public virtual void Init() { }
}
