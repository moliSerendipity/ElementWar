using System.Collections.Generic;
using UnityEngine;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.Skill
{
    [CreateAssetMenu(fileName = "SkillLoadoutConfig", menuName = "Game/Configs/Skill/Skill Loadout Config")]
    public sealed class SkillLoadoutConfig : ConfigBase
    {
        [SerializeField] private List<string> skillIdList = new();

        public IReadOnlyList<string> SkillIdList => skillIdList;
    }
}
