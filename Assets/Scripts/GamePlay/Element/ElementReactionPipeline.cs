using Game.Definition.Combat;
using Game.Definition.Element;

namespace Game.Gameplay.Element
{
    /// <summary>
    /// 同一次命中的元素应用与反应唯一生产入口；双请求固定先处理弹药、再处理技能。
    /// </summary>
    public static class ElementReactionPipeline
    {
        /// <summary>处理一个已经冻结来源、执行和目标身份的元素应用。</summary>
        /// <param name="_application">要交给目标附着运行时处理的请求。</param>
        /// <returns>成功触发反应时返回反应事实；普通附着、刷新或拒绝均返回默认值。</returns>
        public static ElementReactionResult ResolveAndApply(
            in ElementApplicationRequest _application)
        {
            if (TryGetTargetRuntime(_application, out ElementAttachmentRuntime targetRuntime) == false ||
                targetRuntime.HasCommittedReaction(_application.ExecutionId, _application.TargetId))
            {
                return default;
            }

            TryProcessApplication(_application, out ElementReactionResult result);
            return result;
        }

        /// <summary>按弹药、技能顺序处理同一次命中的两个元素应用，并在首次反应后停止。</summary>
        /// <param name="_ammoApplication">固定先处理的弹药元素请求。</param>
        /// <param name="_skillApplication">仅在弹药未形成终态时处理的技能元素请求。</param>
        /// <returns>成功触发反应时返回反应事实；没有反应时返回默认值。</returns>
        public static ElementReactionResult ResolveAndApply(
            in ElementApplicationRequest _ammoApplication,
            in ElementApplicationRequest _skillApplication)
        {
            // 双请求必须先确认属于同一次命中，避免弹药写回后才发现技能请求不一致。
            if (DescribeSameHit(_ammoApplication, _skillApplication) == false ||
                TryGetTargetRuntime(_ammoApplication, out ElementAttachmentRuntime targetRuntime) == false ||
                targetRuntime.HasCommittedReaction(
                    _ammoApplication.ExecutionId,
                    _ammoApplication.TargetId))
            {
                return default;
            }

            // 弹药拒绝或触发反应都会终止本次命中；只有普通附着/刷新才继续技能阶段。
            if (TryProcessApplication(_ammoApplication, out ElementReactionResult result))
            {
                return result;
            }

            TryProcessApplication(_skillApplication, out result);
            return result;
        }

        /// <summary>按无序元素对查询首版固定反应类型。</summary>
        public static bool TryResolveReactionType(
            ElementType _firstElement,
            ElementType _secondElement,
            out ElementReactionType _reactionType)
        {
            _reactionType = ElementReactionType.None;

            // 先按枚举值排序，下面每个无序元素对只需要保留一个分支。
            if ((int)_firstElement > (int)_secondElement)
            {
                ElementType temporary = _firstElement;
                _firstElement = _secondElement;
                _secondElement = temporary;
            }

            if (_firstElement == ElementType.Fire)
            {
                switch (_secondElement)
                {
                    case ElementType.Water:
                        _reactionType = ElementReactionType.Vaporize;
                        return true;
                    case ElementType.Electric:
                        _reactionType = ElementReactionType.Overload;
                        return true;
                    case ElementType.Ice:
                        _reactionType = ElementReactionType.Melt;
                        return true;
                }
            }
            else if (_firstElement == ElementType.Water)
            {
                switch (_secondElement)
                {
                    case ElementType.Electric:
                        _reactionType = ElementReactionType.ElectroCharged;
                        return true;
                    case ElementType.Ice:
                        _reactionType = ElementReactionType.Freeze;
                        return true;
                }
            }
            else if (_firstElement == ElementType.Electric && _secondElement == ElementType.Ice)
            {
                _reactionType = ElementReactionType.Superconduct;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 返回 true 表示当前阶段已经形成终态：低层拒绝、提交失败或成功反应都不能继续下一阶段。
        /// </summary>
        private static bool TryProcessApplication(
            in ElementApplicationRequest _application,
            out ElementReactionResult _result)
        {
            _result = default;
            ElementApplicationResult applicationResult =
                ElementApplicationResolver.ResolveAndApply(_application);

            if (applicationResult.Status == ElementApplicationResolutionStatus.Rejected)
            {
                return true;
            }

            if (applicationResult.RequiresReaction == false)
            {
                return false;
            }

            // 反应类型确定后，目标 Runtime 仍要原子重验版本、间隔和执行去重。
            ElementAttachmentSnapshot comingAttachment = applicationResult.Attachment;
            if (TryResolveReactionType(
                    comingAttachment.Element,
                    _application.Source.Element,
                    out ElementReactionType reactionType) == false ||
                TryGetTargetRuntime(_application, out ElementAttachmentRuntime targetRuntime) == false ||
                targetRuntime.TryCommitReaction(
                    _application,
                    comingAttachment,
                    out ElementAttachmentSnapshot consumedAttachment) == false)
            {
                return true;
            }

            // 第二元素请求是唯一反应归因，已有附着只作为被消费的反应输入保存。
            _result = ElementReactionResult.Triggered(
                reactionType,
                consumedAttachment,
                _application);
            return true;
        }

        private static bool DescribeSameHit(
            in ElementApplicationRequest _first,
            in ElementApplicationRequest _second)
        {
            return _first.Source != null
                && _second.Source != null
                && _first.TargetCombatant != null
                && _first.TargetCombatant == _second.TargetCombatant
                && _first.TargetId == _second.TargetId
                && _first.ExecutionId == _second.ExecutionId
                && _first.ApplicationTime.Equals(_second.ApplicationTime);
        }

        private static bool TryGetTargetRuntime(
            in ElementApplicationRequest _application,
            out ElementAttachmentRuntime _targetRuntime)
        {
            _targetRuntime = _application.TargetCombatant != null
                ? _application.TargetCombatant.ElementAttachments
                : null;
            return _application.Source != null && _targetRuntime != null;
        }
    }
}
