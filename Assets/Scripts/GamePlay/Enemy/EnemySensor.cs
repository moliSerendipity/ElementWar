using UnityEngine;

namespace Game.Gameplay.Enemy
{
    /// <summary>
    /// 敌人感知系统。
    ///
    /// 职责：
    /// 1. 以固定间隔扫描检测范围内的潜在目标
    /// 2. 对候选目标做 LOS（视线）校验
    /// 3. 维护当前目标引用与目标记忆（短时脱离视线不立即丢失目标）
    ///
    /// 计时策略：
    /// 自管理计时器，由 EnemyBrain 每帧调用 TryTick 驱动。
    /// 每个 Sensor 实例有独立的 nextScanTime，避免共享 channel 导致
    /// 大量敌人在同一帧集中扫描。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemySensor : MonoBehaviour
    {
        #region Inspector

        [Header("Detection")]
        [Tooltip("用于检测潜在目标的物理层。通常只包含玩家角色所在层。")]
        [SerializeField] private LayerMask targetDetectionMask;

        [Tooltip("LOS 射线会被哪些层遮挡。通常包含场景几何体但不包含目标本身。")]
        [SerializeField] private LayerMask losObstructionMask;

        [Header("Debug (Read Only)")]
        [SerializeField] private Transform currentTarget;
        [SerializeField] private bool hasLineOfSight;
        [SerializeField] private float lastSeenTime;
        [SerializeField] private Vector3 lastKnownPosition;

        #endregion

        #region Runtime

        /// <summary>自管理的下次扫描时间，每个实例独立。</summary>
        private float nextScanTime;

        /// <summary>物理扫描的碰撞体缓冲区，避免每次 Tick 分配。</summary>
        private readonly Collider[] scanBuffer = new Collider[16];

        /// <summary>LOS 射线起点的高度偏移，从脚底抬到大约胸口位置。</summary>
        private const float EyeHeightOffset = 1.2f;

        private EnemyStat enemyStat;

        #endregion

        #region Public Accessors

        /// <summary>当前锁定的目标。为 null 表示无目标。</summary>
        public Transform CurrentTarget => currentTarget;

        /// <summary>当前是否对目标有直接视线。</summary>
        public bool HasLineOfSight => hasLineOfSight;

        /// <summary>目标最后一次被看到的世界坐标。用于追击最后已知位置。</summary>
        public Vector3 LastKnownPosition => lastKnownPosition;

        /// <summary>是否存在有效目标（包括在记忆窗口内暂时看不见的目标）。</summary>
        public bool HasTarget => currentTarget != null;

        #endregion

        #region Initialization

        /// <summary>
        /// 由 EnemyRoot 在初始化阶段注入 EnemyStat。
        /// </summary>
        public void Initialize(EnemyStat _enemyStat)
        {
            enemyStat = _enemyStat;
            // 初始扫描时间加随机偏移，让多个敌人的感知自然错开帧。
            nextScanTime = Time.time + Random.Range(0f, enemyStat.ScanInterval);
        }

        #endregion

        #region Tick

        /// <summary>
        /// 由 EnemyBrain 每帧调用。只有到了扫描间隔才执行真正的感知逻辑。
        /// </summary>
        public void TryTick(float _currentTime)
        {
            if (_currentTime < nextScanTime)
            {
                return;
            }

            nextScanTime = _currentTime + enemyStat.ScanInterval;
            ExecuteScan(_currentTime);
        }

        /// <summary>
        /// 执行一次完整的感知扫描。
        /// </summary>
        private void ExecuteScan(float _currentTime)
        {
            if (currentTarget != null)
            {
                // 已有目标：评估是否仍然有效。
                EvaluateExistingTarget(_currentTime);
            }
            else
            {
                // 无目标：尝试发现新目标。
                ScanForNewTarget(_currentTime);
            }
        }

        #endregion

        #region Target Evaluation

        /// <summary>
        /// 对已有目标进行持续评估：是否超出失联距离、是否仍有视线、记忆是否过期。
        /// </summary>
        private void EvaluateExistingTarget(float _currentTime)
        {
            // 目标被销毁。
            if (currentTarget == null)
            {
                ClearTarget();
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

            // 超出失联距离，立即放弃。
            if (distanceToTarget > enemyStat.LoseTargetRange)
            {
                ClearTarget();
                return;
            }

            bool canSee = CheckLineOfSight(currentTarget.position);
            if (canSee)
            {
                // 看得见：刷新记忆。
                hasLineOfSight = true;
                lastSeenTime = _currentTime;
                lastKnownPosition = currentTarget.position;
            }
            else
            {
                // 看不见：检查记忆是否过期。
                hasLineOfSight = false;
                if (_currentTime - lastSeenTime > enemyStat.TargetMemoryDuration)
                {
                    ClearTarget();
                }
            }
        }

        #endregion

        #region Target Scanning

        /// <summary>
        /// 在检测范围内扫描新目标。选择最近的可见候选。
        /// </summary>
        private void ScanForNewTarget(float _currentTime)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position, enemyStat.DetectRange, scanBuffer, targetDetectionMask, QueryTriggerInteraction.Ignore);

            if (hitCount <= 0)
            {
                return;
            }

            Transform bestCandidate = null;
            float bestDistanceSqr = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider candidate = scanBuffer[i];
                if (candidate == null || candidate.gameObject == gameObject)
                {
                    continue;
                }

                Vector3 candidatePosition = candidate.transform.position;
                float distanceSqr = (candidatePosition - transform.position).sqrMagnitude;

                // 跳过比当前最佳更远的候选，提前剪枝。
                if (distanceSqr >= bestDistanceSqr)
                {
                    continue;
                }

                if (CheckLineOfSight(candidatePosition) == false)
                {
                    continue;
                }

                bestCandidate = candidate.transform;
                bestDistanceSqr = distanceSqr;
            }

