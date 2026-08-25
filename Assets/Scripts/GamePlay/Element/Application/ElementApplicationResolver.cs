using Game.Gameplay.Combat;

namespace Game.Gameplay.Element
{
    /// <summary>
    /// 元素请求的目标侧唯一提交入口；定位目标运行时后由目标所有者完成最终裁决。
    /// </summary>
    internal static class ElementApplicationResolver
    {
        /// <summary>
        /// 尝试把独立元素请求提交到其权威目标；不读取或修改伤害结果。
        /// </summary>
        /// <param name="_request">由 ELM-010 工厂创建的元素施加请求。</param>
        /// <returns>已提交、待反应、不变或带明确原因的拒绝结果。</returns>
        internal static ElementApplicationResult ResolveAndApply(in ElementApplicationRequest _request)
        {
            // 请求只冻结目标引用和身份；先确认目标对象仍存在，再读取其权威状态所有者。
            Combatant targetCombatant = _request.TargetCombatant;
            if (targetCombatant == null)
            {
                return ElementApplicationResult.Rejected(
                    ElementApplicationRejectionReason.InvalidTarget);
            }

            // 元素状态只能由目标自己的 Runtime 裁决，Resolver 不建立临时或全局替代状态。
            ElementAttachmentRuntime attachmentRuntime = targetCombatant.ElementAttachments;
            if (attachmentRuntime == null)
            {
                return ElementApplicationResult.Rejected(
                    ElementApplicationRejectionReason.MissingAttachmentOwner);
            }

            return attachmentRuntime.ResolveAndApply(_request);
        }
    }
}
