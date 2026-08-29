using Game.Definition.ConfigSystem.Core;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("toughness")]
        [SerializeField, Min(0.1f)] private float maxToughness = 120f;
        [SerializeField, Min(0f)] private float toughnessRecoveryPerSecond = 24f;
        [SerializeField, Min(0.01f)] private float minimumToughnessDamage = 10f;
        [SerializeField, Min(0.01f)] private float staggerDuration = 1f;
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
        /// <summary>敌人初始化时采用的韧性上限。</summary>
        public float MaxToughness => maxToughness;

        /// <summary>未失衡时每秒连续恢复的韧性。</summary>
        public float ToughnessRecoveryPerSecond => toughnessRecoveryPerSecond;

        /// <summary>单次请求必须达到的最低韧性伤害。</summary>
        public float MinimumToughnessDamage => minimumToughnessDamage;

        /// <summary>破韧后保持失衡的秒数。</summary>
        public float StaggerDuration => staggerDuration;
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

            if (IsFinite(maxToughness) == false || maxToughness <= 0f)
            {
                _context.AddError(ConfigId, "MaxToughness 必须是大于 0 的有限数。");
            }

            if (IsFinite(toughnessRecoveryPerSecond) == false || toughnessRecoveryPerSecond < 0f)
            {
                _context.AddError(ConfigId, "ToughnessRecoveryPerSecond 必须是非负有限数。");
            }

            if (IsFinite(minimumToughnessDamage) == false || minimumToughnessDamage <= 0f)
            {
                _context.AddError(ConfigId, "MinimumToughnessDamage 必须是大于 0 的有限数。");
            }

            if (IsFinite(staggerDuration) == false || staggerDuration <= 0f)
            {
                _context.AddError(ConfigId, "StaggerDuration 必须是大于 0 的有限数。");
            }
        }

        private static bool IsFinite(float _value)
        {
            return float.IsNaN(_value) == false && float.IsInfinity(_value) == false;
        }
    }
}
