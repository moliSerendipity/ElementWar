using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Foundation.Pooling
{
    /// <summary>
    /// 对象池服务。
    /// 负责池的注册、构建与借还入口，不参与任何玩法规则裁决。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameObjectPoolService : SingletonBehaviour<GameObjectPoolService>
    {
        [Serializable]
        private struct PoolPreset
        {
            [SerializeField] private string poolKey;
            [SerializeField] private GameObject prefab;
            [SerializeField, Min(0)] private int prewarmCount;
            [SerializeField, Min(0)] private int maxPoolSize;
            [SerializeField] private bool allowExpand;
            [SerializeField, Min(1)] private int growthStep;

            public string PoolKey => poolKey;
            public GameObject Prefab => prefab;
            public int PrewarmCount => prewarmCount;
            public int MaxPoolSize => maxPoolSize;
            public bool AllowExpand => allowExpand;
            public int GrowthStep => growthStep;
        }

        [SerializeField] private PoolPreset[] poolPresets = Array.Empty<PoolPreset>();

        private readonly Dictionary<string, GameObjectPool> poolsByKey = new();
        private readonly Dictionary<int, GameObjectPool> poolsByInstanceId = new();

        protected override void Awake()
        {
            base.Awake();

            BuildPools();
        }

        /// <summary>
        /// 根据 Inspector 预设构建所有对象池。
        /// </summary>
        private void BuildPools()
        {
            poolsByKey.Clear();
            poolsByInstanceId.Clear();

            for (int i = 0; i < poolPresets.Length; i++)
            {
                PoolPreset preset = poolPresets[i];
                if (string.IsNullOrWhiteSpace(preset.PoolKey))
                {
                    continue;
                }

                if (preset.Prefab == null)
                {
                    Debug.LogWarning($"[{nameof(GameObjectPoolService)}] PoolPreset {preset.PoolKey} 缺少 Prefab，已跳过。");
                    continue;
                }

                if (poolsByKey.ContainsKey(preset.PoolKey))
                {
                    Debug.LogWarning($"[{nameof(GameObjectPoolService)}] 检测到重复的 PoolKey：{preset.PoolKey}，后续条目已忽略。");
                    continue;
                }

                GameObject poolRootObject = new GameObject($"{preset.PoolKey}_Pool");
                poolRootObject.transform.SetParent(transform, false);

                GameObjectPool pool = new GameObjectPool(
                    preset.PoolKey,
                    preset.Prefab,
                    poolRootObject.transform,
                    preset.MaxPoolSize,
                    preset.AllowExpand,
                    preset.GrowthStep);

                poolsByKey.Add(preset.PoolKey, pool);
                pool.Prewarm(preset.PrewarmCount);
            }
        }

        public bool HasPool(string _poolKey)
        {
            return poolsByKey.ContainsKey(_poolKey);
        }

        /// <summary>
        /// 借出对象。
        /// 借出成功后会记录实例到对象池的反查索引，
        /// 这样即使对象没有挂 PooledMonoBehaviour，也能 O(1) 找回所属池。
        /// </summary>
        public PoolHandle Spawn(string _poolKey, Vector3 _position, Quaternion _rotation, Transform _parent = null)
        {
            if (!poolsByKey.TryGetValue(_poolKey, out GameObjectPool pool))
            {
                return default;
            }

            PoolHandle handle = pool.Spawn(_position, _rotation, _parent);
            if (handle.IsValid)
            {
                poolsByInstanceId[handle.GameObject.GetInstanceID()] = pool;
            }

            return handle;
        }

        /// <summary>
        /// 通过句柄归还对象。
        /// </summary>
        public bool Despawn(PoolHandle _handle)
        {
            if (_handle.IsValid == false)
            {
                return false;
            }

            return Despawn(_handle.GameObject);
        }

        /// <summary>
        /// 通过 GameObject 归还对象。
        /// 当实例存在明确归属时优先直接回池；
        /// 只有在索引丢失或对象来源异常时，才退化为线性探测。
        /// </summary>
        public bool Despawn(GameObject _obj)
        {
            if (_obj == null)
            {
                return false;
            }

            int instanceId = _obj.GetInstanceID();
            if (TryDespawnByIndexedPool(instanceId, _obj))
            {
                return true;
            }

            foreach (GameObjectPool pool in poolsByKey.Values)
            {
                if (pool.Despawn(_obj))
                {
                    poolsByInstanceId.Remove(instanceId);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 通过池化行为组件归还对象。
        /// </summary>
        public bool Despawn(PooledMonoBehaviour _pooledMonoBehaviour)
        {
            if (_pooledMonoBehaviour == null)
            {
                return false;
            }

            return Despawn(_pooledMonoBehaviour.gameObject);
        }

        private bool TryDespawnByIndexedPool(int _instanceId, GameObject _obj)
        {
            if (!poolsByInstanceId.TryGetValue(_instanceId, out GameObjectPool pool))
            {
                return false;
            }

            bool success = pool.Despawn(_obj);
            if (success)
            {
                poolsByInstanceId.Remove(_instanceId);
            }

            return success;
        }
    }
}
