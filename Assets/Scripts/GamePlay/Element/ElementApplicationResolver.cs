using Game.Gameplay.Combat;

namespace Game.Gameplay.Element
{
    /// <summary>
    /// 元素请求的目标侧唯一提交入口；定位目标运行时后由目标所有者完成最终裁决。
    /// </summary>
    public static class ElementApplicationResolver
    {
        /// <summary>
        /// 尝试把独立元素请求提交到其权威目标；不读取或修改伤害结果。
        /// </summary>
        /// <param name="_request">由 ELM-010 工厂创建的元素施加请求。</param>
        /// <returns>已提交、待反应、不变或带明确原因的拒绝结果。</returns>
        public static ElementApplicationResult ResolveAndApply(in ElementApplicationRequest _request)
        {
            Combatant targetCombatant = _request.TargetCombatant;
            if (targetCombatant == null)
            {
                return ElementApplicationResult.Rejected(
                    _request,
                    ElementApplicationRejectionReason.InvalidTarget);
            }

            ElementAttachmentRuntime attachmentRuntime = targetCombatant.ElementAttachments;
            if (attachmentRuntime == null)
            {
                return ElementApplicationResult.Rejected(
                    _request,
                    ElementApplicationRejectionReason.MissingAttachmentOwner);
            }

            return attachmentRuntime.ResolveAndApply(_request);
        }
    }
}
