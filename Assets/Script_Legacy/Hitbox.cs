using UnityEngine;

// 受击判定盒 (挂在怪物的各个骨骼 Collider 上)
public class Hitbox : MonoBehaviour
{
    [Tooltip("指向该怪物根节点的实体类")]
    public EnemyEntity owner;
    [Tooltip("部位伤害倍率 (头部填 2.0，四肢填 0.5)")]
    public float damageMultiplier = 1.0f;
}