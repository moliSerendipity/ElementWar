using UnityEngine;

/// <summary>
/// 单例模式限定器
/// </summary>
/// <typeparam name="T">子类</typeparam>
public class SingleMonoBase<T> : MonoBehaviour where T : SingleMonoBase<T>
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError($"[Singleton] 场景中存在多个 {typeof(T).Name} 实例，正在销毁多余的物体！");
            Destroy(this);
            return;
        }
        Instance = this as T;
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
