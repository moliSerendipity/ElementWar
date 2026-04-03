using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Foundation.Events
{
    /// <summary>
    /// 强类型同步事件总线。
    /// 只用于广播“已提交事实”，不承载同帧主控制流。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameEventBus : SingletonBehaviour<GameEventBus>
    {
        private readonly Dictionary<Type, Delegate> handlersByEventType = new();

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            handlersByEventType.Clear();
            base.OnDestroy();
        }

        /// <summary>
        /// 订阅指定类型的事件。
        /// </summary>
        public void Subscribe<TEvent>(Action<TEvent> _handler)
            where TEvent : struct
        {
            if (_handler == null)
            {
                return;
            }

            Type eventType = typeof(TEvent);
            if (handlersByEventType.TryGetValue(eventType, out Delegate existingDelegate))
            {
                handlersByEventType[eventType] = Delegate.Combine(existingDelegate, _handler);
                return;
            }

            handlersByEventType.Add(eventType, _handler);
        }

        /// <summary>
        /// 取消订阅指定类型的事件。
        /// </summary>
        public void Unsubscribe<TEvent>(Action<TEvent> _handler)
            where TEvent : struct
        {
            if (_handler == null)
            {
                return;
            }

            Type eventType = typeof(TEvent);
            if (!handlersByEventType.TryGetValue(eventType, out Delegate existingDelegate))
            {
                return;
            }

            Delegate updatedDelegate = Delegate.Remove(existingDelegate, _handler);
            if (updatedDelegate == null)
            {
                handlersByEventType.Remove(eventType);
                return;
            }

            handlersByEventType[eventType] = updatedDelegate;
        }

        /// <summary>
        /// 同步发布一个已提交事实事件。
        /// </summary>
        public void Publish<TEvent>(TEvent _eventData)
            where TEvent : struct
        {
            if (handlersByEventType.TryGetValue(typeof(TEvent), out Delegate handlerDelegate) &&
                handlerDelegate is Action<TEvent> typedHandler)
            {
                typedHandler.Invoke(_eventData);
            }

            DebugEventTap.Record(_eventData);
        }
    }
}
