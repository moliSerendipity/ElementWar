using UnityEngine;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.Character
{
    /// <summary>
    /// 角色移动控制参数。
    /// 这里只保留 CharacterMovementController 直接消费的参数，不和基础面板、跳跃配置、瞄准配置重复。
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterMovementConfig", menuName = "Game/Configs/Character/Character Movement Config")]
    public sealed class CharacterMovementConfig : ConfigBase
    {
        [SerializeField] private float walkSpeed = 2.5f;
        [SerializeField] private float runSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 6.5f;

        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public float SprintSpeed => sprintSpeed;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (walkSpeed < 0f || runSpeed < 0f || sprintSpeed < 0f)
            {
                _context.AddError(ConfigId, "移动速度不能小于 0。");
            }

            if (runSpeed < walkSpeed)
            {
                _context.AddWarning(ConfigId, "RunSpeed 小于 WalkSpeed，通常不是期望配置。");
            }

            if (sprintSpeed < runSpeed)
            {
                _context.AddWarning(ConfigId, "SprintSpeed 小于 RunSpeed，通常不是期望配置。");
            }
        }
    }
}
