using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Character
{
    /// <summary>
    /// 唯一允许直接读取 Unity Input System 的玩家输入读取器。
    /// 只负责把 Action 输入采样为 PlayerInputFrame，不负责角色状态合法性判断。
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference lookAction;

        [Header("Held")]
        [SerializeField] private InputActionReference aimAction;
        [SerializeField] private InputActionReference sprintAction;
        [SerializeField] private InputActionReference fireAction;

        [Header("Pressed")]
        [SerializeField] private InputActionReference reloadAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference skillAction;
        [SerializeField] private InputActionReference switchAction;

        private PlayerInputFrame currentFrame = PlayerInputFrame.Empty;
        private int lastCapturedFrame = -1;

        public PlayerInputFrame CurrentFrame => currentFrame;

        private void OnEnable()
        {
            EnableAction(moveAction);
            EnableAction(lookAction);
            EnableAction(aimAction);
            EnableAction(sprintAction);
            EnableAction(fireAction);
            EnableAction(reloadAction);
            EnableAction(jumpAction);
            EnableAction(skillAction);
            EnableAction(switchAction);
        }

        private void OnDisable()
        {
            DisableAction(moveAction);
            DisableAction(lookAction);
            DisableAction(aimAction);
            DisableAction(sprintAction);
            DisableAction(fireAction);
            DisableAction(reloadAction);
            DisableAction(jumpAction);
            DisableAction(skillAction);
            DisableAction(switchAction);

            currentFrame = PlayerInputFrame.Empty;
        }

        private void Update()
        {
            CaptureFrame();
        }

        /// <summary>
        /// 在当前帧捕获一次原始输入。
        /// 角色主链通过 CharacterInputRouter 在固定阶段读取本结果，避免依赖多个 MonoBehaviour 的自然 Update 顺序。
        /// </summary>
        public void CaptureFrame()
        {
            if (!isActiveAndEnabled)
            {
                currentFrame = PlayerInputFrame.Empty;
                return;
            }

            if (lastCapturedFrame == Time.frameCount)
            {
                return;
            }

            currentFrame = BuildFrame();
            lastCapturedFrame = Time.frameCount;
        }

        public bool TryGetCurrentFrame(out PlayerInputFrame _inputFrame)
        {
            if (!isActiveAndEnabled)
            {
                _inputFrame = PlayerInputFrame.Empty;
                return false;
            }

            CaptureFrame();
            _inputFrame = currentFrame;
            return true;
        }

        private PlayerInputFrame BuildFrame()
        {
            return new PlayerInputFrame(
                ReadVector2(moveAction),
                ReadVector2(lookAction),
                ReadHeld(sprintAction),
                ReadPressed(jumpAction),
                ReadPressed(fireAction),
                ReadHeld(fireAction),
                ReadHeld(aimAction),
                ReadPressed(reloadAction),
                ReadPressed(switchAction),
                ReadPressed(skillAction));
        }

        private static void EnableAction(InputActionReference _actionReference)
        {
            _actionReference?.action?.Enable();
        }

        private static void DisableAction(InputActionReference _actionReference)
        {
            _actionReference?.action?.Disable();
        }

        private static Vector2 ReadVector2(InputActionReference _actionReference)
        {
            return _actionReference?.action == null
                ? Vector2.zero
                : _actionReference.action.ReadValue<Vector2>();
        }

        private static bool ReadHeld(InputActionReference _actionReference)
        {
            return _actionReference?.action != null && _actionReference.action.IsPressed();
        }

        // 触发态输入必须在读取层就和持续态区分，避免后续状态黏连。
        private static bool ReadPressed(InputActionReference _actionReference)
        {
            return _actionReference?.action != null && _actionReference.action.WasPressedThisFrame();
        }
    }
}
