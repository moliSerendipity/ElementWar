using System.Collections.Generic;
using Game.Foundation.Events;
using Game.Gameplay.Combat;
using Game.Gameplay.Element;
using UnityEngine;

namespace Game.Presentation.HUD
{
    /// <summary>
    /// 开发期元素附着调试叠层；只消费已提交事件和只读快照，不参与 Gameplay 裁决。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ElementAttachmentDebugPresenter : MonoBehaviour
    {
        [SerializeField] private bool showOverlay = true;
        [SerializeField] private Vector2 overlayPosition = new(16f, 16f);
        [SerializeField, Min(160f)] private float overlayWidth = 320f;

        private readonly Dictionary<CombatantId, ElementAttachmentSnapshot> attachmentsByTargetId = new();
        private readonly List<ElementAttachmentSnapshot> orderedAttachments = new();
        private bool isSubscribed;

        /// <summary>调试叠层当前跟踪的有效附着数量。</summary>
        public int TrackedAttachmentCount => attachmentsByTargetId.Count;

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Update()
        {
            if (isSubscribed == false)
            {
                TrySubscribe();
            }
        }

        private void OnDisable()
        {
            GameEventBus eventBus = GameEventBus.Instance;
            if (isSubscribed && eventBus != null)
            {
                eventBus.Unsubscribe<ElementAttachmentChangedEvent>(OnElementAttachmentChanged);
            }

            isSubscribed = false;
            attachmentsByTargetId.Clear();
            orderedAttachments.Clear();
        }

        /// <summary>尝试读取指定目标在调试叠层中的当前附着。</summary>
        /// <param name="_targetId">当前目标生命周期身份。</param>
        /// <param name="_attachment">存在时返回叠层保存的只读快照。</param>
        /// <returns>叠层正在跟踪该目标时返回 <see langword="true"/>。</returns>
        public bool TryGetTrackedAttachment(
            CombatantId _targetId,
            out ElementAttachmentSnapshot _attachment)
        {
            return attachmentsByTargetId.TryGetValue(_targetId, out _attachment);
        }

        private void TrySubscribe()
        {
            GameEventBus eventBus = GameEventBus.Instance;
            if (isSubscribed || eventBus == null)
            {
                return;
            }

            eventBus.Subscribe<ElementAttachmentChangedEvent>(OnElementAttachmentChanged);
            isSubscribed = true;
            RebuildFromRuntimeOwners();
        }

        private void RebuildFromRuntimeOwners()
        {
            attachmentsByTargetId.Clear();
            ElementAttachmentRuntime[] runtimes = FindObjectsOfType<ElementAttachmentRuntime>(true);
            for (int i = 0; i < runtimes.Length; i++)
            {
                if (runtimes[i] != null &&
                    runtimes[i].TryGetPrimaryAttachment(out ElementAttachmentSnapshot attachment))
                {
                    attachmentsByTargetId[attachment.TargetId] = attachment;
                }
            }
        }

        private void OnElementAttachmentChanged(ElementAttachmentChangedEvent _eventData)
        {
            if (_eventData.CurrentAttachment.IsValid)
            {
                attachmentsByTargetId[_eventData.TargetId] = _eventData.CurrentAttachment;
                return;
            }

            attachmentsByTargetId.Remove(_eventData.TargetId);
        }

        private void OnGUI()
        {
            if (showOverlay == false ||
                attachmentsByTargetId.Count == 0 ||
                (Application.isEditor == false && Debug.isDebugBuild == false))
            {
                return;
            }

            BuildOrderedSnapshot();
            float overlayHeight = 34f + orderedAttachments.Count * 24f;
            GUILayout.BeginArea(
                new Rect(overlayPosition.x, overlayPosition.y, overlayWidth, overlayHeight),
                GUI.skin.box);
            GUILayout.Label("Element Attachments");

            for (int i = 0; i < orderedAttachments.Count; i++)
            {
                ElementAttachmentSnapshot attachment = orderedAttachments[i];
                string targetName = attachment.TargetCombatant != null
                    ? attachment.TargetCombatant.name
                    : $"Target {attachment.TargetId}";
                GUILayout.Label(
                    $"{targetName}: {attachment.Element}  {attachment.GetRemainingSeconds(Time.time):0.0}s");
            }

            GUILayout.EndArea();
        }

        private void BuildOrderedSnapshot()
        {
            orderedAttachments.Clear();
            foreach (ElementAttachmentSnapshot attachment in attachmentsByTargetId.Values)
            {
                orderedAttachments.Add(attachment);
            }

            orderedAttachments.Sort(CompareTargetId);
        }

        private static int CompareTargetId(
            ElementAttachmentSnapshot _left,
            ElementAttachmentSnapshot _right)
        {
            return _left.TargetId.Value.CompareTo(_right.TargetId.Value);
        }
    }
}
