using Game.Definition.Combat;
using Game.Definition.Element;
using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.Element
{
    /// <summary>
    /// 把一次已提交 Overload 反应转换为确定的范围伤害与敌人控制输出。
    /// </summary>
    internal static class OverloadReactionResolver
    {
        // 首版确认值：触发基准伤害的 80%，以反应目标为中心作用 3.5 米。
        private const float DamageMultiplier = 0.8f;
        private const float EffectRadius = 3.5f;

        // 当前控制入口会对 Elite 的硬控时长折半，并为 Boss 追加同量转换削韧。
        private const float ToughnessDamage = 200f;
        private const float HardControlDuration = 1f;

        /// <summary>
        /// 以反应目标为中心查询合法敌人，并让每个目标共享一个新的反应执行身份，
        /// 从而分别提交一次伤害与一次控制，且不会与触发命中的直接伤害互相去重。
        /// </summary>
        /// <param name="_reactionResult">元素管线已经原子提交的反应事实。</param>
        /// <param name="_triggerBaseDamage">触发命中进入目标减伤前的基础伤害。</param>
        /// <param name="_targetMask">范围物理候选所在层，只承担粗筛。</param>
        /// <param name="_applicationTime">反应成立的运行时时间戳。</param>
        internal static void ResolveAndApply(
            in ElementReactionResult _reactionResult,
            float _triggerBaseDamage,
            LayerMask _targetMask,
            float _applicationTime)
        {
            if (_reactionResult.ReactionType != ElementReactionType.Overload)
            {
                return;
            }

            ElementApplicationRequest triggeringApplication =
                _reactionResult.TriggeringApplication;
            ElementApplicationSourceSnapshot source = triggeringApplication.Source;
            Combatant reactionTarget = triggeringApplication.TargetCombatant;
            if (source == null || reactionTarget == null)
            {
                return;
            }

            // Overload 固定以发生反应的目标根为中心；范围查询继续统一裁决生命与阵营合法性。
            Vector3 effectCenter = reactionTarget.transform.position;
            CombatRangeTarget[] targets = CombatRangeQuery.QueryDamageableTargets(
                source.InstigatorCombatant,
                effectCenter,
                EffectRadius,
                _targetMask);
            if (targets.Length == 0)
            {
                return;
            }

            // 独立反应使用新的执行身份，同一身份可命中不同目标，但每个目标只接受一次。
            AttackExecutionId reactionExecutionId = AttackExecutionId.Create();
            float reactionDamage = Mathf.Max(0f, _triggerBaseDamage) * DamageMultiplier;

            for (int i = 0; i < targets.Length; i++)
            {
                CombatRangeTarget rangeTarget = targets[i];
                Combatant target = rangeTarget.Target;
                Vector3 attackVector = rangeTarget.ClosestPoint - effectCenter;
                Vector3 attackDirection = attackVector.sqrMagnitude > Mathf.Epsilon
                    ? attackVector.normalized
                    : Vector3.forward;

                // 控制先于伤害事件写回，确保致死伤害不会让同一反应已选中的控制输出丢失。
                EnemyControlApplicationRequest controlRequest = new(
                    reactionExecutionId,
                    source.InstigatorCombatant,
                    target,
                    ToughnessDamage,
                    HardControlDuration,
                    ToughnessDamage);
                EnemyControlApplicationResolver.ResolveAndApply(
                    controlRequest,
                    _applicationTime);

                // Overload 是不可命中弱点、不会再次附着元素的触发元素爆炸伤害。
                DamageRequest damageRequest = new(
                    reactionExecutionId,
                    source.InstigatorCombatant,
                    source.SourceObject,
                    target,
                    source.Element,
                    DamageDeliveryType.Explosion,
                    reactionDamage,
                    HitPartType.Default,
                    1f,
                    1f,
                    effectCenter,
                    attackDirection,
                    rangeTarget.ClosestPoint,
                    -attackDirection,
                    _applicationTime);
                DamageResolver.ResolveAndApply(damageRequest);
            }
        }
    }
}
