using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AICharacterInputSource : MonoBehaviour, IInputSource
{
    private NavMeshAgent navMeshAgent;
    private InputFrame simulatedInputFrame;

    [Header("AI 参数配置")]
    public float followDistance = 3.0f;                                 // 跟随距离
    public float engageDistance = 15.0f;                                // 警戒开火距离
    public LayerMask enemyLayer;                                        // 敌人图层
    public LayerMask obstacleLayer;                                     // 障碍物图层，用于开火视线判断

    private Transform currentTarget;                                    // 当前交火目标

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        // 由 CharacterMotor 处理位置更新
        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;

        simulatedInputFrame = new InputFrame();
    }

    private void OnEnable()
    {
        // 激活AI时，同时激活寻路代理组件
        if (navMeshAgent != null)
            navMeshAgent.enabled = true;
    }

    private void OnDisable()
    {
        // 被玩家接管时，彻底沉睡AI，同时关闭寻路组件，防止它在后台偷偷与玩家的操作抢夺角色的控制权！
        if (navMeshAgent != null)
            navMeshAgent.enabled = false;
        ResetInputFrame();
    }

    /// <summary>
    /// 获取当前帧输入数据
    /// </summary>
    /// <returns>当前帧的输入数据</returns>
    public InputFrame GetInputFrame()
    {
        ResetInputFrame();
        TPSCharacterController playerCC = TeamController.Instance.GetCurrentPlayer();
        // 没有玩家可跟随，彻底挂机
        if (playerCC == null)
            return simulatedInputFrame;

        Vector3 playerPos = playerCC.transform.position;
        Vector3 myPos = transform.position;

        // 与真正的实体位置保持同步，才能计算正确路线
        navMeshAgent.nextPosition = myPos;

        // 交火检测
        FindClosestEnemy();

        bool isEngaging = false;
        if (currentTarget != null)
        {
            Vector3 toEnemy = currentTarget.position - myPos;
            float distToEnemy = toEnemy.magnitude;

            // 在射程内，且中间没有墙壁遮挡
            if (distToEnemy <= engageDistance && !Physics.Raycast(myPos + Vector3.up, toEnemy.normalized, distToEnemy, obstacleLayer))
            {
                isEngaging = true;

                // 瞄准,伪装鼠标移动
                simulatedInputFrame.look = new Vector2(toEnemy.x, toEnemy.z).normalized;
                simulatedInputFrame.aimButton.isHeld = true;  // AI自动开镜

                // 随机抖动避免无CD武器一直发射
                // 实际发射会受限于 Lua 层的 FireCooldown
                simulatedInputFrame.fireButton.isHeld = true;
            }
        }

        // 跟随寻路
        float distToPlayer = Vector3.Distance(myPos, playerPos);

        // 如果正在交火，且没脱离队伍太远，优先站桩射击或左右微调
        if (isEngaging && distToPlayer < engageDistance)
        {
            navMeshAgent.ResetPath();
            simulatedInputFrame.move = Vector2.zero;
        }
        else
        {
            // 距离主控玩家较远，需寻路赶上
            if (distToPlayer > followDistance)
            {
                navMeshAgent.SetDestination(playerPos);

                if (navMeshAgent.hasPath)
                {
                    // 获取期望速度的方向，把它降维成给底层动画机和状态机的 2D Input X/Y (WASD模拟)
                    Vector3 moveDir = navMeshAgent.desiredVelocity.normalized;
                    simulatedInputFrame.move = new Vector2(moveDir.x, moveDir.z); // 在底层它会根据相机平面再映射，在这里传绝对世界坐标映射

                    // 距离过远自动加速冲刺
                    simulatedInputFrame.sprintButton.isHeld = distToPlayer > 8f;
                }
            }
        }

        return simulatedInputFrame;
    }

    private void ResetInputFrame()
    {
        simulatedInputFrame.move = Vector2.zero;
        simulatedInputFrame.look = Vector2.zero;
        simulatedInputFrame.aimButton.isHeld = false;
        simulatedInputFrame.fireButton.isHeld = false;
        simulatedInputFrame.sprintButton.isHeld = false;
    }

    private void FindClosestEnemy()
    {
        // 高效球形判定寻找周边敌人
        Collider[] enemies = Physics.OverlapSphere(transform.position, engageDistance, enemyLayer);
        float closestDist = float.MaxValue;
        currentTarget = null;

        foreach (var e in enemies)
        {
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                currentTarget = e.transform;
            }
        }
    }
}