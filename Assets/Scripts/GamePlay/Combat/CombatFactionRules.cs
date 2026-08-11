namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 首版战斗阵营许可矩阵；LayerMask 只能粗筛，最终伤害许可由此规则决定。
    /// </summary>
    public static class CombatFactionRules
    {
        /// <summary>
        /// 判断来源阵营是否可以伤害目标阵营。
        /// </summary>
        /// <param name="_sourceFaction">攻击责任者的阵营。</param>
        /// <param name="_targetFaction">权威目标的阵营。</param>
        /// <returns>仅玩家队伍与敌人两个方向返回 <see langword="true"/>。</returns>
        public static bool CanDamage(CombatFaction _sourceFaction, CombatFaction _targetFaction)
        {
            return (_sourceFaction == CombatFaction.PlayerParty && _targetFaction == CombatFaction.Enemy)
                || (_sourceFaction == CombatFaction.Enemy && _targetFaction == CombatFaction.PlayerParty);
        }
    }
}