            if (bestCandidate != null)
            {
                currentTarget = bestCandidate;
                hasLineOfSight = true;
                lastSeenTime = _currentTime;
                lastKnownPosition = bestCandidate.position;
            }
        }

        #endregion

        #region Line of Sight

        /// <summary>
        /// 检查从自身眼睛高度到目标位置是否有直接视线。
        /// </summary>
        private bool CheckLineOfSight(Vector3 _targetPosition)
        {
            Vector3 eyePosition = transform.position + Vector3.up * EyeHeightOffset;
            Vector3 targetEyePosition = _targetPosition + Vector3.up * EyeHeightOffset;
            Vector3 toTarget = targetEyePosition - eyePosition;
            float distance = toTarget.magnitude;

            if (distance < 0.01f)
            {
                return true;
            }

            // 如果射线命中了遮挡层，说明没有视线。
            return Physics.Raycast(eyePosition, toTarget.normalized, distance,
                losObstructionMask, QueryTriggerInteraction.Ignore) == false;
        }

        #endregion

        #region Clear

        /// <summary>
        /// 强制清空当前目标与记忆。用于死亡或重置。
        /// </summary>
        public void ClearTarget()
        {
            currentTarget = null;
            hasLineOfSight = false;
            lastSeenTime = 0f;
            lastKnownPosition = Vector3.zero;
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 绿圈：检测范围。
            Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, enemyStat.DetectRange);

            // 黄圈：失联范围。
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, enemyStat.LoseTargetRange);

            if (currentTarget != null)
            {
                Vector3 eyePos = transform.position + Vector3.up * EyeHeightOffset;
                if (hasLineOfSight)
                {
                    // 绿线：有视线。
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(eyePos, currentTarget.position + Vector3.up * EyeHeightOffset);
                }
                else
                {
                    // 红线 + 红球：无视线，连接最后已知位置。
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(eyePos, lastKnownPosition + Vector3.up * EyeHeightOffset);
                    Gizmos.DrawWireSphere(lastKnownPosition, 0.3f);
                }
            }
        }
#endif

        #endregion
    }
}
