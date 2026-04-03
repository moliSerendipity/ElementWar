using UnityEngine;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.AI
{
    /// <summary>
    /// 战斗实体通用 AI 参数表。
    /// 当前版本同时供敌人与 AI 队友使用；如果将来字段分叉明显，再拆专用表。
    /// </summary>
    [CreateAssetMenu(fileName = "ActorAIProfileConfig", menuName = "Game/Configs/AI/Actor AI Profile Config")]
    public sealed class ActorAIProfileConfig : ConfigBase
    {
        [Header("Perception")]
        [SerializeField] private float detectRange = 15f;
        [SerializeField] private float loseTargetRange = 20f;

        [Header("Movement")]
        [SerializeField] private float chaseRange = 12f;
        [SerializeField] private float disengageRange = 24f;
        [SerializeField] private float thinkInterval = 0.2f;
        [SerializeField] private float reactionDelay = 0.1f;

        [Header("Combat")]
        [SerializeField] private float preferredAttackRange = 3f;
        [Range(0f, 1f)]
        [SerializeField] private float strafeProbability = 0.15f;

        public float DetectRange => detectRange;
        public float LoseTargetRange => loseTargetRange;
        public float ChaseRange => chaseRange;
        public float DisengageRange => disengageRange;
        public float ThinkInterval => thinkInterval;
        public float ReactionDelay => reactionDelay;
        public float PreferredAttackRange => preferredAttackRange;
        public float StrafeProbability => strafeProbability;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (detectRange <= 0f)
            {
                _context.AddError(ConfigId, "DetectRange 必须大于 0。");
            }

            if (loseTargetRange < detectRange)
            {
                _context.AddWarning(ConfigId, "LoseTargetRange 小于 DetectRange，可能导致 AI 目标丢失过早。");
            }

            if (thinkInterval <= 0f)
            {
                _context.AddError(ConfigId, "ThinkInterval 必须大于 0。");
            }

            if (disengageRange < chaseRange)
            {
                _context.AddWarning(ConfigId, "DisengageRange 小于 ChaseRange，可能导致 AI 反复切换追击状态。");
            }
        }
    }
}