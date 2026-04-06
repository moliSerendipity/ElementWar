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
        [SerializeField, Min(0.1f)] private float patrolSpeed = 2f;
        [SerializeField, Min(0.1f)] private float chaseSpeed = 4f;
        [SerializeField, Min(0.1f)] private float turnSharpness = 12f;
        [SerializeField, Min(0.1f)] private float stopDistance = 1.5f;

        public float PatrolSpeed => patrolSpeed;
        public float ChaseSpeed => chaseSpeed;
        public float TurnSharpness => turnSharpness;
        public float StopDistance => stopDistance;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (patrolSpeed >= chaseSpeed)
            {
                _context.AddError(ConfigId, "patrolSpeed 必须小于 chaseSpeed");
            }
        }
    }
}
