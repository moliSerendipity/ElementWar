using UnityEngine;

namespace Game.Gameplay.Character
{
    /// <summary>
    /// 将 PlayerInputFrame 翻译为角色统一消费的 InputContext。
    /// 该层负责“设备输入 -> 角色语义”，不负责角色门控与状态机判定。
    /// </summary>
    public sealed class PlayerCharacterInputSource : MonoBehaviour, ICharacterInputSource
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private bool normalizeMoveVector = true;

        public bool TryBuildInputContext(out InputContext _inputContext)
        {
            _inputContext = InputContext.Empty;

            if (inputReader == null)
            {
                return false;
            }

            if (!inputReader.TryGetCurrentFrame(out PlayerInputFrame _inputFrame))
            {
                return false;
            }

            Vector2 moveVector = _inputFrame.MoveVector;
            if (normalizeMoveVector && moveVector.sqrMagnitude > 1f)
            {
                moveVector.Normalize();
            }

            _inputContext = new InputContext(
                moveVector,
                _inputFrame.LookDelta,
                _inputFrame.SprintHeld,
                _inputFrame.JumpPressed,
                _inputFrame.FirePressed,
                _inputFrame.FireHeld,
                _inputFrame.AimHeld,
                _inputFrame.ReloadPressed,
                _inputFrame.SwitchAmmoPressed,
                _inputFrame.SwitchPressed,
                _inputFrame.SkillPressed);

            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }
        }
#endif
    }
}
