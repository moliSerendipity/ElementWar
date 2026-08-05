using System;
using System.Collections;
using Game.Foundation.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode.Foundation.Runtime
{
    /// <summary>
    /// 验证运行时调度器只能由真实 PlayMode 帧推进，并在退订后停止分发。
    /// </summary>
    public sealed class RuntimeTickSchedulerPlayModeTests
    {
        private const float TickIntervalSeconds = 0.01f;
        private const float TickTimeoutSeconds = 1f;
        private const float UnsubscribeObservationSeconds = 0.05f;

        private GameObject schedulerGameObject;
        private RuntimeTickScheduler scheduler;
        private Action<float> tickHandler;
        private bool ownsSchedulerGameObject;

        /// <summary>
        /// 在确认不存在来源不明的单例后，创建本用例独占的调度器对象。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            Assert.That(
                RuntimeTickScheduler.Instance,
                Is.Null,
                "PlayMode test refuses to replace or destroy an existing RuntimeTickScheduler instance.");

            schedulerGameObject = new GameObject(nameof(RuntimeTickSchedulerPlayModeTests));
            ownsSchedulerGameObject = true;
            scheduler = schedulerGameObject.AddComponent<RuntimeTickScheduler>();

            Assert.That(RuntimeTickScheduler.Instance, Is.SameAs(scheduler));
        }

        /// <summary>
        /// 订阅回调应在真实帧推进后触发，退订后即使继续推进帧也不再触发。
        /// 所有等待和超时均使用 realtime，不改变 <see cref="Time.timeScale"/>。
        /// </summary>
        /// <returns>由 Unity Test Framework 推进的 PlayMode 帧序列。</returns>
        [UnityTest]
        public IEnumerator SubscribedHandlerRunsAfterFrameAdvancementAndStopsAfterUnsubscribe()
        {
            int invocationCount = 0;
            tickHandler = _ => invocationCount++;
            scheduler.Subscribe(TickIntervalSeconds, tickHandler);

            Assert.That(invocationCount, Is.Zero, "Subscribe must not invoke the handler synchronously.");

            float tickDeadline = Time.realtimeSinceStartup + TickTimeoutSeconds;
            while (invocationCount == 0 && Time.realtimeSinceStartup < tickDeadline)
            {
                yield return null;
            }

            Assert.That(
                invocationCount,
                Is.GreaterThan(0),
                "RuntimeTickScheduler did not invoke the subscribed handler before the realtime timeout.");

            scheduler.Unsubscribe(TickIntervalSeconds, tickHandler);
            int invocationCountAfterUnsubscribe = invocationCount;
            float observationDeadline = Time.realtimeSinceStartup + UnsubscribeObservationSeconds;
            while (Time.realtimeSinceStartup < observationDeadline)
            {
                yield return null;
            }

            Assert.That(
                invocationCount,
                Is.EqualTo(invocationCountAfterUnsubscribe),
                "RuntimeTickScheduler invoked a handler after it was unsubscribed.");
        }

        /// <summary>
        /// 仅退订并销毁本用例创建的对象；若设置阶段发现已有实例则不清理该未知对象。
        /// </summary>
        /// <returns>等待延迟销毁完成的一帧。</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (scheduler != null && tickHandler != null)
            {
                scheduler.Unsubscribe(TickIntervalSeconds, tickHandler);
            }

            tickHandler = null;
            scheduler = null;

            if (ownsSchedulerGameObject && schedulerGameObject != null)
            {
                UnityEngine.Object.Destroy(schedulerGameObject);
            }

            schedulerGameObject = null;

            if (!ownsSchedulerGameObject)
            {
                yield break;
            }

            ownsSchedulerGameObject = false;
            yield return null;

            Assert.That(
                RuntimeTickScheduler.Instance,
                Is.Null,
                "The RuntimeTickScheduler instance created by the test was not cleared after destruction.");
        }
    }
}
