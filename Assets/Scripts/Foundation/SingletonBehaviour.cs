using UnityEngine;

namespace Game.Foundation
{
    /// <summary>
    /// 受限单例基类。
    /// 只负责唯一实例约束，不负责自动创建、常驻切场景或隐式查找依赖。
    /// </summary>
    /// <typeparam name="T">具体单例组件类型。</typeparam>
    public class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"[Singleton] 场景中存在多个 {typeof(T).Name} 实例，正在销毁多余的物体：{gameObject.name}");
                Destroy(gameObject);
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
}
