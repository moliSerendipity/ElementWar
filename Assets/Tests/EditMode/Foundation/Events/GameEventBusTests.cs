using System;
using Game.Foundation.Events;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.Foundation.Events
{
    /// <summary>
    /// 验证事件总线不依赖场景和帧循环的同步分发契约。
    /// </summary>
    public sealed class GameEventBusTests
    {
        private GameObject eventBusGameObject;
        private GameEventBus eventBus;

        /// <summary>
        /// 为每个用例创建独立且不启用的事件总线，避免依赖全局单例状态。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            eventBusGameObject = new GameObject(nameof(GameEventBusTests));
            eventBusGameObject.SetActive(false);
            eventBus = eventBusGameObject.AddComponent<GameEventBus>();
        }

        /// <summary>
        /// 销毁用例创建的临时对象，避免订阅或 Unity 对象跨用例残留。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (eventBusGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(eventBusGameObject);
            }

            eventBus = null;
            eventBusGameObject = null;
        }

        /// <summary>
        /// 已订阅处理器应在发布返回前恰好收到一次原始载荷。
        /// </summary>
        [Test]
        public void PublishInvokesMatchingHandlerSynchronouslyWithPayload()
        {
            int invocationCount = 0;
            int receivedValue = 0;
            eventBus.Subscribe<TestEvent>(_eventData =>
            {
                invocationCount++;
                receivedValue = _eventData.Value;
            });

            eventBus.Publish(new TestEvent(42));

            Assert.That(invocationCount, Is.EqualTo(1));
            Assert.That(receivedValue, Is.EqualTo(42));
        }

        /// <summary>
        /// 发布某种事件时不应调用其他事件类型的处理器。
        /// </summary>
        [Test]
        public void PublishInvokesOnlyHandlersForMatchingEventType()
        {
            int matchingInvocationCount = 0;
            int otherInvocationCount = 0;
            eventBus.Subscribe<TestEvent>(_ => matchingInvocationCount++);
            eventBus.Subscribe<OtherTestEvent>(_ => otherInvocationCount++);

            eventBus.Publish(new TestEvent(7));

            Assert.That(matchingInvocationCount, Is.EqualTo(1));
            Assert.That(otherInvocationCount, Is.EqualTo(0));
        }

        /// <summary>
        /// 处理器退订后不应再收到同类型事件。
        /// </summary>
        [Test]
        public void UnsubscribePreventsLaterDelivery()
        {
            int invocationCount = 0;
            Action<TestEvent> handler = _ => invocationCount++;
            eventBus.Subscribe(handler);

            eventBus.Unsubscribe(handler);
            eventBus.Publish(new TestEvent(1));

            Assert.That(invocationCount, Is.EqualTo(0));
        }

        /// <summary>
        /// 没有匹配订阅时发布事件应安全返回。
        /// </summary>
        [Test]
        public void PublishWithoutSubscribersDoesNotThrow()
        {
            Assert.DoesNotThrow(() => eventBus.Publish(new TestEvent(9)));
        }

        /// <summary>
        /// 提供用于验证载荷透传的测试事件。
        /// </summary>
        private readonly struct TestEvent
        {
            /// <summary>
            /// 创建具有可断言载荷的事件。
            /// </summary>
            /// <param name="_value">待透传的整数载荷。</param>
            public TestEvent(int _value)
            {
                Value = _value;
            }

            /// <summary>
            /// 获取待透传的整数载荷。
            /// </summary>
            public int Value { get; }
        }

        /// <summary>
        /// 提供用于验证事件类型隔离的第二种测试事件。
        /// </summary>
        private readonly struct OtherTestEvent
        {
        }
    }
}
