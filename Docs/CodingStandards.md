# ElementWar C# 代码规范

本文件是项目 C# 命名与注释规则的唯一维护来源。规则适用于新增代码和本次修改触及的代码；不要为统一风格批量改写无关旧文件。

## 命名

| 对象 | 规则 | 示例 |
|---|---|---|
| 命名空间、类、结构体、枚举、委托 | PascalCase | `Game.Gameplay.Combat`、`DamageResolver` |
| 接口 | `I` + PascalCase | `IDamageReceiver` |
| 方法、属性、C# 事件 | PascalCase | `ApplyDamage`、`CurrentSpeed`、`DamageApplied` |
| 枚举成员、常量 | PascalCase | `Physical`、`MaxHitCount` |
| private/protected 字段，包括 `static readonly` | camelCase，不加 `m_`、`s_` | `currentSpeed`、`damageEventType` |
| `[SerializeField] private` 字段 | camelCase | `baseMoveSpeed` |
| 局部变量 | camelCase | `effectiveSpeed` |
| 参数 | `_` + camelCase | `_damageAmount` |
| 泛型参数 | `T` 或 `T` + PascalCase | `T`、`TEvent` |
| 事件数据类型 | PascalCase + `Event` | `DamageAppliedEvent` |
| 事件回调方法 | `On` + 事件名 | `OnDamageApplied` |
| 缓存的事件处理器字段 | camelCase + `Handler` | `onDamageAppliedHandler` |

补充规则：

- 布尔值优先使用 `Is`、`Has`、`Can`、`Should` 等能表达判断的问题式名称。
- 集合使用复数名；映射优先使用 `valuesByKey` 结构，例如 `handlersByEventType`。
- `Try` 模式使用 `TryXxx`；异步方法使用 `Async` 后缀。
- 禁止匈牙利命名和类型缩写前缀。允许领域内稳定缩写，例如 `Id`、`UI`、`XPBD`。
- 避免 public 字段；需要 Inspector 配置时使用 `[SerializeField] private`，需要外部读取时再提供 PascalCase 属性。
- 重命名序列化字段时评估并按需使用 `[FormerlySerializedAs]`，避免破坏已有 scene、prefab 和 ScriptableObject 数据。

## XML 文档注释

XML 文档注释使用正确的 `<summary>` 标签。参数说明写在独立的 `<param>` 标签中，不塞进 `<summary>`。

必须编写 XML 文档注释：

- 项目自有的类、结构体、接口、枚举和委托。
- public/protected 方法、属性、构造函数和事件。
- 含领域规则、生命周期时序、事件订阅、公式、单位/坐标空间或非显然副作用的 internal/private 方法。

可以省略：

- 名称和实现都直白的 private 简短的小工具方法，稍微长一点的方法也要注释。
- 只转调一个明确方法的 Unity 消息函数；若调用顺序、启停时机或对象池复用会影响行为，仍须注释。

标签要求：

- `<summary>`：说明职责，以及必要的时机、状态变化、副作用、单位或范围；不要只把名称改写成一句话。
- `<param>`：每个参数各写一项，名称必须与代码一致，并说明含义、单位或约束。
- `<returns>`：有返回值时说明结果语义；`bool` 尤其要说明 `true` 代表什么。
- `<exception>`：仅记录调用方需要处理且确实可能抛出的异常。
- 事件注释：说明在什么事实发生后发布，以及载荷表达的是请求、结果还是已提交状态。

```csharp
/// <summary>
/// 尝试消耗指定数量的弹药；库存不足时保持状态不变。
/// </summary>
/// <param name="_amount">本次消耗数量，必须大于零。</param>
/// <returns>成功扣除弹药时返回 <see langword="true"/>。</returns>
private bool TryConsumeAmmo(int _amount)
{
    // 实现省略。
}
```

## 字段、属性与 Inspector 提示

- public/protected 属性按公共契约写 XML 文档注释。
- private 字段默认不写 XML 文档注释；仅在名称无法表达来源、单位、不变量或所有权时补充简短说明。
- 面向策划或美术的序列化字段，在含义、单位或效果不直观时使用 `[Tooltip]`；可机械表达的范围使用 `[Min]`、`[Range]` 等属性。
- 常量和 `static readonly` 字段只有在数值来源、选择理由或不变量不明显时注释。

## 实现注释

- 注释解释“为什么”和约束，不逐行翻译“做了什么”。
- 公式注明目的、变量含义、单位、坐标空间以及关键边界；有外部依据时记录来源。
- 为性能、兼容性、序列化或生命周期采取的非显然做法，要写清保留原因。
- 行为变化时在同一修改中同步更新相关注释；过期注释按缺陷处理。
- 项目注释使用简洁中文，代码标识符和 API 名保持英文。

## 工具边界

根目录 `.editorconfig` 检查可机械表达的命名与基础格式。注释是否必要、是否准确仍由代码审查确认；不要仅因构建通过就认定符合本规范。
