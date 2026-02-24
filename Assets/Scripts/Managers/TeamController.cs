using Cinemachine;
using System.Collections.Generic;
using UnityEngine;

public class TeamController : SingleMonoBase<TeamController>
{
    [Header("队伍成员")]
    public List<TPSCharacterController> teamMembers = new List<TPSCharacterController>();

    [Header("摄像机跟随目标")]
    public CinemachineFreeLook cameraFollowTarget;

    private int currentPlayerIndex = 0;                                         // 当前控制的玩家索引

    protected override void Awake()
    {
        base.Awake();
    }

    void Start()
    {
        if (teamMembers.Count == 0)
        {
            Debug.LogWarning("[TeamController] 队伍为空！请在 Inspector 中拖入角色。");
            return;
        }

        // 初始化：让 0 号位成为玩家，其余成为 AI
        for (int i = 0; i < teamMembers.Count; ++i)
        {
            bool isPlayer = (i == currentPlayerIndex);
            teamMembers[i].SetPlayerControl(isPlayer);
        }

        UpdateCameraTarget();
    }

    void Update()
    {
        if (teamMembers.Count <= 0)
            return;

        InputFrame inputFrame = InputManager.Instance.Frame;

        // 顺序切换角色
        if (inputFrame.switchNextCharacterButton.wasPressedThisFrame)
        {
            int nextIndex = (currentPlayerIndex + 1) % teamMembers.Count;
            SwapCharacter(nextIndex);
            return;
        }

        // 直接切换到指定角色
        for (int i = 0; i < inputFrame.switchAIButtonArray.Length; ++i)
        {
            // 如果按下了按键，且队伍里有这个人，且不是当前正在操控的人
            if (inputFrame.switchAIButtonArray[i].wasPressedThisFrame && i < teamMembers.Count && i != currentPlayerIndex)
            {
                SwapCharacter(i);
                return;
            }
        }
    }

    /// <summary>
    /// 更新摄像机跟随目标为当前主控角色
    /// </summary>
    private void UpdateCameraTarget()
    {
        if (cameraFollowTarget != null && teamMembers.Count > 0)
        {
            cameraFollowTarget.Follow = teamMembers[currentPlayerIndex].transform;
            cameraFollowTarget.LookAt = teamMembers[currentPlayerIndex].transform;
        }
    }

    /// <summary>
    /// 切换控制权到指定索引的角色，角色坐标互换
    /// </summary>
    /// <param name="_targetIndex">要切换到的角色索引</param>
    private void SwapCharacter(int _targetIndex)
    {
        TPSCharacterController oldCharacter = teamMembers[currentPlayerIndex];
        TPSCharacterController newCharacter = teamMembers[_targetIndex];

        // 缓存旧坐标与旧旋转
        Vector3 oldPosition = oldCharacter.transform.position;
        Quaternion oldRotation = oldCharacter.transform.rotation;

        Vector3 newPosition = newCharacter.transform.position;
        Quaternion newRotation = newCharacter.transform.rotation;

        // 剥夺与赋予控制权
        oldCharacter.SetPlayerControl(false);
        newCharacter.SetPlayerControl(true);

        // 瞬间坐标互换
        oldCharacter.Motor.Teleport(newPosition, newRotation);
        newCharacter.Motor.Teleport(oldPosition, oldRotation);

        // 更新索引与摄像机跟随目标
        currentPlayerIndex = _targetIndex;
        UpdateCameraTarget();

        Debug.Log($"[TeamController] 角色互换成功！当前主控: 角色 {_targetIndex}");
    }

    /// <summary>
    /// 获取当前主控角色
    /// </summary>
    /// <returns>角色控制器脚本</returns>
    public TPSCharacterController GetCurrentPlayer()
    {
        if (teamMembers.Count == 0)
            return null;
        return teamMembers[currentPlayerIndex];
    }
}
