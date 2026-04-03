using UnityEngine;

namespace Game.Foundation.Pooling
{
    /// <summary>
    /// 池化对象的可选行为基类。
    /// 说明：
    /// 1. 不是所有池化对象都必须继承它。
    /// 2. 继承它的对象可以接收 OnSpawned / OnDespawned 生命周期回调。
    /// 3. 它只负责池归属和生命周期钩子，不承载具体业务逻辑。
    /// </summary>
    public abstract class PooledMonoBehaviour : MonoBehaviour
    {
        /// <summary>
        /// 当前所属对象池。
        /// 只有通过对象池创建出来的实例才会绑定该引用。
        /// </summary>
        internal GameObjectPool OwnerPool { get; private set; }

        /// <summary>
        /// 由对象池在创建实例时绑定池归属。
        /// 该绑定是内部协议，业务层不应主动改写。
        /// </summary>
        internal void BindOwnerPool(GameObjectPool _ownerPool)
        {
            OwnerPool = _ownerPool;
        }

        /// <summary>
        /// 对象被借出后调用。
        /// 子类可以在这里重置短生命周期状态。
        /// </summary>
        public virtual void OnSpawned()
        {
        }

        /// <summary>
        /// 对象被归还前调用。
        /// 子类可以在这里停止特效、清空缓存、解绑临时引用。
        /// </summary>
        public virtual void OnDespawned()
        {
        }

        /// <summary>
        /// 主动归还自身。
        /// 当对象来源于池时会回池；否则会直接销毁，避免生成孤儿对象。
        /// </summary>
        public void DespawnSelf()
        {
            if (OwnerPool != null)
            {
                OwnerPool.Despawn(this);
                return;
            }

            Destroy(gameObject);
        }
    }
}
