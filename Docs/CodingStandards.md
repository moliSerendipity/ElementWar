# ElementWar C# 代码规范

本文件只维护 C# 命名和注释规范。架构、状态所有权、防御边界和组件复杂度由根 `AGENTS.md` 与项目 Skill 维护；只有 Full 再读取 Workflow，避免每次 C# 修改重复加载架构规则。

## 命名

| 对象                                           | 规则                       | 示例                                           |
| ---------------------------------------------- | -------------------------- | ---------------------------------------------- |
| 命名空间、类、结构体、枚举、委托               | PascalCase                 | `Game.Gameplay.Combat`、`DamageResolver`       |
| 接口                                           | `I` + PascalCase           | `IDamageReceiver`                              |
| 方法、属性、C# 事件                            | PascalCase                 | `ApplyDamage`、`CurrentSpeed`、`DamageApplied` |
| 枚举成员、常量                                 | PascalCase                 | `Physical`、`MaxHitCount`                      |
| private/protected 字段，包括 `static readonly` | camelCase，不加 `m_`、`s_` | `currentSpeed`、`damageEventType`              |
| `[SerializeField] private` 字段                | camelCase                  | `baseMoveSpeed`                                |
| 局部变量                                       | camelCase                  | `effectiveSpeed`                               |
| 参数                                           | `_` + camelCase            | `_damageAmount`                                |
| 泛型参数                                       | `T` 或 `T` + PascalCase    | `T`、`TEvent`                                  |
| 事件数据类型                                   | PascalCase + `Event`       | `DamageAppliedEvent`                           |
| 事件回调方法                                   | `On` + 事件名              | `OnDamageApplied`                              |
| 缓存的事件处理器字段                           | camelCase + `Handler`      | `onDamageAppliedHandler`                       |

- 布尔值优先 `Is`、`Has`、`Can`、`Should`；集合使用复数名；映射优先 `valuesByKey`。
- `Try` 模式使用 `TryXxx`；异步方法使用 `Async`。
- 禁止匈牙利命名和类型缩写前缀；允许领域稳定缩写，如 `Id`、`UI`、`XPBD`。
- 避免 public 字段；Inspector 配置使用 `[SerializeField] private`，外部需要读取时再提供属性。
- 重命名序列化字段时评估 `[FormerlySerializedAs]`，避免破坏 scene、prefab 和 ScriptableObject 数据。

## XML 文档注释

代码注释保持详细，不因 token 优化而降低要求。

必须编写 XML 文档注释：

- 项目自有的类、结构体、接口、枚举和委托；
- public/protected 方法、属性、构造函数和事件；
- 含领域规则、生命周期时序、事件订阅、公式、单位/坐标空间或非显然副作用的 internal/private 方法。

可以省略：

- 名称和实现都直白的 private 简短工具方法；
- 只转调一个明确方法的 Unity 消息函数；若当前真实调用顺序或生命周期会影响行为，仍须注释。

标签要求：

- `<summary>`：说明职责，以及必要的时机、状态变化、副作用、单位或范围；不要只改写名称。
- `<param>`：每个参数单独说明含义、单位或约束，名称与代码一致。
- `<returns>`：说明返回结果语义；`bool` 必须明确 `true` 代表什么。
- `<exception>`：仅记录调用方确实需要处理且确实可能抛出的异常。
- 事件注释：说明何时发布，以及载荷是请求、结果还是已提交事实。

```
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

- public/protected 属性按公共契约写 XML 注释。
- private 字段默认不写 XML；名称无法表达来源、单位、不变量或所有权时补充简短说明。
- 面向策划/美术的序列化字段，在含义、单位或效果不直观时使用 `[Tooltip]`；机械范围用 `[Min]`、`[Range]`。
- 常量和 `static readonly` 只在数值来源、选择理由或不变量不明显时注释。

## 函数体逻辑块注释

- 多阶段方法，以及包含非显然校验顺序、状态转换、生命周期、公式、事务写回或事件发布的方法，必须按逻辑块添加简洁中文注释。
- 注释放在逻辑块之前，解释目的、顺序原因、依赖的不变量，或失败时为何保持状态不变；一条注释可以覆盖若干语句和分支。
- 常见需要解释的内容：公式来源、单位/坐标空间、状态转换顺序、权威状态写回、事件发布时机，以及当前真实生命周期或性能取舍。
- 不逐句翻译代码。简单赋值、直白 getter、单一转调、名称已完整表达意图的短分支和机械枚举循环可以不写块注释。
- 语法虽简单但顺序会影响结果、或读者需要反推领域规则时，仍应拆成逻辑块说明。

```
// 先按命中部位得到本次攻击自身的确定倍率，再叠加目标侧减伤。
// 顺序写在这里是为了让所有伤害入口共享同一公式，而不是由各武器重复组合。
float hitDamage = baseDamage * hitPartMultiplier;
float finalDamage = hitDamage * defenseMultiplier * resistanceMultiplier;

// Health 是生命值唯一事实源，因此只在最终伤害确定后提交一次；
// Presentation 通过已提交结果更新表现，不参与重新计算。
healthComponent.ApplyDamage(finalDamage);
```

## 实现注释

- 注释解释“为什么”和约束，不逐行翻译“做了什么”。
- 公式注明目的、变量含义、单位、坐标空间和关键边界；有外部依据时记录来源。
- 为性能、兼容性、序列化或实际生命周期采取的非显然做法，写清保留原因。
- 行为变化时同步更新相关注释；过期注释按缺陷处理。
- 项目注释使用简洁中文，代码标识符和 API 名保持英文。

## 工具边界

根目录 `.editorconfig` 检查可机械表达的命名与基础格式。注释是否必要、是否准确仍由代码审查确认；不要仅因构建通过就认定符合本规范。
