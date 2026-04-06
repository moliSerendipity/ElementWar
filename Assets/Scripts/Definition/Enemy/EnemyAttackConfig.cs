using Game.Definition.Combat;
using Game.Definition.ConfigSystem.Core;
using UnityEngine;

namespace Game.Definition.Enemy
{
    /// <summary>
    /// 攻击判定区域形状。
    /// </summary>
    public enum AttackShapeType
    {
        /// <summary>球形。适合拳击、锤砸等全方位近身攻击。</summary>
        Sphere,
        /// <summary>扇形。适合劈砍、横扫等弧形攻击。OverlapSphere + 角度过滤。</summary>
        Sector,
        /// <summary>盒形。适合突刺、冲撞等线性攻击。</summary>
        Box,
    }

    /// <summary>
    /// 单次敌人攻击行为的完整定义。
    ///
    /// 设计要点：
    /// 1. 每种攻击（轻击/重击/跳砸等）各创建一个 SO 实例
    /// 2. 动画时长从 AnimationClip 引用自动读取，不手填
    /// 3. 有效打击距离从形状参数自动推导，maxUseRange 校验时检查不超过此值
    /// 4. Inspector 中不同 ShapeType 只显示对应的形状参数（需配合 EnemyAttackConfigEditor）
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyAttackConfig", menuName = "Game/Configs/Enemy/Enemy Attack Config")]
    public sealed class EnemyAttackConfig : ConfigBase
    {
        #region Animation

        [Header("Animation")]
        [Tooltip("攻击动画 Clip。时长自动从 clip.length 读取，不需要手填。\n" +
                 "请确保此 Clip 与 Animator Controller 中对应状态使用的是同一个。")]
        [SerializeField] private AnimationClip animationClip;

        [Tooltip("Animator.SetTrigger 使用的参数名。\n" +
                 "对应 Animator Controller 中触发此攻击动画的 Trigger 参数。")]
        [SerializeField] private string animationTriggerName = "AttackTrigger";

        [Tooltip("伤害判定在动画进度中的归一化时间点（0~1）。\n" +
                 "例如 0.4 = 动画播到 40% 时执行伤害检测。\n" +
                 "需要观察动画，找到武器/拳头挥到最远处的那一帧。")]
        [SerializeField, Range(0.05f, 0.95f)] private float damageNormalizedTime = 0.4f;

        #endregion

        #region Damage

        [Header("Damage")]
        [Tooltip("相对于 EnemyStat.AttackPower 的伤害倍率。\n" +
                 "轻击 = 1.0，重击 = 1.8，跳砸 = 2.5 等。")]
        [SerializeField, Min(0.1f)] private float damageMultiplier = 1f;

        [Tooltip("伤害类型。")]
        [SerializeField] private CombatDamageKind damageKind = CombatDamageKind.Physical;

        [Tooltip("是否命中范围内所有有效目标（AOE）。\nfalse = 只命中最近的一个。")]
        [SerializeField] private bool isAreaOfEffect;

        #endregion

        #region Shape

        [Header("Detection Shape")]
        [SerializeField] private AttackShapeType shapeType = AttackShapeType.Sphere;

        [Tooltip("判定中心距敌人位置的前方偏移距离。")]
        [SerializeField, Min(0f)] private float offsetDistance = 0.8f;

        // --- Sphere & Sector 共用 ---
        [Tooltip("Sphere/Sector: 检测半径。")]
        [SerializeField, Min(0.1f)] private float radius = 1.2f;

        // --- Sector 专用 ---
        [Tooltip("Sector: 扇形半角（度）。60 = 左右各 60°，共 120° 扇形。")]
        [SerializeField, Range(10f, 180f)] private float sectorHalfAngle = 60f;

        // --- Box 专用 ---
        [Tooltip("Box: 前方半深度（z 方向）。")]
        [SerializeField, Min(0.1f)] private float boxHalfDepth = 1.0f;

        [Tooltip("Box: 半宽（x 方向）。")]
        [SerializeField, Min(0.1f)] private float boxHalfWidth = 0.5f;

