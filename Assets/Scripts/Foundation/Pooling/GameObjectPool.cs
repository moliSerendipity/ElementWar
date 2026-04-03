using System.Collections.Generic;
using UnityEngine;

namespace Game.Foundation.Pooling
{
    /// <summary>
    /// 单池实例。
    /// 负责某一种预制体的创建、借出、归还与运行时扩容。
    /// 约束：
    /// 1. 该池以 GameObject 为核心管理对象，而不是绑定某个特定脚本类型。
    /// 2. 如果实例上存在 PooledMonoBehaviour，则会自动触发生命周期回调。
    /// 3. maxPoolSize 代表配置层默认容量；运行时扩容只修改 runtimeCapacity，不污染配置值。
    /// </summary>
    public sealed class GameObjectPool
    {
        private readonly string poolKey;
        private readonly GameObject prefab;
        private readonly Transform poolRoot;
        private readonly Stack<GameObject> availableGameObjects = new();
        private readonly HashSet<GameObject> borrowedGameObjects = new();
        private readonly int configuredMaxPoolSize;
        private readonly bool allowExpand;
        private readonly int growthStep;

        /// <summary>
        /// 当前运行时容量上限。
        /// 0 表示不设上限，池在运行时可以持续创建新对象。
        /// </summary>
        private int runtimeCapacity;

        public GameObjectPool(
            string _poolKey,
            GameObject _prefab,
            Transform _poolRoot,
            int _maxPoolSize,
            bool _allowExpand,
            int _growthStep)
        {
            poolKey = _poolKey;
            prefab = _prefab;
            poolRoot = _poolRoot;
            configuredMaxPoolSize = Mathf.Max(0, _maxPoolSize);
            allowExpand = _allowExpand;
            growthStep = Mathf.Max(1, _growthStep);
            runtimeCapacity = configuredMaxPoolSize;
        }

        public string PoolKey => poolKey;
        public int AvailableCount => availableGameObjects.Count;
        public int BorrowedCount => borrowedGameObjects.Count;
        public int TotalCount => availableGameObjects.Count + borrowedGameObjects.Count;
        public int ConfiguredMaxPoolSize => configuredMaxPoolSize;
        public int RuntimeCapacity => runtimeCapacity;
        public bool AllowExpand => allowExpand;

        /// <summary>
        /// 预热对象池。
        /// 预热只负责提前准备库存，不会因为预热请求而改写运行时容量上限。
        /// </summary>
        public void Prewarm(int _count)
        {
            if (prefab == null || _count <= 0)
            {
                return;
            }

            int targetPrewarmCount = _count;

            // 当运行时容量存在上限时，预热不能突破当前容量。
            if (runtimeCapacity > 0)
            {
                targetPrewarmCount = Mathf.Min(targetPrewarmCount, Mathf.Max(0, runtimeCapacity - TotalCount));
            }

            for (int i = 0; i < targetPrewarmCount; i++)
            {
                GameObject instance = CreateNewInstance();
                ReturnToPool(instance);
            }
        }

        /// <summary>
        /// 借出一个实例。
        /// </summary>
        public PoolHandle Spawn(Vector3 _position, Quaternion _rotation, Transform _parent = null)
        {
            GameObject obj = TakeInstance();
            if (obj == null)
            {
                return default;
            }

            Transform objTransform = obj.transform;
            objTransform.SetParent(_parent, false);
            objTransform.SetPositionAndRotation(_position, _rotation);

            borrowedGameObjects.Add(obj);
            obj.SetActive(true);

            PooledMonoBehaviour pooledBehaviour = GetPooledBehaviour(obj);
            if (pooledBehaviour != null)
            {
                pooledBehaviour.OnSpawned();
            }

            return new PoolHandle(poolKey, obj, pooledBehaviour);
        }

        /// <summary>
        /// 通过 GameObject 回收实例。
        /// 这是池的核心回收入口，其他回收重载都应委托到这里。
        /// </summary>
        public bool Despawn(GameObject _obj)
        {
            if (_obj == null)
            {
                return false;
            }

            if (borrowedGameObjects.Remove(_obj) == false)
            {
                return false;
            }

            PooledMonoBehaviour pooledBehaviour = GetPooledBehaviour(_obj);
            if (pooledBehaviour != null)
            {
                pooledBehaviour.OnDespawned();
            }

            ReturnToPool(_obj);
            return true;
        }

        /// <summary>
        /// 通过池化行为组件回收实例。
        /// </summary>
        public bool Despawn(PooledMonoBehaviour _pooledMonoBehaviour)
        {
            if (_pooledMonoBehaviour == null)
            {
                return false;
            }

            return Despawn(_pooledMonoBehaviour.gameObject);
        }

        /// <summary>
        /// 创建一个新实例，并完成池回调组件的绑定。
        /// </summary>
        private GameObject CreateNewInstance()
        {
            GameObject obj = Object.Instantiate(prefab, poolRoot);
            PooledMonoBehaviour pooledBehaviour = GetPooledBehaviour(obj);
            if (pooledBehaviour != null)
            {
                pooledBehaviour.BindOwnerPool(this);
            }

            obj.SetActive(false);
            return obj;
        }

        /// <summary>
        /// 尝试从实例上获取池化行为组件。
        /// 该组件是可选的：没有它也能正常进池，只是不会收到生命周期回调。
        /// </summary>
        private static PooledMonoBehaviour GetPooledBehaviour(GameObject _obj)
        {
            if (_obj == null)
            {
                return null;
            }

            return _obj.GetComponent<PooledMonoBehaviour>();
        }

        /// <summary>
        /// 将实例放回库存。
        /// </summary>
        private void ReturnToPool(GameObject _obj)
        {
            Transform objTransform = _obj.transform;
            objTransform.SetParent(poolRoot, false);
            _obj.SetActive(false);
            availableGameObjects.Push(_obj);
        }

        /// <summary>
        /// 获取一个可用实例。
        /// 
        /// 流程：
        /// 1. 优先复用库存。
        /// 2. 库存为空时，若当前容量未满则直接创建。
        /// 3. 当前容量已满且允许扩容时，先扩容再创建。
        /// 4. 仍不能创建时返回 null，由上层决定是否降级处理。
        /// </summary>
        private GameObject TakeInstance()
        {
            if (availableGameObjects.Count > 0)
            {
                return availableGameObjects.Pop();
            }

            if (CanCreateWithinCurrentCapacity() || TryExpandRuntimeCapacity())
            {
                return CreateNewInstance();
            }

            return null;
        }

        /// <summary>
        /// 判断当前是否还能在现有容量内创建新实例。
        /// </summary>
        private bool CanCreateWithinCurrentCapacity()
        {
            if (runtimeCapacity == 0)
            {
                return true;
            }

            return TotalCount < runtimeCapacity;
        }

        /// <summary>
        /// 尝试扩展运行时容量。
        /// 只有当 allowExpand 为 true 且当前使用的是有限容量时才会生效。
        /// </summary>
        private bool TryExpandRuntimeCapacity()
        {
            if (allowExpand == false)
            {
                return false;
            }

            if (runtimeCapacity == 0)
            {
                // 0 代表不设上限，此时理论上不会走到扩容分支。
                return true;
            }

            runtimeCapacity += growthStep;
            return true;
        }
    }
}
