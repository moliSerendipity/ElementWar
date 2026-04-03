using UnityEngine;

namespace Game.Foundation.Pooling
{
    /// <summary>
    /// 池化借出句柄。
    /// 业务层只持有句柄与实例引用，不关心池内部结构。
    /// </summary>
    public readonly struct PoolHandle
    {
        public PoolHandle(string _poolKey, GameObject _gameObject, PooledMonoBehaviour _pooledMonoBehaviour)
        {
            PoolKey = _poolKey;
            GameObject = _gameObject;
            PooledMonoBehaviour = _pooledMonoBehaviour;
        }

        public string PoolKey { get; }
        public GameObject GameObject { get; }
        public PooledMonoBehaviour PooledMonoBehaviour { get; }

        /// <summary>
        /// 句柄是否仍然指向一个有效借出实例。
        /// </summary>
        public bool IsValid => string.IsNullOrWhiteSpace(PoolKey) == false && GameObject != null;

        public Transform Transform => GameObject != null ? GameObject.transform : null;
    }
}
