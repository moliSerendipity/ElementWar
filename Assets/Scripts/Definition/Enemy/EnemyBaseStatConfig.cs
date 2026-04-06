using Game.Definition.ConfigSystem.Core;
using UnityEngine;

namespace Game.Definition.Enemy
{
    /// <summary>
    /// 敌人基础面板配置。
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyBaseStatConfig", menuName = "Game/Configs/Enemy/Enemy Base Stat Config")]
    public sealed class EnemyBaseStatConfig : ConfigBase
    {
        [Header("Detection")]
        [SerializeField, Min(0.1f)] private float detectRange = 15f;
        [SerializeField, Min(0.1f)] private float loseTargetRange = 20f;
        [SerializeField, Min(0.01f)] private float targetMemoryDuration = 1.5f;
        [SerializeField, Min(0.01f)] private float scanInterval = 0.15f;

        [Header("Combat")]
        [SerializeField, Min(0.1f)] private float maxHealth = 120f;
        [SerializeField, Min(0f)] private float attackPower = 12f;
        [SerializeField, Min(0f)] private float defense = 2f;
        [SerializeField] private float toughness = 120f;
        [SerializeField, Min(0.01f)] private float attackCooldown = 0.5f;
        [SerializeField, Min(1f)] private float damageTakenMultiplier = 1f;
        [SerializeField, Min(1f)] private float weakPointDamageMultiplier = 1.5f;

        public float DetectRange => detectRange;
        public float LoseTargetRange => loseTargetRange;
        public float TargetMemoryDuration => targetMemoryDuration;
        public float ScanInterval => scanInterval;
        public float MaxHealth => maxHealth;
        public float AttackPower => attackPower;
        public float Defense => defense;
        public float Toughness => toughness;
        public float AttackCooldown => attackCooldown;
        public float DamageTakenMultiplier => damageTakenMultiplier;
        public float WeakPointDamageMultiplier => weakPointDamageMultiplier;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (detectRange >= loseTargetRange)
            {
                _context.AddError(ConfigId, "Detect Range 必须小于 Lose Target Range.");
            }
        }
    }
}
