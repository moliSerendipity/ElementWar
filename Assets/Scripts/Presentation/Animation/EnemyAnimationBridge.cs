using Game.Foundation.Events;
using Game.Definition.Enemy;
using Game.Gameplay.Combat;
using Game.Gameplay.Combat.Events;
using Game.Gameplay.Enemy;
using UnityEngine;

namespace Game.Presentation.Enemy
{
    /// <summary>
    /// 敌人动画桥接（Presentation 层）。
    ///
    /// 职责：
    /// 1. 从 EnemyBrain 读取已提交状态，映射为 Animator 参数
    /// 2. 在攻击发起时用 EnemyAttackConfig.AnimationTriggerName 触发对应攻击动画
    /// 3. 攻击状态下将 Animator.speed 设为 EnemyStat.AttackSpeedMultiplier，
    ///    让动画与逻辑计时器始终同步
    /// 4. 订阅 DamageAppliedEvent，在 HitLayer（Additive）上触发受击动画
    ///
    /// Animator 参数约定：
    /// - MoveSpeed (float)：locomotion blend tree 驱动
    /// - IsDead (bool)：死亡状态
    /// - HitTrigger (trigger)：受击动画触发（HitLayer, Additive）
    /// - 攻击动画通过 EnemyAttackConfig.AnimationTriggerName 动态触发
    ///
    /// 约束：
    /// 不反向修改 Gameplay 任何运行时事实。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyAnimationBridge : MonoBehaviour
    {
        #region Inspector

        [Header("References")]
        [SerializeField] private EnemyBrain brain;
        [SerializeField] private EnemyStat enemyStat;
        [SerializeField] private HealthComponent healthComponent;
        [SerializeField] private EnemyAttack attack;
        [SerializeField] private Animator animator;

        [Header("Locomotion")]
        [Tooltip("MoveSpeed 参数的平滑过渡时间（秒）。避免速度突变导致动画跳帧。")]
        [SerializeField, Min(0f)] private float moveSpeedDampTime = 0.1f;

        #endregion

        #region Animator Hash Cache

        private static readonly int moveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int isDeadHash = Animator.StringToHash("IsDead");
        private static readonly int hitTriggerHash = Animator.StringToHash("HitTrigger");

        #endregion

        #region Runtime

        /// <summary>上一帧的攻击阶段，用于检测攻击发起的边沿。</summary>
        private EnemyAttackPhase previousAttackPhase;

        /// <summary>上一帧的 Brain 状态，用于检测死亡进入的边沿。</summary>
        private EnemyState previousBrainState;

        /// <summary>上一帧是否处于攻击状态，用于管理 Animator.speed 切换。</summary>
        private bool wasInAttackState;

        /// <summary>受击事件回调缓存，用于安全取消订阅。</summary>
        private System.Action<DamageAppliedEvent> onDamageAppliedHandler;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ResolveReferences();
            onDamageAppliedHandler = OnDamageApplied;
        }

        private void OnEnable()
        {
            GameEventBus eventBus = GameEventBus.Instance;
            if (eventBus != null)
            {
                eventBus.Subscribe(onDamageAppliedHandler);
            }
        }

        private void OnDisable()
        {
            GameEventBus eventBus = GameEventBus.Instance;
            if (eventBus != null)
            {
                eventBus.Unsubscribe(onDamageAppliedHandler);
            }
        }

        private void LateUpdate()
        {
            if (brain == null || brain.IsInitialized == false || animator == null)
            {
                return;
            }

            SyncLocomotion();
            SyncAttack();
            SyncAttackSpeed();
            SyncDeath();
        }

        #endregion

        #region Locomotion

        /// <summary>
        /// 同步移动速度到 Animator。使用 dampTime 平滑过渡。
        /// </summary>
        private void SyncLocomotion()
        {
            float moveSpeed = brain.CurrentMoveSpeed;
            animator.SetFloat(moveSpeedHash, moveSpeed, moveSpeedDampTime, Time.deltaTime);
        }

        #endregion

        #region Attack

        /// <summary>
        /// 检测攻击阶段变化。从 None → Windup 的边沿触发攻击动画。
        /// 使用 EnemyAttackConfig.AnimationTriggerName 作为 Trigger 名，支持多种攻击动画。
        /// </summary>
        private void SyncAttack()
        {
            EnemyAttackPhase currentPhase = brain.CurrentAttackPhase;

            // 从 None → Windup 的边沿：攻击刚发起，触发攻击动画。
            if (currentPhase == EnemyAttackPhase.Windup && previousAttackPhase == EnemyAttackPhase.None)
            {
                EnemyAttackConfig activeConfig = attack != null ? attack.ActiveConfig : null;
                if (activeConfig != null && string.IsNullOrEmpty(activeConfig.AnimationTriggerName) == false)
                {
                    animator.SetTrigger(activeConfig.AnimationTriggerName);
                }
            }

            previousAttackPhase = currentPhase;
        }

        /// <summary>
        /// 攻击状态下将 Animator.speed 设为攻速倍率，非攻击状态恢复 1.0。
        /// 攻击期间敌人停止移动，不会影响 locomotion 动画速度。
        /// </summary>
        private void SyncAttackSpeed()
        {
            bool isInAttackState = brain.CurrentState == EnemyState.Attack && brain.CurrentAttackPhase != EnemyAttackPhase.None;

            if (isInAttackState && wasInAttackState == false)
            {
                // 进入攻击：缩放动画速度。
                float speedMultiplier = enemyStat != null ? Mathf.Max(0.1f, enemyStat.AttackSpeedMultiplier) : 1f;
                animator.speed = speedMultiplier;
            }
            else if (isInAttackState == false && wasInAttackState)
            {
                // 离开攻击：恢复正常速度。
                animator.speed = 1f;
            }

            wasInAttackState = isInAttackState;
        }

        #endregion

        #region Death

        private void SyncDeath()
        {
            EnemyState currentBrainState = brain.CurrentState;

            // 死亡进入边沿。
            if (currentBrainState == EnemyState.Dead && previousBrainState != EnemyState.Dead)
            {
                animator.speed = 1f;
                animator.SetBool(isDeadHash, true);
            }

            previousBrainState = currentBrainState;
        }

        #endregion

        #region Hit Reaction

        /// <summary>
        /// 响应 DamageAppliedEvent。只在目标是自身时触发受击动画。
        /// </summary>
        private void OnDamageApplied(DamageAppliedEvent _event)
        {
            // 确认伤害目标是自身。
            if (_event.DamageResult.Target != healthComponent)
            {
                return;
            }

            // 死亡后不再播受击。
            if (brain != null && brain.CurrentState == EnemyState.Dead)
            {
                return;
            }

            if (animator != null)
            {
                animator.SetTrigger(hitTriggerHash);
            }
        }

        #endregion

        #region Reference Resolution

        private void ResolveReferences()
        {
            if (brain == null)
            {
                brain = GetComponentInParent<EnemyBrain>();
            }

            if (enemyStat == null)
            {
                enemyStat = GetComponentInParent<EnemyStat>();
            }

            if (healthComponent == null)
            {
                healthComponent = GetComponentInParent<HealthComponent>();
            }

            if (attack == null)
            {
                attack = GetComponentInParent<EnemyAttack>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif

        #endregion
    }
}
