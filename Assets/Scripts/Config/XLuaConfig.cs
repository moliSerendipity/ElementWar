using System;
using System.Collections.Generic;
using XLua;

/// <summary>
/// XLua 全局生成配置表
/// 职责：告诉 XLua 需要为哪些系统泛型或第三方类生成桥接代码
/// </summary>
public static class XLuaConfig
{
    //[CSharpCallLua]：告诉 XLua，C# 需要调用这些类型的 Lua 函数
    // 主要是给各种 Action, Func 委托生成桥接代码
    [CSharpCallLua]
    public static List<Type> CSharpCallLuaList = new List<Type>()
    {
        typeof(Action),
        typeof(Action<int>),
        typeof(Action<float>),
        typeof(Action<string>),
        typeof(Action<bool>),
        typeof(LuaManager.OnHitMessageDelegate)
        
        // 如果以后有带返回值的委托，加在这里，例如：
    };

    //[LuaCallCSharp]：如果你用到了一些没有源码的 DLL 类库，也可以在这里集中配置
    // [LuaCallCSharp]
    // public static List<Type> LuaCallCSharpList = new List<Type>()
    // {
    //     typeof(UnityEngine.GameObject),
    // };
}
