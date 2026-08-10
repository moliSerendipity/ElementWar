# 路线 04：集成、验证与发布

- 上级路线：[`DevelopmentRoadmap.md`](../DevelopmentRoadmap.md)
- 架构与旧路径：[`Architecture.md`](../Architecture.md)
- 工作流与证据：[`Workflow.md`](../Workflow.md)
- 维护日期：2026-08-10

本路线包含贯穿主线的门禁任务。许可、组合根和验证不能全部拖到发布前；Legacy/Lua、旧场景与空配置壳也不能在没有消费者和迁移证据时顺手清理。

## 已完成验证基线

### VER-001 EditMode 自动化基线

- 状态：Done（Closed / Fast Verified）
- 已完成：真实项目测试、非零测试门禁、PID/时间新鲜度、XML/日志/摘要证据。
- 证据：[`EditModeAutomationBaseline.md`](../Features/EditModeAutomationBaseline.md)。
- 边界：只证明对应 EditMode 切片，不证明 PlayMode、Player 或人工验收。
- 解锁：后续任务的 Fast Verified 基础入口。

### VER-002 PlayMode 自动化基线

- 状态：Done（Closed；不等于 Full Verified）
- 已完成：真实帧循环测试、生命周期清理、PID/时间和精确测试身份门禁。
- 证据：[`PlayModeAutomationBaseline.md`](../Features/PlayModeAutomationBaseline.md)。
- 边界：测试范围仍小，不能替代玩法集成或主线场景人工验收。
- 解锁：后续生产链 PlayMode 证据。

### VER-003 Bootstrap-only Windows64 自动化基线

- 状态：Done（Closed；Bootstrap-only）
- 已完成：可信起点约束、Windows64 构建、Player 文件/架构校验、副作用恢复和原始证据。
- 证据：[`Windows64AutomationBaseline.md`](../Features/Windows64AutomationBaseline.md)。
- 边界：入口仍绑定旧可信提交/严格工作树条件，只构建 Bootstrap，未启动 Player，也未证明整个项目 Full Verified。
- 解锁：`VER-010`、`BUILD-010`。

## P0 早期门禁

### LIC-010 第三方资产来源、许可与分发清单

- 状态：Ready
- 依赖：无。
- 当前缺口：仓库没有统一 `ThirdPartyAssets.md`；第二角色、动画、音频、VFX、场景资产和插件在公开作品集前无法逐项证明来源与许可。
- 实施要点：① 扫描实际被主线引用和计划采用的第三方资产；② 记录名称、作者/发布者、来源链接、版本/获取日期、许可证、署名要求、可否再分发原文件；③ 将“未知/仅个人使用/禁止再分发”标为阻断；④ 资产替换保留 GUID/引用迁移计划；⑤ 发布包只含获准内容和必要署名。
- 可观察完成：每项主线第三方资产都有可审计条目，未知许可资产不会进入第二角色或发布候选。
- 范围：文档与只读资产引用审计；下载、替换、删除资产需另行授权。
- 验证：清单到实际 GUID/prefab/scene 的抽样或全量映射，许可证原始证据路径。
- 解锁：`CHR-020`、`LVL-010`、`REL-010`。

### VER-010 当前 HEAD 验证策略与统一入口

- 状态：Ready
- 依赖：`VER-001`、`VER-002`、`VER-003`。
- 当前缺口：EditMode、PlayMode、Windows64 三个脚本入口重复；阶段 2 统一脚本尚未接入；Windows 基线的可信起点与当前开发 HEAD 不再等同。
- 实施要点：① 提取不改变安全门禁的共享只读解析；② 新增实际存在的统一编排入口，支持 Fast/Full 选择但保留子脚本；③ 每个阶段单独结果与总摘要，零测试/缺 XML/非零退出均失败；④ 定义“已批准脏工作树”与当前 HEAD 的证据绑定，不削弱副作用恢复；⑤ 更新 verification matrix 与 Feature Spec。
- 可观察完成：一次命令可运行批准的验证组合并准确报告每阶段；任一失败不被后续成功覆盖。
- 范围：Tools/Editor build/测试文档；高风险脚本，需独立审查。非目标是 CI 或跨平台。
- 验证：PowerShell AST、离线失败探针、真实 EditMode/PlayMode；Windows64 仅在关闭 Editor 且满足安全前提时运行。
- 解锁：所有后续任务更低成本的 Full Verified、`BUILD-010`。

