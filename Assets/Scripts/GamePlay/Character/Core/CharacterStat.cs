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
        [SerializeField] private float normalYawSensitivity = 0.18f;
        [SerializeField] private float normalPitchSensitivity = 0.12f;
        [SerializeField] private float aimYawSensitivity = 0.12f;
        [SerializeField] private float aimPitchSensitivity = 0.08f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float maxFallSpeed = -30f;

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
        public float NormalYawSensitivity => normalYawSensitivity;
        public float NormalPitchSensitivity => normalPitchSensitivity;
        public float AimYawSensitivity => aimYawSensitivity;
        public float AimPitchSensitivity => aimPitchSensitivity;
        public float JumpHeight => jumpHeight;
        public float Gravity => gravity;
        public float MaxFallSpeed => maxFallSpeed;
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
                characterResistanceSetConfig != null ? characterResistanceSetConfig.WaterResistance : 0f,
                characterResistanceSetConfig != null ? characterResistanceSetConfig.ElectricResistance : 0f,
                characterResistanceSetConfig != null ? characterResistanceSetConfig.IceResistance : 0f,
                characterResistanceSetConfig != null ? characterResistanceSetConfig.ExplosionResistance : 0f);

            walkSpeed = Mathf.Max(0f, characterMovementConfig.WalkSpeed);
            runSpeed = Mathf.Max(0f, characterMovementConfig.RunSpeed);
            sprintSpeed = Mathf.Max(0f, characterMovementConfig.SprintSpeed);
            aimMoveSpeedMultiplier = Mathf.Max(0f, characterAimConfig.AimMoveSpeedMultiplier);
            normalYawSensitivity = Mathf.Max(0.0001f, characterAimConfig.NormalYawSensitivity);
            normalPitchSensitivity = Mathf.Max(0.0001f, characterAimConfig.NormalPitchSensitivity);
            aimYawSensitivity = Mathf.Max(0.0001f, characterAimConfig.AimYawSensitivity);
            aimPitchSensitivity = Mathf.Max(0.0001f, characterAimConfig.AimPitchSensitivity);
            jumpHeight = Mathf.Max(0.01f, characterJumpConfig.JumpHeight);
            gravity = Mathf.Min(-0.01f, characterJumpConfig.Gravity);
            maxFallSpeed = Mathf.Min(-0.01f, characterJumpConfig.MaxFallSpeed);
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
            normalYawSensitivity = 0.18f;
            normalPitchSensitivity = 0.12f;
            aimYawSensitivity = 0.12f;
            aimPitchSensitivity = 0.08f;
            jumpHeight = 0f;
            gravity = -25f;
            maxFallSpeed = -30f;
            ResetCombatStatRuntime();
        }

        public void SetWalkSpeed(float _value) => walkSpeed = Mathf.Max(0f, _value);
        public void SetRunSpeed(float _value) => runSpeed = Mathf.Max(0f, _value);
        public void SetSprintSpeed(float _value) => sprintSpeed = Mathf.Max(0f, _value);
        public void SetAimMoveSpeedMultiplier(float _value) => aimMoveSpeedMultiplier = Mathf.Max(0f, _value);
        public void SetNormalYawSensitivity(float _value) => normalYawSensitivity = Mathf.Max(0.0001f, _value);
        public void SetNormalPitchSensitivity(float _value) => normalPitchSensitivity = Mathf.Max(0.0001f, _value);
        public void SetAimYawSensitivity(float _value) => aimYawSensitivity = Mathf.Max(0.0001f, _value);
        public void SetAimPitchSensitivity(float _value) => aimPitchSensitivity = Mathf.Max(0.0001f, _value);
        public void SetJumpHeight(float _value) => jumpHeight = Mathf.Max(0.01f, _value);
        public void SetGravity(float _value) => gravity = Mathf.Min(-0.01f, _value);
        public void SetMaxFallSpeed(float _value) => maxFallSpeed = Mathf.Min(-0.01f, _value);
    }
}
