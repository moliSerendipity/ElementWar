using System.Collections;
using System.Collections.Generic;
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
        if (Instance != null)
        {
#if UNITY_EDITOR
            Debug.LogError($"{typeof(T).Name} 不符合单例模式");
#endif
        }
        Instance = this as T;
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