### ARC-010 Bootstrap 组合根与运行时服务生命周期契约

- 状态：Planned
- 依赖：`WPN-040`。
- 当前缺口：未来 Party、Run、技能、敌人和 UI 都需要显式所有者；若继续增加静态 `Active`/`Instance` 或标签查找，会形成平行状态和测试污染。
- 实施要点：① 审计 Bootstrap 创建顺序、`GameServices`/单例/场景引用和销毁；② 画出服务生命周期与依赖；③ 用一个场景组合根显式装配长期服务和每 Run 服务；④ 构造函数/Inspector/明确注册传递依赖，静态入口只保留经批准的兼容门面；⑤ 重载、退出 PlayMode 和测试 teardown 对称释放。
- 可观察完成：Party/Run 等新所有者有唯一创建和销毁路径，测试可显式装配，场景重载不留下旧实例。
- 范围：场景组合根、公共生命周期与可能的 asmdef；需要 ADR 和序列化引用迁移。非目标是一次移除全部旧静态 API。
- 验证：依赖图、生命周期 EditMode/PlayMode、Bootstrap 引用和重载测试、独立审查。
- 解锁：`PTY-010`、`RUN-010`、`ARC-020`。

### ARC-020 按消费者收敛全局 Active/Instance 与查找

- 状态：Planned
- 依赖：`ARC-010`；随各消费者任务分批完成。
- 当前缺口：现有全局入口便于单角色原型，但会隐藏所有权、创建测试顺序依赖，并在多角色/多 Run 中返回错误实例。
- 实施要点：列出调用点并按 Foundation 设施、场景唯一服务、角色/Run 状态分类；先迁移 Party/Run/武器/敌人新消费者；每批保留特征测试和兼容门面；确认无引用后再提出删除；禁止仓库级一次性替换。
- 可观察完成：主线 Gameplay 不通过全局搜索猜测活动角色/武器/Run，兼容入口的剩余调用有清单和删除条件。
- 范围：嵌入对应功能 Feature Spec；公共 API 删除必须单独迁移授权。
- 验证：调用点扫描、显式装配测试、scene 重载 PlayMode。
- 解锁：`LEG-010`、发布前架构收口。

## P1 真实资产、配置与旧架构迁移

### VER-020 真实 scene/prefab/config 资产验证

- 状态：Planned
- 依赖：`ARC-010`、`ELM-040`、`PTY-030`。
- 当前缺口：很多测试可动态装配对象，不能证明 Bootstrap、角色/敌人 prefab、配置 Registry、Input Action 和 Addressables 的真实引用有效。
- 实施要点：① 为主线场景和关键 prefab 建立加载测试；② 校验 Missing Script、空必需引用、重复稳定 ID、空反应表和配置壳；③ 从真实资产运行至少一个战斗闭环；④ 序列化迁移前后保存 GUID/引用报告；⑤ 动态替身证据与真实资产证据分开声明。
- 可观察完成：关键主线资产能在干净导入后加载并执行最小行为，引用缺失在自动化阶段失败。
- 范围：测试/Editor 校验优先；修复发现的资产缺陷按所属功能授权。
- 验证：EditMode 资产检查、PlayMode 场景加载、日志/XML。
- 解锁：`RUN-090`、`BUILD-010`。

### CFG-010 Definition 配置壳与 Registry 收敛

