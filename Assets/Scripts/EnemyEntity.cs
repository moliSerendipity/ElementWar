using UnityEngine;

// 怪物实体根节点 (挂在怪物的最顶层)
public class EnemyEntity : MonoBehaviour
{
    [HideInInspector]
    public int uid;
    [Header("怪物的配置表ID")]
    public int configID = 81001;

    private void Awake()
    {
        // 自动分配一个 UID
        uid = gameObject.GetInstanceID();
    }
}
