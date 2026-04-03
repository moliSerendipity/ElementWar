using Game.Definition.Character;
using Game.Definition.Combat;
using Game.Definition.ConfigSystem.Core;
using UnityEngine;

namespace Game.Gameplay.Character
{
    /// <summary>
    /// 角色运行时属性容器。
    ///
    /// 职责：
    /// 1. 在初始化阶段把角色基础面板、移动、跳跃、瞄准和抗性配置解析为运行时当前值；
    /// 2. 作为 Character 域唯一运行时数值入口，供 Movement / Facing / Combat / Buff 统一读取；
    /// 3. 隔离配置表，避免热路径继续散读 ScriptableObject。
    ///
    /// 边界：
    /// 1. grounded / aiming / reloading 这类事实状态不进 Stat；
    /// 2. 规则开关允许缓存，但这里只保存运行时当前值；
    /// 3. Buff 后续只改这里，不回写配置。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterStat : ActorStatBase
    {
        [Header("Config")]
        [SerializeField] private string characterDefinitionConfigId;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 2.5f;
        [SerializeField] private float runSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [SerializeField] private float aimMoveSpeedMultiplier = 0.7f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float maxFallSpeed = -30f;

        [Header("Crit")]
        [SerializeField] private float critChance = 5f;
        [SerializeField] private float critDamageMultiplier = 1.5f;

        private CharacterDefinitionConfig characterDefinitionConfig;
        private CharacterBaseStatConfig characterBaseStatConfig;
        private CharacterMovementConfig characterMovementConfig;
        private CharacterJumpConfig characterJumpConfig;
        private CharacterAimConfig characterAimConfig;
        private ResistanceSetConfig characterResistanceSetConfig;

        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public float SprintSpeed => sprintSpeed;
        public float AimMoveSpeedMultiplier => aimMoveSpeedMultiplier;
        public float JumpHeight => jumpHeight;
        public float Gravity => gravity;
        public float MaxFallSpeed => maxFallSpeed;
        public float CritChance => critChance;
        public float CritDamageMultiplier => critDamageMultiplier;

        public bool TryInitialize(ConfigService _configService)
        {
            ResetRuntimeState();

            if (_configService == null || _configService.IsInitialized == false)
            {
                Debug.LogError($"[{nameof(CharacterStat)}] 初始化失败：ConfigService 不可用。Object={name}", this);
                return false;
            }

            if (ConfigIdUtility.IsValid(characterDefinitionConfigId) == false)
            {
                Debug.LogError($"[{nameof(CharacterStat)}] 初始化失败：CharacterDefinitionConfigId 为空。Object={name}", this);
                return false;
            }

            if (_configService.TryGetConfig(characterDefinitionConfigId, out CharacterDefinitionConfig resolvedDefinitionConfig) == false)
            {
                Debug.LogError($"[{nameof(CharacterStat)}] 初始化失败：找不到 CharacterDefinitionConfig，Id={characterDefinitionConfigId}，Object={name}", this);
                return false;
            }

            if (resolvedDefinitionConfig.CharacterBaseStatConfig == null)
            {
                Debug.LogError($"[{nameof(CharacterStat)}] 初始化失败：CharacterBaseStatConfig 缺失。CharacterId={characterDefinitionConfigId}，Object={name}", this);
                return false;
            }

            if (resolvedDefinitionConfig.CharacterMovementConfig == null)
            {
                Debug.LogError($"[{nameof(CharacterStat)}] 初始化失败：CharacterMovementConfig 缺失。CharacterId={characterDefinitionConfigId}，Object={name}", this);
                return false;
            }

            if (resolvedDefinitionConfig.CharacterJumpConfig == null)
            {
                Debug.LogError($"[{nameof(CharacterStat)}] 初始化失败：CharacterJumpConfig 缺失。CharacterId={characterDefinitionConfigId}，Object={name}", this);
                return false;
            }

            if (resolvedDefinitionConfig.CharacterAimConfig == null)
            {
                Debug.LogError($"[{nameof(CharacterStat)}] 初始化失败：CharacterAimConfig 缺失。CharacterId={characterDefinitionConfigId}，Object={name}", this);
                return false;
            }

            if (resolvedDefinitionConfig.CharacterResistanceSetConfig == null)
            {
                Debug.LogError($"[{nameof(CharacterStat)}] 初始化失败：ResistanceSetConfig 缺失。CharacterId={characterDefinitionConfigId}，Object={name}", this);
                return false;
            }

            characterDefinitionConfig = resolvedDefinitionConfig;
            characterBaseStatConfig = resolvedDefinitionConfig.CharacterBaseStatConfig;
            characterMovementConfig = resolvedDefinitionConfig.CharacterMovementConfig;
            characterJumpConfig = resolvedDefinitionConfig.CharacterJumpConfig;
            characterAimConfig = resolvedDefinitionConfig.CharacterAimConfig;
            characterResistanceSetConfig = resolvedDefinitionConfig.CharacterResistanceSetConfig;

            CommitCombatStatInitialization(
                characterBaseStatConfig.MaxHealth,
                characterBaseStatConfig.MaxShield,
                characterBaseStatConfig.AttackPower,
                characterBaseStatConfig.Defense,
                characterBaseStatConfig.Toughness,
                characterBaseStatConfig.DamageTakenMultiplier,
                characterBaseStatConfig.HealingTakenMultiplier,
                characterResistanceSetConfig != null ? characterResistanceSetConfig.PhysicalResistance : 0f,
                characterResistanceSetConfig != null ? characterResistanceSetConfig.FireResistance : 0f,
                characterResistanceSetConfig != null ? characterResistanceSetConfig.ElectricResistance : 0f,
                characterResistanceSetConfig != null ? characterResistanceSetConfig.IceResistance : 0f,
                characterResistanceSetConfig != null ? characterResistanceSetConfig.ExplosionResistance : 0f);

            walkSpeed = Mathf.Max(0f, characterMovementConfig.WalkSpeed);
            runSpeed = Mathf.Max(0f, characterMovementConfig.RunSpeed);
            sprintSpeed = Mathf.Max(0f, characterMovementConfig.SprintSpeed);
            aimMoveSpeedMultiplier = Mathf.Max(0f, characterAimConfig.AimMoveSpeedMultiplier);
            jumpHeight = Mathf.Max(0.01f, characterJumpConfig.JumpHeight);
            gravity = Mathf.Min(-0.01f, characterJumpConfig.Gravity);
            maxFallSpeed = Mathf.Min(-0.01f, characterJumpConfig.MaxFallSpeed);
            critChance = characterBaseStatConfig.CritChance;
            critDamageMultiplier = Mathf.Max(1f, characterBaseStatConfig.CritDamageMultiplier);
            return true;
        }

        public void ResetRuntimeState()
        {
            characterDefinitionConfig = null;
            characterBaseStatConfig = null;
            characterMovementConfig = null;
            characterJumpConfig = null;
            characterAimConfig = null;
            characterResistanceSetConfig = null;
            walkSpeed = 0f;
            runSpeed = 0f;
            sprintSpeed = 0f;
            aimMoveSpeedMultiplier = 0f;
            jumpHeight = 0f;
            gravity = -25f;
            maxFallSpeed = -30f;
            critChance = 5f;
            critDamageMultiplier = 1.5f;
            ResetCombatStatRuntime();
        }

        public void SetWalkSpeed(float _value) => walkSpeed = Mathf.Max(0f, _value);
        public void SetRunSpeed(float _value) => runSpeed = Mathf.Max(0f, _value);
        public void SetSprintSpeed(float _value) => sprintSpeed = Mathf.Max(0f, _value);
        public void SetAimMoveSpeedMultiplier(float _value) => aimMoveSpeedMultiplier = Mathf.Max(0f, _value);
        public void SetJumpHeight(float _value) => jumpHeight = Mathf.Max(0.01f, _value);
        public void SetGravity(float _value) => gravity = Mathf.Min(-0.01f, _value);
        public void SetMaxFallSpeed(float _value) => maxFallSpeed = Mathf.Min(-0.01f, _value);
        public void SetCritChance(float _value) => critChance = _value;
        public void SetCritDamageMultiplier(float _value) => critDamageMultiplier = Mathf.Max(1f, _value);
    }
}
