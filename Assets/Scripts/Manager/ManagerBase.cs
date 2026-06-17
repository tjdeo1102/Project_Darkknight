using UnityEngine;

public interface IManager
{
    bool IsReady { get; }
    public void StartInit();
}

public abstract class ManagerBase<T> : MonoBehaviour,IManager where T : MonoBehaviour
{
    public static T Instance;
    public bool DontDestroyOnLoadCheck = false;
    public bool IsReady { get; protected set; } = false;

    public virtual void StartInit() { }

    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this as T;
            if (DontDestroyOnLoadCheck)
                DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
