using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.Enemy
{
    /// <summary>
    /// 敌人状态。
    /// </summary>
    public enum EnemyState
    {
        /// <summary>无目标，原地待机。</summary>
        Idle,
        /// <summary>有目标，正在追击。</summary>
        Chase,
        /// <summary>处于攻击距离内，正在执行或等待攻击。</summary>
        Attack,
        /// <summary>生命值归零，已死亡。</summary>
        Dead,
    }

    /// <summary>
    /// 敌人行为核心。
    ///
    /// 职责：
    /// 1. 维护状态机（Idle → Chase → Attack → Dead）
    /// 2. 协调 Sensor / Locomotion / Attack 三个子系统
    /// 3. 驱动 Sensor 的低频感知 tick
    ///
    /// Brain 不知道具体使用哪种攻击——它只提供距离，EnemyAttack 内部根据距离和权重选择。
    /// 攻击状态能否进入，也统一由 EnemyAttack 判断，不再维护第二套 AttackRange 真相源。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyBrain : MonoBehaviour
    {
        #region Inspector

        [Header("References")]
        [SerializeField] private HealthComponent healthComponent;
        [SerializeField] private EnemySensor sensor;
        [SerializeField] private EnemyLocomotion locomotion;
        [SerializeField] private EnemyAttack attack;

        [Header("State (Read Only)")]
        [SerializeField] private EnemyState currentState = EnemyState.Idle;

        #endregion

        #region Runtime

        private EnemyStat enemyStat;

        #endregion

        #region Public Accessors

        /// <summary>当前状态。供 AnimationBridge 等表现层只读。</summary>
        public EnemyState CurrentState => currentState;

        /// <summary>当前追击/攻击目标。</summary>
        public Transform CurrentTarget => sensor != null ? sensor.CurrentTarget : null;

        /// <summary>当前移动速度。供 Animator 驱动 locomotion blend。</summary>
        public float CurrentMoveSpeed => locomotion != null ? locomotion.CurrentSpeed : 0f;

        /// <summary>当前攻击阶段。供 Animator 驱动攻击动画。</summary>
        public EnemyAttackPhase CurrentAttackPhase => attack != null ? attack.CurrentPhase : EnemyAttackPhase.None;

        /// <summary>是否已初始化。</summary>
        public bool IsInitialized { get; private set; }

        #endregion

        #region Initialization

        /// <summary>
        /// 由 EnemyRoot 在 Start 阶段调用。
        /// </summary>
        public void Initialize(EnemyStat _enemyStat)
        {
            enemyStat = _enemyStat;
            ResolveReferences();

            currentState = EnemyState.Idle;
            IsInitialized = enemyStat != null && sensor != null && locomotion != null && attack != null;
        }

        #endregion

        #region Main Tick

        /// <summary>
        /// 由 EnemyRoot.Update 每帧调用。
        /// </summary>
        public void Tick(float _deltaTime)
        {
            if (IsInitialized == false)
            {
                return;
            }

            // Sensor 内部自管理计时器，只有到了间隔才真正扫描。
            sensor.TryTick(Time.time);

            // 死亡检测优先级最高。
            if (currentState != EnemyState.Dead && CheckDeathTransition())
            {
                return;
            }

            switch (currentState)
            {
                case EnemyState.Idle:
                    TickIdle();
                    break;
                case EnemyState.Chase:
                    TickChase(_deltaTime);
                    break;
                case EnemyState.Attack:
                    TickAttack(_deltaTime);
                    break;
                case EnemyState.Dead:
                    break;
            }

            locomotion.Tick();
        }

        #endregion

        #region State: Idle

        private void TickIdle()
        {
            if (sensor.HasTarget)
            {
                TransitionTo(EnemyState.Chase);
            }
        }

        #endregion

        #region State: Chase

        private void TickChase(float _deltaTime)
        {
            if (sensor.HasTarget == false)
            {
                TransitionTo(EnemyState.Idle);
                return;
            }

            Transform target = sensor.CurrentTarget;
            float distanceToTarget = Vector3.Distance(transform.position, target.position);

            // 只要当前距离下存在至少一种可用攻击，且攻击已冷却，就进入 Attack。
            if (attack.IsReady && attack.CanEnterAttackState(distanceToTarget))
            {
                TransitionTo(EnemyState.Attack);
                return;
            }

            Vector3 chaseDestination = sensor.HasLineOfSight
                ? target.position
                : sensor.LastKnownPosition;

            locomotion.ChaseTarget(chaseDestination);
        }

        #endregion

        #region State: Attack

        private void TickAttack(float _deltaTime)
        {
            // 目标丢失。
            if (sensor.HasTarget == false)
            {
                TransitionTo(EnemyState.Idle);
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, sensor.CurrentTarget.position);
            bool canEnterAttackState = attack.CanEnterAttackState(distanceToTarget);

            // 当前距离下已经没有任何可用攻击，回到 Chase 重新贴近或追击。
            if (attack.IsAttacking == false && canEnterAttackState == false)
            {
                TransitionTo(EnemyState.Chase);
                return;
            }

            // 未在攻击中：尝试发起或等待冷却。
            if (attack.IsAttacking == false)
            {
                if (attack.IsReady)
                {
                    // 面朝目标后发起攻击，传入距离让 EnemyAttack 自行选择攻击类型。
                    locomotion.FaceTarget(sensor.CurrentTarget.position, _deltaTime);

                    // 理论上 canEnterAttackState 为 true 时这里应能成功；若失败则退回 Chase，避免站桩。
                    if (attack.TryBeginAttack(distanceToTarget) == false)
                    {
                        TransitionTo(EnemyState.Chase);
                    }
                }
                else
                {
                    // 冷却中，仍面朝目标，等待下一次可用攻击窗口。
                    locomotion.FaceTarget(sensor.CurrentTarget.position, _deltaTime);
                }

                return;
            }

            // 攻击进行中：Windup 阶段持续追踪目标朝向。
            if (attack.IsInWindup)
            {
                locomotion.FaceTarget(sensor.CurrentTarget.position, _deltaTime);
            }

            attack.Tick(_deltaTime);

            // Tick 后如果攻击刚好结束，而当前距离下又没有任何可用攻击，则回 Chase。
            if (attack.IsAttacking == false && attack.CanEnterAttackState(distanceToTarget) == false)
            {
                TransitionTo(EnemyState.Chase);
            }
        }

        #endregion

        #region Death

        private bool CheckDeathTransition()
        {
            if (healthComponent == null || healthComponent.IsDead == false)
            {
                return false;
            }

            TransitionTo(EnemyState.Dead);
            return true;
        }

        #endregion

        #region State Transition

        private void TransitionTo(EnemyState _newState)
        {
            if (currentState == _newState)
            {
                return;
            }

            EnemyState previousState = currentState;

            // --- 旧状态退出 ---
            switch (previousState)
            {
                case EnemyState.Chase:
                    locomotion.StopMovement();
                    break;

                case EnemyState.Attack:
                    if (attack.IsAttacking)
                    {
                        attack.CancelAttack();
                    }
                    break;
            }

            currentState = _newState;

            // --- 新状态进入 ---
            switch (_newState)
            {
                case EnemyState.Idle:
                    locomotion.StopMovement();
                    break;

                case EnemyState.Chase:
                    locomotion.SetMoveSpeed(enemyStat.ChaseSpeed);
                    break;

                case EnemyState.Attack:
                    locomotion.StopMovement();
                    break;

                case EnemyState.Dead:
                    OnEnterDead();
                    break;
            }
        }

        /// <summary>
        /// 进入死亡状态时禁用所有交互与移动能力。
        /// </summary>
        private void OnEnterDead()
        {
            attack.CancelAttack();
            locomotion.Disable();
            sensor.ClearTarget();

            GetComponent<Rigidbody>().isKinematic = true;

            Collider[] colliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        #endregion

        #region Reference Resolution

        private void ResolveReferences()
        {
            if (healthComponent == null)
            {
                healthComponent = GetComponentInParent<HealthComponent>();
            }

            if (sensor == null)
            {
                sensor = GetComponent<EnemySensor>();
            }

            if (locomotion == null)
            {
                locomotion = GetComponent<EnemyLocomotion>();
            }

            if (attack == null)
            {
                attack = GetComponent<EnemyAttack>();
            }
        }

        #endregion
    }
}
