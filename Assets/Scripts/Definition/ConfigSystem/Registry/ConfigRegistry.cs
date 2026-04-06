using System.Collections.Generic;
using UnityEngine;
using Game.Definition.Character;
using Game.Definition.Combat;
using Game.Definition.AreaEffect;
using Game.Definition.Buff;
using Game.Definition.ConfigSystem.Core;
using Game.Definition.Debug;
using Game.Definition.Element;
using Game.Definition.Enemy;
using Game.Definition.Skill;
using Game.Definition.Stage;
using Game.Definition.Presentation;
using Game.Definition.UI;
using Game.Definition.Weapon;

namespace Game.Definition.ConfigSystem.Registry
{
    /// <summary>
    /// 配置总注册表。
    /// 只负责收集启动阶段需要注册的正式配置资产。
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigRegistry_Default", menuName = "Game/Configs/Common/Config Registry")]
    public sealed class ConfigRegistry : ScriptableObject
    {
        [Header("Character")]
        [SerializeField] private List<CharacterDefinitionConfig> characterDefinitions = new();
        [SerializeField] private List<CharacterBaseStatConfig> characterBaseStats = new();
        [SerializeField] private List<CharacterMovementConfig> characterMovements = new();
        [SerializeField] private List<CharacterJumpConfig> characterJumps = new();
        [SerializeField] private List<CharacterAimConfig> characterAims = new();
        [SerializeField] private List<CharacterSwitchConfig> characterSwitches = new();

        [Header("Combat")]
        [SerializeField] private List<DamageRuleConfig> damageRules = new();
        [SerializeField] private List<ResistanceSetConfig> resistanceSets = new();
        [SerializeField] private List<ElementReactionConfig> elementReactions = new();
        [SerializeField] private List<BuffDefinitionConfig> buffDefinitions = new();
        [SerializeField] private List<AreaEffectConfig> areaEffects = new();

        [Header("Weapon")]
        [SerializeField] private List<WeaponDefinitionConfig> weaponDefinitions = new();
        [SerializeField] private List<WeaponLoadoutConfig> weaponLoadouts = new();
        [SerializeField] private List<WeaponStatConfig> weaponStats = new();
        [SerializeField] private List<WeaponSpreadConfig> weaponSpreads = new();
        [SerializeField] private List<WeaponRecoilConfig> weaponRecoils = new();
        [SerializeField] private List<WeaponReloadConfig> weaponReloads = new();

        [Header("Skill")]
        [SerializeField] private List<SkillLoadoutConfig> skillLoadouts = new();

        [Header("Presentation")]
        [SerializeField] private List<WeaponPresentationConfig> weaponPresentations = new();
        [SerializeField] private List<VfxSetConfig> vfxSets = new();
        [SerializeField] private List<AudioEventConfig> audioEvents = new();
        [SerializeField] private List<CameraProfileConfig> cameraProfiles = new();
        [SerializeField] private List<CameraBlendConfig> cameraBlends = new();
        [SerializeField] private List<PoolConfig> poolConfigs = new();

        [Header("Enemy / Stage")]
        [SerializeField] private List<EnemyDefinitionConfig> enemyDefinitions = new();
        [SerializeField] private List<EnemyBaseStatConfig> enemyBaseStats = new();
        [SerializeField] private List<EnemyMovementConfig> enemyMovements = new();
        [SerializeField] private List<StageDefinitionConfig> stageDefinitions = new();

        [Header("UI / Debug")]
        [SerializeField] private List<HUDConfig> hudConfigs = new();
        [SerializeField] private List<DebugConfig> debugConfigs = new();

        public IEnumerable<ConfigBase> EnumerateAllConfigs()
        {
            foreach (ConfigBase config in Enumerate(characterDefinitions)) yield return config;
            foreach (ConfigBase config in Enumerate(characterBaseStats)) yield return config;
            foreach (ConfigBase config in Enumerate(characterMovements)) yield return config;
            foreach (ConfigBase config in Enumerate(characterJumps)) yield return config;
            foreach (ConfigBase config in Enumerate(characterAims)) yield return config;
            foreach (ConfigBase config in Enumerate(characterSwitches)) yield return config;
            foreach (ConfigBase config in Enumerate(damageRules)) yield return config;
            foreach (ConfigBase config in Enumerate(resistanceSets)) yield return config;
            foreach (ConfigBase config in Enumerate(elementReactions)) yield return config;
            foreach (ConfigBase config in Enumerate(buffDefinitions)) yield return config;
            foreach (ConfigBase config in Enumerate(areaEffects)) yield return config;
            foreach (ConfigBase config in Enumerate(weaponDefinitions)) yield return config;
            foreach (ConfigBase config in Enumerate(weaponLoadouts)) yield return config;
            foreach (ConfigBase config in Enumerate(weaponStats)) yield return config;
            foreach (ConfigBase config in Enumerate(weaponSpreads)) yield return config;
            foreach (ConfigBase config in Enumerate(weaponRecoils)) yield return config;
            foreach (ConfigBase config in Enumerate(weaponReloads)) yield return config;
            foreach (ConfigBase config in Enumerate(skillLoadouts)) yield return config;
            foreach (ConfigBase config in Enumerate(weaponPresentations)) yield return config;
            foreach (ConfigBase config in Enumerate(vfxSets)) yield return config;
            foreach (ConfigBase config in Enumerate(audioEvents)) yield return config;
            foreach (ConfigBase config in Enumerate(cameraProfiles)) yield return config;
            foreach (ConfigBase config in Enumerate(cameraBlends)) yield return config;
            foreach (ConfigBase config in Enumerate(poolConfigs)) yield return config;
            foreach (ConfigBase config in Enumerate(enemyDefinitions)) yield return config;
            foreach (ConfigBase config in Enumerate(enemyBaseStats)) yield return config;
            foreach (ConfigBase config in Enumerate(enemyMovements)) yield return config;
            foreach (ConfigBase config in Enumerate(stageDefinitions)) yield return config;
            foreach (ConfigBase config in Enumerate(hudConfigs)) yield return config;
            foreach (ConfigBase config in Enumerate(debugConfigs)) yield return config;
        }

        private static IEnumerable<ConfigBase> Enumerate<TConfig>(List<TConfig> _configs)
            where TConfig : ConfigBase
        {
            if (_configs == null)
            {
                yield break;
            }

            for (int i = 0; i < _configs.Count; i++)
            {
                if (_configs[i] != null)
                {
                    yield return _configs[i];
                }
            }
        }
    }
}
