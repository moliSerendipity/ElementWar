using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using XLua;

/// <summary>
/// 按钮状态结构体
/// </summary>
[Serializable]
public struct ButtonState
{
    public bool wasPressedThisFrame;                                            // 当前帧是否刚按下
    public bool isHeld;                                                         // 是否持续按住
    public bool wasReleasedThisFrame;                                           // 当前帧是否刚松开

    /// <summary>
    /// 重置按钮按下和松开状态
    /// </summary>
    public void Reset()
    {
        wasPressedThisFrame = false;
        wasReleasedThisFrame = false;
        // 注意：isHeld不重置，因为它表示当前是否持续按住，由逻辑控制
    }
}

/// <summary>
/// 输入帧数据
/// </summary>
public class InputFrame
{
    [Header("移动与视角")]
    public Vector2 move;                                                        // 移动输入 (单位向量)
    public Vector2 look;                                                        // 视角输入 (单位：像素)
    public ButtonState sprintButton;                                            // 冲刺输入
    public ButtonState jumpButton;                                              // 跳跃输入

    [Header("战斗输入")]
    public ButtonState fireButton;                                              // 射击输入
    public ButtonState aimButton;                                               // 瞄准输入
    public ButtonState reloadButton;                                            // 换弹输入
    public ButtonState switchAmmoButton;                                        // 切换弹药输入
    public float switchWeapon;                                                  // 切换武器输入 (滚轮)

    [Header("玩家技能")]
    public ButtonState playerSkillButton;                                       // 玩家小技能输入
    public ButtonState playerSkillUltButton;                                    // 玩家大招输入

    [Header("技能 (通过事件触发，这里仅做状态缓存)")]
    public bool[] skills = new bool[4];                                         // 小技能数组
    public bool[] skillsUlt = new bool[4];                                      // 大招数组

    [Header("队伍输入")]
    public ButtonState[] switchAIButtonArray = new ButtonState[4];              // 切换AI输入数组
    public ButtonState switchNextCharacterButton;                               // 切换下一个角色输入

    [Header("系统/交互")]
    public ButtonState interactButton;                                          // 交互输入
    public bool toggleBag;                                                      // 当前帧是否按下背包按键
}

[LuaCallCSharp]
public class InputManager : SingleMonoBase<InputManager>
{
    private MyInputSystem inputSystem;                                          // 输入系统
    private bool isUIMode = false;                                              // 是否处于 UI 模式（如背包界面），UI 模式下只处理 UI 相关输入
    private InputAction[] switchAIActions;                                      // 切换AI输入缓存数组

    public InputFrame Frame { get; private set; } = new InputFrame();           // 当前帧输入数据

    // C# 事件系统 (供 Lua 或 外部系统 订阅)，相比每帧轮询，事件对 UI 和 技能系统更友好
    public event Action<int> OnSkill;                                           // 参数是角色索引 (0-3)
    public event Action<int> OnSkillUlt;                                        // 参数是角色索引 (0-3)
    public event Action OnToggleBag;                                            // 打开/关闭背包
    public event Action OnSwitchAmmoType;                                       // 切换弹药类型

    override protected void Awake()
    {
        base.Awake();

        inputSystem = new MyInputSystem();

        BindSkillsNormalButton();

        // 缓存切换AI的输入动作
        switchAIActions = new InputAction[]
        {
            inputSystem.Team.IsSwitchAI1,
            inputSystem.Team.IsSwitchAI2,
            inputSystem.Team.IsSwitchAI3,
            inputSystem.Team.IsSwitchAI4
        };
    }

    private void Update()
    {
        // 检测是否按下背包按键
        if (inputSystem.UI.IsToggleBag.WasPressedThisFrame())
        {
            OnToggleBag?.Invoke();
            Frame.toggleBag = true;
        }
        else
        {
            Frame.toggleBag = false;
        }

        // 如果在 UI 模式 (打开背包)，不再更新移动和战斗输入
        if (isUIMode)
        {
            ResetFrameData();
            return;
        }

        // 更新移动输入
        Frame.move = inputSystem.Movement.Move.ReadValue<Vector2>();
        Frame.look = inputSystem.Movement.Look.ReadValue<Vector2>();
        UpdateButtonState(ref Frame.sprintButton, inputSystem.Movement.IsSprint);
        UpdateButtonState(ref Frame.jumpButton, inputSystem.Movement.IsJump);

        // 更新战斗输入
        UpdateButtonState(ref Frame.fireButton, inputSystem.Combat.IsFire);
        UpdateButtonState(ref Frame.aimButton, inputSystem.Combat.IsAim);
        UpdateButtonState(ref Frame.reloadButton, inputSystem.Combat.IsReload);
        UpdateButtonState(ref Frame.switchAmmoButton, inputSystem.Combat.IsSwitchAmmo);
        Frame.switchWeapon = inputSystem.Combat.SwitchWeapon.ReadValue<float>();

        // 更新玩家技能输入
        UpdateButtonState(ref Frame.playerSkillButton, inputSystem.Skills.IsPlayerSkill);
        UpdateButtonState(ref Frame.playerSkillUltButton, inputSystem.Skills.IsPlayerSkillUlt);

        // 更新队伍输入
        for (int i = 0; i < Frame.switchAIButtonArray.Length; ++i)
            UpdateButtonState(ref Frame.switchAIButtonArray[i], switchAIActions[i]);
        UpdateButtonState(ref Frame.switchNextCharacterButton, inputSystem.Team.IsSwitchNextCharacter);

        // 更新系统/交互输入
        UpdateButtonState(ref Frame.interactButton, inputSystem.Interaction.Interact);

        // 处理切换弹药类型的输入
        if (Frame.switchAmmoButton.wasPressedThisFrame)
            OnSwitchAmmoType?.Invoke();
    }

