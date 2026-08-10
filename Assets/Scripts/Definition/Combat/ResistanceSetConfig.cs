using System.Collections.Generic;
using UnityEngine;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.Combat
{
    /// <summary>
    /// 通用抗性集合。
    /// 这里采用扁平字段，是因为当前阶段只需要直接支撑最小战斗闭环与基础抗性查询。
    /// </summary>
    [CreateAssetMenu(fileName = "ResistanceSetConfig", menuName = "Game/Configs/Combat/Resistance Set Config")]
    public sealed class ResistanceSetConfig : ConfigBase
    {
        [Range(-1f, 1f)][SerializeField] private float physicalResistance;
        [Range(-1f, 1f)][SerializeField] private float fireResistance;
        [Range(-1f, 1f)][SerializeField] private float waterResistance;
        [Range(-1f, 1f)][SerializeField] private float electricResistance;
        [Range(-1f, 1f)][SerializeField] private float iceResistance;
        [Range(-1f, 1f)][SerializeField] private float explosionResistance;
        [Range(0f, 1f)][SerializeField] private float staggerResistance;
        [Range(0f, 1f)][SerializeField] private float knockBackResistance;
        [Range(0f, 1f)][SerializeField] private float debuffResistance;

        public float PhysicalResistance => physicalResistance;
        public float FireResistance => fireResistance;
        public float WaterResistance => waterResistance;
        public float ElectricResistance => electricResistance;
        public float IceResistance => iceResistance;
        public float ExplosionResistance => explosionResistance;
        public float StaggerResistance => staggerResistance;
        public float KnockBackResistance => knockBackResistance;
        public float DebuffResistance => debuffResistance;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);
        }
    }
}
