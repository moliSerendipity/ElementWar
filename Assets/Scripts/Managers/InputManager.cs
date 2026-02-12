using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 按钮状态结构体
/// </summary>
public struct ButtonState
{
    public bool wasPressedThisFrame;                                            // 当前帧是否刚按下
    public bool isHeld;                                                         // 是否持续按住
    public bool wasReleasedThisFrame;                                           // 当前帧是否刚松开
}

/// <summary>
/// 输入帧数据
/// </summary>
public class InputFrame
{
    [Header("移动输入")]
    public Vector2 move;                                                        // 移动输入 (单位向量)
    public Vector2 look;                                                        // 视角输入 (单位：像素)
    public ButtonState sprintButton;                                            // 冲刺输入
    public ButtonState jumpButton;                                              // 跳跃输入

    [Header("战斗输入")]
    public ButtonState fireButton;                                              // 射击输入
    public ButtonState aimButton;                                               // 瞄准输入
    public ButtonState reloadButton;                                            // 换弹输入
    public float switchWeapon;                                                  // 切换武器输入 (滚轮)

    [Header("技能输入")]
    public ButtonState[] skillsButtonArray = new ButtonState[4];                // 技能输入数组

    [Header("队伍输入")]
    public ButtonState[] switchAIButtonArray = new ButtonState[4];              // 切换AI输入数组
    public ButtonState switchNextCharacterButton;                               // 切换下一个角色输入

    [Header("交互输入")]
    public ButtonState pickUpButton;                                            // 捡起物品输入
    public ButtonState openOrCloseBagButton;                                    // 打开/关闭背包输入
}

public class InputManager : SingleMonoBase<InputManager>
{
    private MyInputSystem inputSystem;                                          // 输入系统
    private InputAction[] skillActions;                                         // 技能输入缓存数组
    private InputAction[] switchAIActions;                                      // 切换AI输入缓存数组

    public InputFrame Frame { get; private set; } = new InputFrame();           // 当前输入帧数据

    override protected void Awake()
    {
        base.Awake();

        inputSystem = new MyInputSystem();
        inputSystem.Enable();

        // 缓存技能和切换AI的输入动作
        skillActions = new InputAction[]
        {
            inputSystem.PlayerSkill.IsSkill1,
            inputSystem.PlayerSkill.IsSkill2,
            inputSystem.PlayerSkill.IsSkill3,
            inputSystem.PlayerSkill.IsSkill4
        };
        switchAIActions = new InputAction[]
        {
            inputSystem.PlayerTeam.IsSwitchAI1,
            inputSystem.PlayerTeam.IsSwitchAI2,
            inputSystem.PlayerTeam.IsSwitchAI3,
            inputSystem.PlayerTeam.IsSwitchAI4
        };
    }

    private void Update()
    {
        // 更新移动输入
        Frame.move = inputSystem.PlayerMovement.Move.ReadValue<Vector2>();
        Frame.look = inputSystem.PlayerMovement.Look.ReadValue<Vector2>();
        UpdateButtonState(ref Frame.sprintButton, inputSystem.PlayerMovement.IsSprint);
        UpdateButtonState(ref Frame.jumpButton, inputSystem.PlayerMovement.IsJump);

        // 更新战斗输入
        UpdateButtonState(ref Frame.fireButton, inputSystem.PlayerCombat.IsFire);
        UpdateButtonState(ref Frame.aimButton, inputSystem.PlayerCombat.IsAim);
        UpdateButtonState(ref Frame.reloadButton, inputSystem.PlayerCombat.IsReload);
        Frame.switchWeapon = inputSystem.PlayerCombat.SwitchWeapon.ReadValue<float>();

        // 更新技能输入
        for (int i = 0; i < Frame.skillsButtonArray.Length; ++i)
            UpdateButtonState(ref Frame.skillsButtonArray[i], skillActions[i]);

        // 更新队伍输入
        for (int i = 0; i < Frame.switchAIButtonArray.Length; ++i)
            UpdateButtonState(ref Frame.switchAIButtonArray[i], switchAIActions[i]);

        UpdateButtonState(ref Frame.switchNextCharacterButton, inputSystem.PlayerTeam.IsSwitchNextCharacter);

        // 更新交互输入
        UpdateButtonState(ref Frame.pickUpButton, inputSystem.PlayerInteraction.IsPickUp);
        UpdateButtonState(ref Frame.openOrCloseBagButton, inputSystem.PlayerInteraction.IsOpenOrCloseBag);
    }

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

    private void OnEnable()
    {
        inputSystem?.Enable();
    }

    private void OnDisable()
    {
        inputSystem?.Disable();
    }
}
