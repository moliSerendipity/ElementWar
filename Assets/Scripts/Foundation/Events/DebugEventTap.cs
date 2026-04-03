using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Foundation.Events
{
    /// <summary>
    /// 最近事件调试记录器。
    /// 用于联调时查看事件总线最近广播过的事实事件，不参与任何玩法逻辑。
    /// </summary>
    public sealed class DebugEventTap : SingletonBehaviour<DebugEventTap>
    {
        [SerializeField, Min(8)] private int maxRecordCount = 64;

        private readonly Queue<string> recentEventLogs = new();

        public IReadOnlyCollection<string> RecentEventLogs => recentEventLogs;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        /// <summary>
        /// 记录一条事件摘要。
        /// 若当前场景没有挂载调试记录器，则静默跳过。
        /// </summary>
        public static void Record<TEvent>(TEvent _eventData)
            where TEvent : struct
        {
            Instance?.RecordInternal(_eventData);
        }

        private void RecordInternal<TEvent>(TEvent _eventData)
            where TEvent : struct
        {
            string logText = $"[{DateTime.Now:HH:mm:ss.fff}] {typeof(TEvent).Name}: {_eventData}";
            recentEventLogs.Enqueue(logText);

            while (recentEventLogs.Count > maxRecordCount)
            {
                recentEventLogs.Dequeue();
            }
        }
    }
}
