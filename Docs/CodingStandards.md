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

## 校验所有权与运行时边界

- 每项不变量必须只有一个权威校验层：序列化配置的结构、枚举、数量、重复项和引用完整性由 `ConfigBase.Validate` / `ConfigValidationRunner` 在启动阶段负责；一次请求的来源、目标、身份、时间和参数由 Gameplay 入口负责；版本、生命周期、间隔、去重和其他可变状态由权威 Runtime 负责。
- `TryGetXxx`、只读查询和每帧/每次命中的热路径默认信任已经通过上游校验的不变量，不得再次扫描整张配置表或重复执行同一组结构校验。可以保留避免本方法自身空引用、越界或状态竞争所需的最小保护。
- 只有当条件可能在上游校验后发生变化，或调用入口明确接受未受信任数据时，才允许跨层重复防御；代码注释必须说明新的失败来源，并由对应测试证明。
- 添加校验前必须回答：它校验谁拥有的数据、上一层是否已经保证、删除后合法调用路径会出现什么具体错误。第三项没有明确答案时，不增加该判断。
- 已校验配置值不得在热路径重复调用 `Enum.IsDefined`、完整 `Validate` 或等价的 `HasValidXxx` 扫描；固定小规模表优先直接遍历，没有性能或规模证据时不增加缓存、字典或平行状态。

## Component 职责与规模审查

- 不存在适用于所有 Component 的默认模板。不得因为另一个组件含有请求校验、生命周期绑定、事件、缓存或执行集合，就复制同样的结构；先根据本任务的状态、入口、消费者和失败方式判断。
- Component 优先只拥有与自身可变事实直接相关的状态转换。身份/阵营、一次操作对多个组件的协调、跨组件等级策略或事务结果，若由更外层入口统一处理更清晰，就不应在每个状态组件内重复。
- `ValidateRequest`、`BeginLifecycle` / `EndLifecycle` 等方法名不是禁用项；加入前必须说明该组件实际拥有哪项不变量或生命周期、谁调用、删除后会出现什么当前缺陷。只有“以后可能需要”或“其他组件也是这样”时不增加。
- 简单状态组件出现多份执行集合、平行 TargetId、调用方时间线、同步事件发布或大量 Request/Result 包装时，必须复核是否把入口策略误塞进状态所有者。需要协调多个状态时，优先使用按具体操作命名、无状态或最小持态的边界，避免含糊的总控类型。
- 代码行数和方法数不是硬性上限，但规模明显超过领域状态复杂度时必须逐项审查额外字段、分支和公共类型；无法对应当前验收、真实消费者或明确迁移成本的内容删除或推迟。

## 函数体内的逻辑块注释

- 多阶段方法，以及包含非显然校验顺序、状态转换、生命周期、公式、事务写回或事件发布的方法，必须在函数体内按逻辑块添加简洁中文注释，不能只写 XML `<summary>` 后留下大段无解释实现。
- 一条注释可以覆盖紧随其后的若干语句和分支。注释应放在逻辑块之前，并说明目的、顺序原因、依赖的不变量或失败时为何保持状态不变。
- 常见需要注释的逻辑块包括：无副作用预检、时间/生命周期同步、接收资格、数值派生与边界保护、重复/间隔裁决、原子状态提交和已提交事实发布。
- 不要求逐句翻译代码。简单赋值、直白 getter、单一转调、名称已经完整表达意图的短分支和机械枚举循环可以不写块注释。
- 连续代码虽然语法简单，但如果前后顺序会影响结果或读者需要反推领域规则，仍应拆成逻辑块并解释；不得用“代码本身能看懂”省略关键意图。

```csharp
// 旧请求不能推进或清理对象复用后的新生命周期，因此必须在时间同步前拦截。
if (MatchesCurrentTarget(_request) == false)
{
    return ElementApplicationResult.Rejected(
        ElementApplicationRejectionReason.InvalidTarget,
        primaryAttachment);
}

// 在读取当前槽前同步到请求时间，保证过期、死亡或重置状态已经被清理。
ElementApplicationRejectionReason rejectionReason;
if (TryAdvanceTime(_request.ApplicationTime, out rejectionReason) == false)
{
    return ElementApplicationResult.Rejected(
        rejectionReason,
        primaryAttachment);
}
```

## 实现注释

- 注释解释“为什么”和约束，不逐行翻译“做了什么”。
- 公式注明目的、变量含义、单位、坐标空间以及关键边界；有外部依据时记录来源。
- 为性能、兼容性、序列化或生命周期采取的非显然做法，要写清保留原因。
- 行为变化时在同一修改中同步更新相关注释；过期注释按缺陷处理。
- 项目注释使用简洁中文，代码标识符和 API 名保持英文。

## 工具边界

根目录 `.editorconfig` 检查可机械表达的命名与基础格式。注释是否必要、是否准确仍由代码审查确认；不要仅因构建通过就认定符合本规范。
