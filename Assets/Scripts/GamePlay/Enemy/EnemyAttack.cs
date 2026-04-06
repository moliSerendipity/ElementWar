using Game.Definition.Combat;
using Game.Definition.Enemy;
using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.Enemy
{
    /// <summary>
    /// 敌人攻击阶段。
    /// </summary>
    public enum EnemyAttackPhase
    {
        /// <summary>未在攻击中。</summary>
        None,
        /// <summary>前摇：起手动作，给玩家反应时间。敌人仍可追踪目标。</summary>
        Windup,
        /// <summary>判定帧：伤害生效。方向在此刻锁定。</summary>
        Strike,
        /// <summary>后摇：攻击后的僵硬期，玩家的反击窗口。</summary>
        Recovery,
    }

    /// <summary>
    /// 敌人攻击系统。
    ///
    /// 核心设计：
    /// 1. 多攻击行为：通过 EnemyAttackConfig[] 配置多种攻击（轻击/重击/跳砸等），
    ///    发起攻击时根据距离 + 权重自动选择
    /// 2. 时长从 AnimationClip 自动读取：config.BaseDuration = clip.length，
    ///    前摇 = BaseDuration × damageNormalizedTime，后摇 = 剩余时间
    ///    攻速 Buff 只需改 EnemyStat.AttackSpeedMultiplier
    /// 3. 可配置判定形状：球形 / 扇形 / 盒形，从当前攻击 config 读取
    /// 4. 方向在 Strike 瞬间锁定：Windup 期间 Brain 可持续追踪目标
    ///
    /// 注意：
    /// 攻击可进入范围的唯一真相源是 EnemyAttackConfig。Brain 不再依赖 EnemyStat 中的独立 AttackRange。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyAttack : MonoBehaviour
    {
        #region Inspector

        [Header("Attack Library")]
        [Tooltip("该敌人可用的全部攻击配置。Brain 发起攻击时根据距离和权重选择其中一个。")]
        [SerializeField] private EnemyAttackConfig[] attackConfigs;

        [Header("Detection")]
        [Tooltip("可被攻击命中的物理层。通常只含玩家角色所在层。")]
        [SerializeField] private LayerMask attackTargetMask;

        [Header("Debug (Read Only)")]
        [SerializeField] private EnemyAttackPhase currentPhase;
        [SerializeField] private float phaseTimer;
        [SerializeField] private float nextAttackAllowedTime;
        [SerializeField] private string currentAttackName;

        #endregion

        #region Runtime

        private EnemyStat enemyStat;
        private HealthComponent ownHealthComponent;

        /// <summary>本次攻击选中的配置。攻击结束后清空。</summary>
        private EnemyAttackConfig activeConfig;

        /// <summary>本次 Strike 是否已投递伤害。避免多帧重复造伤。</summary>
        private bool hasDamageBeenDelivered;

        /// <summary>Strike 瞬间锁定的面朝方向。</summary>
        private Vector3 lockedStrikeDirection;

        /// <summary>物理检测缓冲区。</summary>
        private readonly Collider[] hitBuffer = new Collider[16];

        /// <summary>Strike 固定短窗口（秒）。足够执行一次伤害检测。</summary>
        private const float StrikeFixedDuration = 0.1f;

        #endregion

        #region Public Accessors

        /// <summary>当前攻击阶段。供 Brain 和 AnimationBridge 读取。</summary>
        public EnemyAttackPhase CurrentPhase => currentPhase;

        /// <summary>是否正在攻击中（任意非 None 阶段）。</summary>
        public bool IsAttacking => currentPhase != EnemyAttackPhase.None;

        /// <summary>是否处于前摇阶段。Brain 在此期间可让敌人继续追踪目标。</summary>
        public bool IsInWindup => currentPhase == EnemyAttackPhase.Windup;

        /// <summary>攻击冷却完毕且未在攻击中。</summary>
        public bool IsReady => currentPhase == EnemyAttackPhase.None && Time.time >= nextAttackAllowedTime;

        /// <summary>当前选中的攻击配置。供 AnimationBridge 读取动画触发名。</summary>
        public EnemyAttackConfig ActiveConfig => activeConfig;

        /// <summary>是否配置了至少一种攻击行为。</summary>
        public bool HasAttacks => attackConfigs != null && attackConfigs.Length > 0;

        #endregion

        #region Initialization

        private void Awake()
        {
            ownHealthComponent = GetComponentInParent<HealthComponent>();
        }

        /// <summary>
        /// 由 EnemyRoot 在初始化阶段注入 EnemyStat。
        /// </summary>
        public void Initialize(EnemyStat _enemyStat)
        {
            enemyStat = _enemyStat;
        }

        #endregion

        #region Attack Queries

        /// <summary>
        /// 判断当前距离下，攻击库中是否至少存在一种可用攻击。
        /// 这是 Brain 进入 Attack 状态的唯一距离判断入口。
        /// </summary>
        public bool CanEnterAttackState(float _distanceToTarget)
        {
            if (HasAttacks == false)
            {
                return false;
            }

            return FindFirstAvailableAttackConfig(_distanceToTarget) != null;
        }

        /// <summary>
        /// 计算攻击库的最小可用距离。主要用于调试观察，不作为额外配置真相源。
        /// </summary>
        public float GetMinAvailableAttackRange()
        {
            if (HasAttacks == false)
            {
                return 0f;
            }

            float minRange = float.MaxValue;
            bool hasValidConfig = false;

            for (int i = 0; i < attackConfigs.Length; i++)
            {
                EnemyAttackConfig config = attackConfigs[i];
                if (config == null)
                {
                    continue;
                }

                minRange = Mathf.Min(minRange, Mathf.Max(0f, config.MinUseRange));
                hasValidConfig = true;
            }

            return hasValidConfig ? minRange : 0f;
        }

        /// <summary>
        /// 计算攻击库的最大可用距离。主要用于调试观察，不作为额外配置真相源。
        /// </summary>
        public float GetMaxAvailableAttackRange()
        {
            if (HasAttacks == false)
            {
                return 0f;
            }

            float maxRange = 0f;
            for (int i = 0; i < attackConfigs.Length; i++)
            {
                EnemyAttackConfig config = attackConfigs[i];
                if (config == null)
                {
                    continue;
                }

                maxRange = Mathf.Max(maxRange, Mathf.Max(0f, config.MaxUseRange));
            }

            return maxRange;
        }

        #endregion

        #region Attack Lifecycle

        /// <summary>
        /// 尝试发起一次攻击。根据与目标的距离从攻击库中选择。
        /// </summary>
        public bool TryBeginAttack(float _distanceToTarget)
        {
            if (IsReady == false || HasAttacks == false)
            {
                return false;
            }

            // 根据距离和权重选择一个攻击配置。
            EnemyAttackConfig selectedAttackConfig = SelectAttackConfig(_distanceToTarget);
            if (selectedAttackConfig == null)
            {
                return false;
            }

            activeConfig = selectedAttackConfig;
            currentAttackName = selectedAttackConfig.AnimationTriggerName;
            hasDamageBeenDelivered = false;
            lockedStrikeDirection = Vector3.zero;
            EnterPhase(EnemyAttackPhase.Windup);
            return true;
        }

        /// <summary>
        /// 每帧推进攻击阶段。由 Brain 在 Attack 状态下调用。
        /// </summary>
        public void Tick(float _deltaTime)
        {
            if (currentPhase == EnemyAttackPhase.None || activeConfig == null)
            {
                return;
            }

            // 阶段计时器受攻速倍率影响——倍率越高消耗越快。
            phaseTimer -= _deltaTime * GetAttackSpeedMultiplier();

            if (phaseTimer <= 0f)
            {
                AdvancePhase();
                return;
            }

            // Strike 阶段内执行伤害投递（仅一次）。
            if (currentPhase == EnemyAttackPhase.Strike && hasDamageBeenDelivered == false)
            {
                DeliverDamage();
                hasDamageBeenDelivered = true;
            }
        }

        /// <summary>
        /// 强制取消当前攻击。
        /// </summary>
        public void CancelAttack()
        {
            currentPhase = EnemyAttackPhase.None;
            phaseTimer = 0f;
            activeConfig = null;
            currentAttackName = string.Empty;
            hasDamageBeenDelivered = false;
            lockedStrikeDirection = Vector3.zero;
        }

        #endregion

        #region Attack Selection

        /// <summary>
        /// 返回当前距离下第一个可用攻击。仅用于快速判断是否能进入 Attack 状态。
        /// </summary>
        private EnemyAttackConfig FindFirstAvailableAttackConfig(float _distance)
        {
            for (int i = 0; i < attackConfigs.Length; i++)
            {
                EnemyAttackConfig config = attackConfigs[i];
                if (config != null && config.IsInRange(_distance))
                {
                    return config;
                }
            }

            return null;
        }

        /// <summary>
        /// 从攻击库中按距离过滤 + 权重随机选择。
        /// </summary>
        private EnemyAttackConfig SelectAttackConfig(float _distance)
        {
            // 先收集所有距离合法的候选和总权重。
            int totalWeight = 0;
            for (int i = 0; i < attackConfigs.Length; i++)
            {
                if (attackConfigs[i] != null && attackConfigs[i].IsInRange(_distance))
                {
                    totalWeight += attackConfigs[i].SelectionWeight;
                }
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            // 按权重随机选择。
            int roll = Random.Range(0, totalWeight);
            int accumulated = 0;
            for (int i = 0; i < attackConfigs.Length; i++)
            {
                EnemyAttackConfig config = attackConfigs[i];
                if (config == null || config.IsInRange(_distance) == false)
                {
                    continue;
                }

                accumulated += config.SelectionWeight;
                if (roll < accumulated)
                {
                    return config;
                }
            }

            return null;
        }

        #endregion

        #region Phase Management

        /// <summary>
        /// 进入指定阶段，设置计时器。
        /// 时长基于 config.BaseDuration（从 AnimationClip.length 自动读取）。
        /// Tick 中通过乘以攻速倍率加速消耗，AnimationBridge 同步缩放 Animator.speed。
        /// </summary>
        private void EnterPhase(EnemyAttackPhase _phase)
        {
            currentPhase = _phase;

            float baseDuration = activeConfig.BaseDuration;
            phaseTimer = _phase switch
            {
                // 前摇 = 动画总时长 × 伤害归一化时间点。
                EnemyAttackPhase.Windup => baseDuration * activeConfig.DamageNormalizedTime,

                // Strike 固定短窗口。
                EnemyAttackPhase.Strike => StrikeFixedDuration,

                // 后摇 = 剩余时间 - Strike 窗口。
                EnemyAttackPhase.Recovery => Mathf.Max(0.05f,
                    baseDuration * (1f - activeConfig.DamageNormalizedTime) - StrikeFixedDuration),

                _ => 0f,
            };

            // 进入 Strike 的瞬间锁定面朝方向。
            if (_phase == EnemyAttackPhase.Strike)
            {
                lockedStrikeDirection = transform.forward;
            }
        }

        /// <summary>
        /// 当前阶段计时器归零，推进到下一阶段。
        /// </summary>
        private void AdvancePhase()
        {
            switch (currentPhase)
            {
                case EnemyAttackPhase.Windup:
                    EnterPhase(EnemyAttackPhase.Strike);
                    break;

                case EnemyAttackPhase.Strike:
                    EnterPhase(EnemyAttackPhase.Recovery);
                    break;

                case EnemyAttackPhase.Recovery:
                    // 攻击完成，进入冷却。
                    currentPhase = EnemyAttackPhase.None;
                    phaseTimer = 0f;
                    nextAttackAllowedTime = Time.time + enemyStat.AttackCooldown;
                    activeConfig = null;
                    currentAttackName = string.Empty;
                    break;
            }
        }

        #endregion

        #region Speed

        /// <summary>
        /// 获取当前攻速倍率。EnemyStat 没有该字段时返回 1。
        /// </summary>
        private float GetAttackSpeedMultiplier()
        {
            return enemyStat != null ? Mathf.Max(0.1f, enemyStat.AttackSpeedMultiplier) : 1f;
        }

        #endregion

        #region Damage Delivery

        /// <summary>
        /// 根据当前攻击配置的判定形状检测目标，通过 DamageResolver 投递伤害。
        /// </summary>
        private void DeliverDamage()
        {
            if (activeConfig == null)
            {
                return;
            }

            // 根据配置的形状类型执行对应的物理检测。
            int hitCount = DetectTargets(activeConfig);
            if (hitCount <= 0)
            {
                return;
            }

            // 对命中的有效目标投递伤害。
            float damage = enemyStat.AttackPower * activeConfig.DamageMultiplier;
            bool aoe = activeConfig.IsAreaOfEffect;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = hitBuffer[i];
                if (hitCollider == null)
                {
                    continue;
                }

                HealthComponent targetHealth = hitCollider.GetComponentInParent<HealthComponent>();
                if (targetHealth == null || targetHealth == ownHealthComponent)
                {
                    continue;
                }

                SubmitDamage(hitCollider, targetHealth, damage, activeConfig.DamageKind);

                // 非 AOE 攻击只命中第一个有效目标。
                if (aoe == false)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 构建伤害请求并通过 DamageResolver 统一结算。
        /// </summary>
        private void SubmitDamage(
            Collider _hitCollider,
            HealthComponent _targetHealth,
            float _damage,
            CombatDamageKind _damageKind)
        {
            Vector3 hitPoint = _hitCollider.ClosestPoint(transform.position);
            Vector3 hitNormal = (transform.position - hitPoint).normalized;
            float hitDistance = Vector3.Distance(transform.position, hitPoint);

            HitScanHitContext hitContext = new(
                true, _hitCollider, _targetHealth,
                hitPoint, hitNormal, hitDistance, CombatHitPartType.Default);

            CombatDamageRequestContext request = new(
                gameObject,
                _damageKind,
                _damage,
                _critChance: 0f,
                _critDamageMultiplier: 1f,
                _headShotDamageMultiplier: 1f,
                _weakPointDamageMultiplier: 1f,
                transform.position,
                lockedStrikeDirection,
                hitContext,
                Time.time);

            DamageResolver.ResolveAndApply(request);
        }

        #endregion

        #region Shape Detection

        /// <summary>
        /// 根据攻击配置的形状类型执行物理检测，结果写入 hitBuffer。
        /// 返回命中数量。
        /// </summary>
        private int DetectTargets(EnemyAttackConfig _config)
        {
            return _config.ShapeType switch
            {
                AttackShapeType.Sphere => DetectSphere(_config),
                AttackShapeType.Sector => DetectSector(_config),
                AttackShapeType.Box => DetectBox(_config),
                _ => 0,
            };
        }

        /// <summary>
        /// 球形检测。判定中心沿锁定方向偏移。
        /// </summary>
        private int DetectSphere(EnemyAttackConfig _config)
        {
            Vector3 center = transform.position + lockedStrikeDirection * _config.OffsetDistance;
            return Physics.OverlapSphereNonAlloc(
                center, _config.Radius, hitBuffer, attackTargetMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// 扇形检测。先 OverlapSphere 收集候选，再按角度过滤。
        /// 把不在扇形内的碰撞体置 null，返回原始 hitCount（调用方跳过 null）。
        /// </summary>
        private int DetectSector(EnemyAttackConfig _config)
        {
            Vector3 center = transform.position + lockedStrikeDirection * _config.OffsetDistance;
            int hitCount = Physics.OverlapSphereNonAlloc(
                center, _config.Radius, hitBuffer, attackTargetMask, QueryTriggerInteraction.Ignore);

            // 对每个候选做角度检查，不在扇形内的置 null。
            float halfAngle = _config.SectorHalfAngle;
            for (int i = 0; i < hitCount; i++)
            {
                if (hitBuffer[i] == null)
                {
                    continue;
                }

                Vector3 toTarget = hitBuffer[i].transform.position - transform.position;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude < 0.001f)
                {
                    // 几乎重叠，算在扇形内。
                    continue;
                }

                if (Vector3.Angle(lockedStrikeDirection, toTarget.normalized) > halfAngle)
                {
                    hitBuffer[i] = null;
                }
            }

            return hitCount;
        }

        /// <summary>
        /// 盒形检测。沿锁定方向偏移放置 OverlapBox。
        /// </summary>
        private int DetectBox(EnemyAttackConfig _config)
        {
            Vector3 center = transform.position + lockedStrikeDirection * _config.OffsetDistance;
            Vector3 halfExtents = new(_config.BoxHalfWidth, _config.BoxHalfHeight, _config.BoxHalfDepth);
            Quaternion orientation = Quaternion.LookRotation(lockedStrikeDirection, Vector3.up);

            return Physics.OverlapBoxNonAlloc(
                center, halfExtents, hitBuffer, orientation, attackTargetMask, QueryTriggerInteraction.Ignore);
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (attackConfigs == null || attackConfigs.Length == 0)
            {
                return;
            }

            // 运行时显示当前攻击的判定区域，编辑时显示第一个配置的区域。
            EnemyAttackConfig drawConfig = activeConfig ?? (attackConfigs.Length > 0 ? attackConfigs[0] : null);
            if (drawConfig == null)
            {
                return;
            }

            Vector3 direction = Application.isPlaying && currentPhase == EnemyAttackPhase.Strike
                ? lockedStrikeDirection
                : transform.forward;
            Vector3 center = transform.position + direction * drawConfig.OffsetDistance;

            Gizmos.color = IsAttacking
                ? new Color(1f, 0.3f, 0f, 0.4f)
                : new Color(1f, 0.6f, 0f, 0.15f);

            switch (drawConfig.ShapeType)
            {
                case AttackShapeType.Sphere:
                    Gizmos.DrawWireSphere(center, drawConfig.Radius);
                    break;

                case AttackShapeType.Sector:
                    // 扇形用球+两条边线表示。
                    Gizmos.DrawWireSphere(center, drawConfig.Radius);
                    Vector3 leftEdge = Quaternion.Euler(0f, -drawConfig.SectorHalfAngle, 0f) * direction * drawConfig.Radius;
                    Vector3 rightEdge = Quaternion.Euler(0f, drawConfig.SectorHalfAngle, 0f) * direction * drawConfig.Radius;
                    Gizmos.DrawLine(transform.position, transform.position + leftEdge);
                    Gizmos.DrawLine(transform.position, transform.position + rightEdge);
                    break;

                case AttackShapeType.Box:
                    Quaternion orientation = Quaternion.LookRotation(direction, Vector3.up);
                    Vector3 size = new(drawConfig.BoxHalfWidth * 2f, drawConfig.BoxHalfHeight * 2f, drawConfig.BoxHalfDepth * 2f);
                    Gizmos.matrix = Matrix4x4.TRS(center, orientation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, size);
                    Gizmos.matrix = Matrix4x4.identity;
                    break;
            }
        }
#endif

        #endregion
    }
}