        [Tooltip("Box: 半高（y 方向）。")]
        [SerializeField, Min(0.1f)] private float boxHalfHeight = 0.8f;

        #endregion

        #region Selection

        [Header("Selection")]
        [Tooltip("Brain 使用此攻击的最大距离。\n" +
                 "应 ≤ 有效打击距离（自动从形状参数计算），否则会打空。")]
        [SerializeField, Min(0.1f)] private float maxUseRange = 2.5f;

        [Tooltip("Brain 使用此攻击的最小距离。")]
        [SerializeField, Min(0f)] private float minUseRange;

        [Tooltip("距离合法时，多个候选按权重随机选择。")]
        [SerializeField, Min(1)] private int selectionWeight = 10;

        #endregion

        #region Properties — Animation

        /// <summary>攻击动画 Clip 引用。</summary>
        public AnimationClip AnimationClip => animationClip;

        /// <summary>Animator Trigger 参数名。</summary>
        public string AnimationTriggerName => animationTriggerName;

        /// <summary>
        /// 1x 攻速下的攻击动画完整时长（秒）。
        /// 直接从 AnimationClip.length 读取。clip 缺失时回退为 1.0s。
        /// </summary>
        public float BaseDuration => animationClip != null ? animationClip.length : 1f;

        /// <summary>伤害判定在动画进度中的归一化时间点。</summary>
        public float DamageNormalizedTime => damageNormalizedTime;

        #endregion

        #region Properties — Damage

        public float DamageMultiplier => damageMultiplier;
        public CombatDamageKind DamageKind => damageKind;
        public bool IsAreaOfEffect => isAreaOfEffect;

        #endregion

        #region Properties — Shape

        public AttackShapeType ShapeType => shapeType;
        public float OffsetDistance => offsetDistance;
        public float Radius => radius;
        public float SectorHalfAngle => sectorHalfAngle;
        public float BoxHalfDepth => boxHalfDepth;
        public float BoxHalfWidth => boxHalfWidth;
        public float BoxHalfHeight => boxHalfHeight;

        #endregion

        #region Properties — Selection

        public float MaxUseRange => maxUseRange;
        public float MinUseRange => minUseRange;
        public int SelectionWeight => selectionWeight;

        #endregion

        #region Computed

        /// <summary>
        /// 从形状参数自动推导的有效打击最远距离。
        /// 超过此距离的目标不可能被命中。
        /// </summary>
        public float EffectiveStrikeRange
        {
            get
            {
                return shapeType switch
                {
                    AttackShapeType.Sphere => offsetDistance + radius,
                    AttackShapeType.Sector => offsetDistance + radius,
                    AttackShapeType.Box => offsetDistance + boxHalfDepth,
                    _ => offsetDistance + radius,
                };
            }
        }

        #endregion

        #region Queries

        /// <summary>
        /// 给定距离是否在此攻击的可用范围内。
        /// </summary>
        public bool IsInRange(float _distance)
        {
            return _distance >= minUseRange && _distance <= maxUseRange;
        }

        #endregion

        #region Validation

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (animationClip == null)
            {
                _context.AddWarning(ConfigId, "animationClip 未设置，BaseDuration 将回退为 1.0s。");
            }

            if (string.IsNullOrWhiteSpace(animationTriggerName))
            {
                _context.AddError(ConfigId, "animationTriggerName 不能为空。");
            }

            if (maxUseRange <= minUseRange)
            {
                _context.AddError(ConfigId, "maxUseRange 必须大于 minUseRange。");
            }

            // 校验使用距离不超过有效打击距离，避免"够得着发起但打不中"的配置错误。
            float effectiveRange = EffectiveStrikeRange;
            if (maxUseRange > effectiveRange + 0.01f)
            {
                _context.AddWarning(ConfigId,
                    $"maxUseRange ({maxUseRange:F2}) 超过有效打击距离 ({effectiveRange:F2})，" +
                    $"敌人可能在打不到目标的距离发起攻击。");
            }
        }

        #endregion
    }
}
