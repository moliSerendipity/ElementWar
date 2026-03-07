using XLua;

// 跨语言通信结构体 (使用 GCOptimize 避免装箱拆箱产生的 GC)
[GCOptimize]
[LuaCallCSharp]
public struct HitMessage
{
    public int instigatorID;                            // 攻击者 ID
    public int targetUID;                               // 被击中的怪物 UID
    public int targetConfigID;                          // 怪物在配置表里的模板 ID
    public int ammoID;                                  // 子弹的配置 ID (Lua 需要靠它查元素类型和伤害倍率)
    public float hitMultiplier;                         // 命中部位倍率 (爆头 = 2.0，身体 = 1.0)
}