- 状态：Planned
- 依赖：`ELM-100`、`SKL-060`、`RUN-030`、`UPG-010`。
- 当前缺口：ElementReaction、Skill、Buff、AreaEffect、Stage 等定义存在无消费者或字段不足的壳，Registry 中部分集合为空；继续提前扩展会形成平行 schema。
- 实施要点：① 逐类型列出真实消费者、资产和字段使用；② 已由新契约采用的配置迁移并校验；③ 无消费者且无批准用途的壳提出删除/冻结；④ 合并重复 ID/Tag/Enum 事实源；⑤ 引用检查后以可恢复迁移提交处理，不手改大批 YAML。
- 可观察完成：主线加载的每类配置都有生产消费者与校验，无“看似可用但运行时忽略”的资产。
- 范围：Definition/Configs/Registry 的序列化迁移，需要 ADR；旧场景引用先记录，不静默破坏。
- 验证：调用点/GUID 扫描、配置加载 EditMode、真实场景 PlayMode。
- 解锁：发布配置冻结。

### LEG-010 Legacy C# 与 Lua 主线引用审计及冻结

- 状态：Planned
- 依赖：`ARC-020`、`ELM-100`、`RUN-050`。
- 当前缺口：`Assets/Script_Legacy` 仍进入 Assembly-CSharp，旧 Lua 元素反应与新 C# 主链未收敛；共享资源可能仍有活动引用。
- 实施要点：① 建立脚本/GUID/场景/prefab/Lua 调用引用图；② 区分编译存在、运行时引用和纯备份；③ 主线明确禁止新增旧实现调用；④ 对仍需行为建立迁移 Feature Spec；⑤ 只有所有活动引用和回滚证据清楚后才提出隔离 asmdef、移出 Build 或删除。
- 可观察完成：可以准确说明哪些旧代码仍编译/运行、哪些仅历史；新功能扫描不再增加旧路径引用。
- 范围：首先只读审计和冻结规则；移动、删除、asmdef 隔离是独立迁移，需要 ADR/授权。
- 验证：GUID/调用点/Build 场景扫描、编译和主线 PlayMode。
- 解锁：`LEG-020`、发布体积/编译边界收口。

### LEG-020 旧场景、旧 WeaponView 与 Build Settings 迁移

- 状态：Planned
- 依赖：`LEG-010`、`WPN-060`、`RUN-090`。
- 当前缺口：`SampleScene.unity` 仍在 Build Settings 启用，Bootstrap 的 `FN IWS Primary` 仍挂旧 `WeaponView`；直接删除可能破坏序列化引用。
- 实施要点：① 冻结当前 Build Settings/场景 GUID/组件引用；② 证明新武器表现覆盖活动行为；③ 从 Bootstrap 精确迁移旧组件；④ 将 SampleScene 从发布场景列表移除或标为开发场景；⑤ 保留可恢复提交和 Player smoke 证据后才讨论删除资源。
- 可观察完成：发布 Player 只进入新主线场景且不实例化旧 WeaponView；旧场景处理状态有明确记录。
- 范围：scene/Build Settings 高风险迁移，需要 ADR、引用检查和独立审查。非目标是顺手删除全部旧资源。
- 验证：YAML/GUID scoped diff、场景加载 PlayMode、Windows64 启动与人工验收。
- 解锁：`BUILD-010`、`REL-010`。

## P2 体验、性能与发布

### UX-010 暂停、设置与基础可访问性

- 状态：Planned
- 依赖：`INP-010`、`HUD-030`、`RUN-050`。
- 当前缺口：首版需要统一暂停、音量、灵敏度、相机抖动/闪烁强度和输入重绑边界，不能由各 UI 局部改 `timeScale` 或配置资产。
- 实施要点：Run 接受暂停意图并保存唯一事实；时间相关 Gameplay 区分 scaled/unscaled；设置保存为玩家偏好；UI/输入焦点一致；提供相机抖动和强闪反馈强度开关。
- 可观察完成：暂停时批准系统停止、UI 仍可操作；恢复/重启/再次启动设置一致且不破坏冷却/无敌/引信。
- 范围：UI、输入、时间与本地设置；序列化/持久化格式需版本策略。
- 验证：时间矩阵 EditMode/PlayMode、Windows Player 人工检查。
- 解锁：发布候选体验门。

### PERF-010 代表性 Player 性能预算与回归

