using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Foundation.Runtime
{
    /// <summary>
    /// 固定步长运行时调度器。
    /// 用于驱动 AI 感知、AreaEffect、低频扫描等不需要逐帧刷新的子系统。
    /// 不替代 Character / Weapon 这类高频主链的直接推进。
    /// </summary>
    public sealed class RuntimeTickScheduler : SingletonBehaviour<RuntimeTickScheduler>
    {
        /// <summary>
        /// 通道步长统一量化到毫秒，避免直接用 float 作为字典键时出现精度漂移。
        /// </summary>
        private const int MinIntervalMilliseconds = 10;

        private sealed class TickChannel
        {
            public TickChannel(int _intervalMilliseconds)
            {
                IntervalMilliseconds = Mathf.Max(MinIntervalMilliseconds, _intervalMilliseconds);
                IntervalSeconds = IntervalMilliseconds / 1000f;
                NextTickTime = Time.time + IntervalSeconds;
            }

            public int IntervalMilliseconds { get; }
            public float IntervalSeconds { get; }
            public float NextTickTime { get; set; }
            public List<Action<float>> Handlers { get; } = new();
        }

        private readonly Dictionary<int, TickChannel> channelsByIntervalMs = new();

        private void Update()
        {
            if (channelsByIntervalMs.Count == 0)
            {
                return;
            }

            float currentTime = Time.time;
            foreach (TickChannel channel in channelsByIntervalMs.Values)
            {
                if (currentTime < channel.NextTickTime)
                {
                    continue;
                }

                // 使用 while 可以在长帧后按通道步长追平下一次触发时间，
                // 避免简单写成 currentTime + interval 导致调度相位持续漂移。
                while (channel.NextTickTime <= currentTime)
                {
                    channel.NextTickTime += channel.IntervalSeconds;
                }

                // 复制一份快照再回调，避免订阅者在回调过程中增删订阅时破坏当前遍历。
                Action<float>[] handlerSnapshot = channel.Handlers.ToArray();
                for (int i = 0; i < handlerSnapshot.Length; i++)
                {
                    handlerSnapshot[i]?.Invoke(currentTime);
                }
            }
        }

        /// <summary>
        /// 以固定步长注册一个回调。
        /// 相同步长的回调会被聚合到同一个调度通道。
        /// </summary>
        public void Subscribe(float _interval, Action<float> _handler)
        {
            if (_handler == null)
            {
                return;
            }

            int intervalMilliseconds = NormalizeIntervalMilliseconds(_interval);
            if (!channelsByIntervalMs.TryGetValue(intervalMilliseconds, out TickChannel channel))
            {
                channel = new TickChannel(intervalMilliseconds);
                channelsByIntervalMs.Add(intervalMilliseconds, channel);
            }

            if (!channel.Handlers.Contains(_handler))
            {
                channel.Handlers.Add(_handler);
            }
        }

        /// <summary>
        /// 取消固定步长回调。
        /// </summary>
        public void Unsubscribe(float _interval, Action<float> _handler)
        {
            if (_handler == null)
            {
                return;
            }

            int intervalMilliseconds = NormalizeIntervalMilliseconds(_interval);
            if (!channelsByIntervalMs.TryGetValue(intervalMilliseconds, out TickChannel channel))
            {
                return;
            }

            channel.Handlers.Remove(_handler);
            if (channel.Handlers.Count == 0)
            {
                channelsByIntervalMs.Remove(intervalMilliseconds);
            }
        }

        private static int NormalizeIntervalMilliseconds(float _interval)
        {
            return Mathf.Max(MinIntervalMilliseconds, Mathf.RoundToInt(_interval * 1000f));
        }
    }
}
