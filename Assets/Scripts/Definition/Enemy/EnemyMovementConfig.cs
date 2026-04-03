using Game.Definition.ConfigSystem.Core;
using UnityEngine;

namespace Game.Definition.Enemy
{
    /// <summary>
    /// 敌人移动配置
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyMovementConfig", menuName = "Game/Configs/Enemy/Enemy Movement Config")]
    public sealed class EnemyMovementConfig : ConfigBase
    {
        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private float combatMoveSpeed = 3f;
        [SerializeField] private float chaseSpeed = 4f;
        [SerializeField] private float turnSharpness = 12f;
        [SerializeField] private float stopDistance = 1.5f;

        public float PatrolSpeed => patrolSpeed;
        public float CombatMoveSpeed => combatMoveSpeed;
        public float ChaseSpeed => chaseSpeed;
        public float TurnSharpness => turnSharpness;
        public float StopDistance => stopDistance;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (patrolSpeed < 0f || combatMoveSpeed < 0f || chaseSpeed < 0f)
            {
                _context.AddError(ConfigId, "敌人移动速度不能小于 0。");
            }

            if (turnSharpness <= 0f)
            {
                _context.AddError(ConfigId, "TurnSharpness 必须大于 0。");
            }

            if (stopDistance < 0f)
            {
                _context.AddError(ConfigId, "StopDistance 不能小于 0。");
            }
        }
    }
}
