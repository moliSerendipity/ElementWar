using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 池对象标记组件（自动挂载，用于记录身世）
/// </summary>
public class PoolItem : MonoBehaviour
{
    [HideInInspector]
    public int prefabID;
}

public class GameObjectPool : SingleMonoBase<GameObjectPool>
{
    // 字典结构：Key 是预制体的 InstanceID，Value 是存放闲置对象的队列
    private Dictionary<int, Queue<GameObject>> pool = new Dictionary<int, Queue<GameObject>>();

    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// 从池中获取对象
    /// </summary>
    public GameObject Get(GameObject _prefab, Vector3 _position, Quaternion _rotation)
    {
        if (_prefab == null)
            return null;

        int prefabID = _prefab.GetInstanceID();
        // 如果池子里有这个预制体的队列，且队列不为空
        if (pool.ContainsKey(prefabID) && pool[prefabID].Count > 0)
        {
            GameObject obj = pool[prefabID].Dequeue();
            obj.transform.position = _position;
            obj.transform.rotation = _rotation;
            obj.SetActive(true);
            return obj;
        }

        // 如果池子里没有，直接实例化一个新的
        GameObject newObj = Instantiate(_prefab, _position, _rotation);
        // 挂载一个标记脚本，记住它属于哪个池子（方便回收）
        PoolItem item = newObj.AddComponent<PoolItem>();
        item.prefabID = prefabID;
        return newObj;
    }

    /// <summary>
    /// 回收对象到池中
    /// </summary>
    public void Release(GameObject _obj)
    {
        if (_obj == null)
            return;

        PoolItem item = _obj.GetComponent<PoolItem>();
        if (item != null)
        {
            _obj.SetActive(false);

            if (!pool.ContainsKey(item.prefabID))
            {
                pool[item.prefabID] = new Queue<GameObject>();
            }
            pool[item.prefabID].Enqueue(_obj);
        }
        else
        {
            // 如果没有标记，说明不是从池子里出来的，直接销毁
            Destroy(_obj);
        }
    }
}