- 状态：Planned
- 依赖：`PRJ-050`、`RUN-030`、`BOS-030`。
- 当前缺口：尚无在真实 Windows Player、代表敌人数/投射物/反应/VFX 下的 CPU、GC、内存和帧时间基线，不能声称优化收益。
- 实施要点：① 定义目标硬件、分辨率、质量级别和代表场景；② 记录空场、普通波次、峰值波次和 Boss；③ 标记主线程/物理/渲染/GC 热点；④ 只优化有证据的瓶颈；⑤ 每项优化保存前后同条件数据和行为回归。
- 可观察完成：发布候选在已声明条件下达到批准预算，或明确记录未达标项与裁剪决定。
- 范围：Profiler/Player 证据及经批准的定向优化；不做无测量微优化。
- 验证：Development Player + Profiler 原始数据、自动/人工行为回归。
- 解锁：`BUILD-010`、`REL-020`。

### BUILD-010 当前主线 Windows64 构建与启动 smoke

- 状态：Planned
- 依赖：`VER-010`、`VER-020`、`LEG-020`、`PERF-010`、`RUN-090`。
- 当前缺口：现有基线证明旧可信起点的 Bootstrap-only 构建，没有证明当前发布候选、实际 Build Settings、Player 冷启动或完整一次 Run。
- 实施要点：冻结候选 commit/工作树；运行 EditMode+PlayMode；按批准发布场景构建；校验 Player 文件；启动 Player 捕获日志；执行冷启动、成功/失败/重开 smoke；保留构建与副作用恢复证据。
- 可观察完成：从全新 Windows64 输出可启动并完成 Must 流程，无 Missing Script/配置或旧场景入口。
- 范围：Windows64；非目标是安装器、签名、其他平台。
- 验证：Full Verified、Player 日志、截图/录像和人工验收记录。
- 解锁：`REL-010`、`REL-020`。

### REL-010 许可、Credits、README 与作品集交付包

- 状态：Planned
- 依赖：`LIC-010`、`BUILD-010`、`CFG-010`。
- 当前缺口：没有把可分发 Player、第三方许可/署名、运行说明、控制说明、已知限制和证据索引组成可审计交付包。
- 实施要点：只复制批准 Player；生成 Credits/ThirdParty 清单；README 区分已实现与计划；列出硬件/控制/玩法；验证不含禁止再分发源文件、调试密钥或无关大文件；记录 SHA-256。
- 可观察完成：陌生用户可按说明启动游戏，所有第三方内容可追溯，作品集描述不夸大未测结果。
- 范围：本地交付物；上传/发布需用户单独授权。
- 验证：包内容清单、哈希、干净目录启动和人工阅读。
- 解锁：`REL-020`。

### REL-020 发布候选独立审查与最终验收

- 状态：Planned
- 依赖：`RUN-090`、`BUILD-010`、`REL-010`。
- 当前缺口：需要独立于实施者核对行为、证据可信度、许可、生命周期、序列化迁移和完成声明。
- 实施要点：审查冻结后的 Feature Specs、ADR、完整 diff、测试 XML、Player/日志/录像和许可清单；发现按缺陷/违反验收/证据缺口/可选增强分类；前两类修复并重验；证据缺口下调声明或补证；可选项不阻断既定 Must。
- 可观察完成：无未处理 P0/P1 或违反 Must 验收，所有完成声明有证据；用户完成主线人工 Accepted 决定。
- 范围：只读审查优先；修复仍按原任务或新 Feature Spec 授权。上传、提交、推送不自动包含。
- 验证：审查报告、最终统一验证、人工 Accepted 记录。
- 解锁：可发布候选。

## 明确延期

### NET-010 双人网络实验

- 状态：Deferred
- 依赖：单机版本 `REL-020` 完成后重新评估。
- 延期理由：当前 `SourceObject`、Unity 对象引用、场景组合和大量可变运行时状态不是网络稳定身份；在单机状态所有权未稳定前接入联网会扩大所有契约和测试矩阵。
- 未来入口：先按 [`Networking.md`](../Design/Networking.md) 选一个隔离实验，定义网络实体/攻击 ID、服务器权威、输入同步、预测/回滚和断线恢复；不得把本地 Unity 引用当网络标识。
- 非目标：首版作品集不承诺联网、跨机同步或在线服务。
