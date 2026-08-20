using System;
using Game.Definition.Combat;
using Game.Definition.ConfigSystem.Core;
using UnityEngine;

namespace Game.Definition.Element
{
    /// <summary>
    /// 定义一个元素来源可复用的施加规则；运行时来源身份与目标间隔状态由 Gameplay 持有。
    /// </summary>
    [CreateAssetMenu(
        fileName = "ElementApplicationProfileConfig",
        menuName = "Game/Configs/Element/Element Application Profile Config")]
    public sealed class ElementApplicationProfileConfig : ConfigBase
    {
        [Tooltip("本 Profile 尝试施加的元素。None 不是合法的元素施加。")]
        [SerializeField] private ElementType element = ElementType.Fire;

        [Tooltip("同一运行时来源再次尝试影响同一目标前的最短间隔，单位为秒。0 表示不限制。")]
        [SerializeField, Min(0f)] private float sourceTargetIntervalSeconds;

        [Tooltip("成功附着后元素默认保留的时间，单位为秒。")]
        [SerializeField, Min(0.01f)] private float attachmentDurationSeconds = 6f;

        /// <summary>本 Profile 尝试施加的元素。</summary>
        public ElementType Element => element;

        /// <summary>同一运行时来源与目标之间的最短再次应用间隔，单位为秒。</summary>
        public float SourceTargetIntervalSeconds => sourceTargetIntervalSeconds;

        /// <summary>成功附着后的默认持续时间，单位为秒。</summary>
        public float AttachmentDurationSeconds => attachmentDurationSeconds;

        /// <summary>元素、间隔和持续时间是否能安全冻结进运行时来源快照。</summary>
        public bool HasValidApplicationData =>
            Enum.IsDefined(typeof(ElementType), element)
            && element != ElementType.None
            && IsFinite(sourceTargetIntervalSeconds)
            && sourceTargetIntervalSeconds >= 0f
            && IsFinite(attachmentDurationSeconds)
            && attachmentDurationSeconds > 0f;

        /// <inheritdoc />
        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (Enum.IsDefined(typeof(ElementType), element) == false || element == ElementType.None)
            {
                _context.AddError(ConfigId, "element 必须是 Fire、Water、Electric 或 Ice。");
            }

            if (IsFinite(sourceTargetIntervalSeconds) == false || sourceTargetIntervalSeconds < 0f)
            {
                _context.AddError(ConfigId, "sourceTargetIntervalSeconds 必须是大于等于 0 的有限数值。");
            }

            if (IsFinite(attachmentDurationSeconds) == false || attachmentDurationSeconds <= 0f)
            {
                _context.AddError(ConfigId, "attachmentDurationSeconds 必须是大于 0 的有限数值。");
            }
        }

        private static bool IsFinite(float _value)
        {
            return float.IsNaN(_value) == false && float.IsInfinity(_value) == false;
        }
    }
}
