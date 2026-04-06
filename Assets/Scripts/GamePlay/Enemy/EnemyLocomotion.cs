using UnityEngine;
using UnityEngine.AI;

namespace Game.Gameplay.Enemy
{
    /// <summary>
    /// 敌人移动系统。封装 NavMeshAgent，只暴露 Brain 需要的高层指令。
    ///
    /// 职责：
    /// 1. 接收 Brain 的移动指令（追击目标、停止、面朝目标）
    /// 2. 管理 NavMeshAgent 的目的地、速度、停止距离
    /// 3. 提供移动状态查询（是否已到达、当前速度等）
    ///
    /// 约束：
    /// 不做任何决策判断。Brain 说走就走，说停就停。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyLocomotion : MonoBehaviour
    {
        #region Inspector

        [Header("Debug")]
        [SerializeField] private float currentSpeed;
        [SerializeField] private bool hasReachedDestination;

        #endregion

        #region Runtime

        private NavMeshAgent agent;
        private EnemyStat enemyStat;

        #endregion

        #region Public Accessors

        /// <summary>当前实际移动速度。供 Brain 和 Animator 读取。</summary>
        public float CurrentSpeed => currentSpeed;

        /// <summary>是否已到达目的地（或足够接近停止距离）。</summary>
        public bool HasReachedDestination => hasReachedDestination;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 由 EnemyRoot 在初始化阶段注入 EnemyStat。
        /// </summary>
        public void Initialize(EnemyStat _enemyStat)
        {
            enemyStat = _enemyStat;
            agent.speed = 0f;
            agent.stoppingDistance = enemyStat.StopDistance;
        }

        #endregion

        #region Tick

        /// <summary>
        /// 每帧更新只读状态。移动本身由 NavMeshAgent 内部驱动。
        /// </summary>
        public void Tick()
        {
            agent.stoppingDistance = enemyStat.StopDistance;

            currentSpeed = agent.velocity.magnitude;
            hasReachedDestination = EvaluateArrival();
        }

        #endregion

        #region Movement Commands

        /// <summary>
        /// 设置追击目标位置。NavMeshAgent 会自动寻路。
        /// </summary>
        public void ChaseTarget(Vector3 _targetPosition)
        {
            if (agent.isOnNavMesh == false)
            {
                return;
            }

            agent.speed = enemyStat.ChaseSpeed;
            agent.stoppingDistance = enemyStat.StopDistance;
            agent.isStopped = false;
            agent.SetDestination(_targetPosition);
        }

        /// <summary>
        /// 立即停止移动。保持当前位置不变。
        /// </summary>
        public void StopMovement()
        {
            if (agent.isOnNavMesh == false)
            {
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();
        }

        /// <summary>
        /// 面朝目标位置做平滑转向。通常在攻击阶段调用，让敌人正对目标。
        /// </summary>
        public void FaceTarget(Vector3 _targetPosition, float _deltaTime)
        {
            Vector3 direction = _targetPosition - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation, enemyStat.TurnSharpness * _deltaTime);
        }

        /// <summary>
        /// 切换移动速度。用于在追击/巡逻之间切换。
        /// </summary>
        public void SetMoveSpeed(float _speed)
        {
            agent.speed = Mathf.Max(0f, _speed);
        }

        /// <summary>
        /// 完全禁用 NavMeshAgent（死亡时调用）。
        /// </summary>
        public void Disable()
        {
            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.enabled = false;
            }
        }

        #endregion

        #region Arrival Evaluation

        /// <summary>
        /// 判断是否已到达目的地。
        /// NavMeshAgent 在无路径或剩余距离小于停止距离时视为到达。
        /// </summary>
        private bool EvaluateArrival()
        {
            if (agent.isOnNavMesh == false || agent.pathPending)
            {
                return false;
            }

            return agent.remainingDistance <= agent.stoppingDistance && agent.hasPath == false;
        }

        #endregion
    }
}