    private void LateUpdate()
    {
        // 清理单帧触发的指令状态，防止连续触发
        for (int i = 0; i < 4; ++i)
        {
            Frame.skills[i] = false;
            Frame.skillsUlt[i] = false;
        }
    }

    private void OnEnable()
    {
        inputSystem?.Enable();
        // 默认进入游戏模式，启用游戏相关输入，并锁定鼠标
        SwitchToGameplayMode();
    }

    private void OnDisable()
    {
        inputSystem?.Disable();
    }

    protected override void OnDestroy()
    {
        // 暴力清空所有委托，切断 C# 对 Lua 的引用
        OnSkill = null;
        OnSkillUlt = null;
        OnToggleBag = null;
        OnSwitchAmmoType = null;

        base.OnDestroy();
    }

    #region 技能输入绑定
    /// <summary>
    /// 绑定小技能、大招通用按钮输入，区分 Tap 和 Hold 交互，分别触发小技能和大招事件
    /// </summary>
    private void BindSkillsNormalButton()
    {
        inputSystem.Skills.IsSkill1.performed += ctx =>
        {
            if (ctx.interaction is TapInteraction)
                TriggerSkill(0);
            else if (ctx.interaction is HoldInteraction)
                TriggerSkillUlt(0);
        };
        inputSystem.Skills.IsSkill2.performed += ctx =>
        {
            if (ctx.interaction is TapInteraction)
                TriggerSkill(1);
            else if (ctx.interaction is HoldInteraction)
                TriggerSkillUlt(1);
        };
        inputSystem.Skills.IsSkill3.performed += ctx =>
        {
            if (ctx.interaction is TapInteraction)
                TriggerSkill(2);
            else if (ctx.interaction is HoldInteraction)
                TriggerSkillUlt(2);
        };
        inputSystem.Skills.IsSkill4.performed += ctx =>
        {
            if (ctx.interaction is TapInteraction)
                TriggerSkill(3);
            else if (ctx.interaction is HoldInteraction)
                TriggerSkillUlt(3);
        };
    }

    /// <summary>
    /// 触发小技能，通过事件通知 Lua
    /// </summary>
    /// <param name="_index">角色索引</param>
    private void TriggerSkill(int _index)
    {
        // UI 模式下不触发技能
        if (isUIMode)
            return;

        Frame.skills[_index] = true;                                            // 写入 Frame 供状态机读取
        OnSkill?.Invoke(_index);                                                // 触发事件供 Lua 读取
    }

    /// <summary>
    /// 触发大招，通过事件通知 Lua
    /// </summary>
    /// <param name="_index"></param>
    private void TriggerSkillUlt(int _index)
    {
        // UI 模式下不触发技能
        if (isUIMode)
            return;

        Frame.skillsUlt[_index] = true;                                         // 写入 Frame 供状态机读取
        OnSkillUlt?.Invoke(_index);                                             // 触发事件供 Lua 读取
    }
    #endregion

    #region 输入状态切换 (供外部调用)
    /// <summary>
    /// 切换到 UI 模式，禁用游戏相关输入，仅保留 UI 输入，并显示鼠标
    /// </summary>
    public void SwitchToUIMode()
    {
        isUIMode = true;
        inputSystem.Movement.Disable();
        inputSystem.Combat.Disable();
        inputSystem.Skills.Disable();
        inputSystem.Team.Disable();
        inputSystem.Interaction.Disable();
        inputSystem.UI.Enable();

        Cursor.lockState = CursorLockMode.None;                                 // 解锁鼠标
        Cursor.visible = true;                                                  // 显示鼠标
    }

    /// <summary>
    /// 切换到游戏模式，启用游戏相关输入，并锁定鼠标
    /// </summary>
    public void SwitchToGameplayMode()
    {
        isUIMode = false;
        inputSystem.Movement.Enable();
        inputSystem.Combat.Enable();
        inputSystem.Skills.Enable();
        inputSystem.Team.Enable();
        inputSystem.Interaction.Enable();
        inputSystem.UI.Enable();

        Cursor.lockState = CursorLockMode.Locked;                               // 锁定鼠标
        Cursor.visible = false;                                                 // 隐藏鼠标
    }
    #endregion

    #region 辅助方法
    /// <summary>
    /// 更新按钮状态
    /// </summary>
    /// <param name="_buttonState">按钮状态</param>
    /// <param name="_action">输入</param>
    private void UpdateButtonState(ref ButtonState _buttonState, InputAction _action)
    {
        _buttonState.wasPressedThisFrame = _action.WasPressedThisFrame();
        _buttonState.isHeld = _action.IsPressed();
        _buttonState.wasReleasedThisFrame = _action.WasReleasedThisFrame();
    }

    /// <summary>
    /// 重置单帧数据，主要用于切换输入模式时，防止残留输入影响新模式的行为
    /// </summary>
    private void ResetFrameData()
    {
        Frame.move = Vector2.zero;
        Frame.look = Vector2.zero;
        Frame.sprintButton.Reset();
        Frame.jumpButton.Reset();
        Frame.fireButton.Reset();
        Frame.aimButton.Reset();
        Frame.reloadButton.Reset();
        Frame.switchWeapon = 0f;
        Frame.playerSkillButton.Reset();
        Frame.playerSkillUltButton.Reset();
        for (int i = 0; i < Frame.switchAIButtonArray.Length; ++i)
            Frame.switchAIButtonArray[i].Reset();
        Frame.switchNextCharacterButton.Reset();
        Frame.interactButton.Reset();
        // 技能状态由事件触发，不在这里重置
    }
    #endregion
}
