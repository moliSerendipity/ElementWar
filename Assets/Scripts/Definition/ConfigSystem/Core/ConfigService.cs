using System;
using System.Collections.Generic;
using Game.Definition.ConfigSystem.Registry;

namespace Game.Definition.ConfigSystem.Core
{
    /// <summary>
    /// 运行时统一配置访问入口。
    /// 生命周期：Bootstrap 创建 -> Initialize 建索引 -> Gameplay 只读查询。
    /// </summary>
    public sealed class ConfigService
    {
        private static ConfigService active;

        private readonly ConfigRegistry registry;
        private readonly Dictionary<string, ConfigBase> configsById = new();
        private readonly Dictionary<Type, List<ConfigBase>> configsByType = new();
        private bool isInitialized;

        public ConfigService(ConfigRegistry _registry)
        {
            registry = _registry;
        }

        public ConfigRegistry Registry => registry;
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// 当前运行时共享的正式配置服务入口。
        /// 由 Bootstrap 在启动阶段完成初始化后写入，Gameplay / Presentation 只读消费，不自行构造新实例。
        /// </summary>
        public static ConfigService Active => active;

        /// <summary>
        /// 由 Bootstrap 在配置初始化与校验通过后注册当前激活的 ConfigService。
        /// </summary>
        public static void SetActive(ConfigService _configService)
        {
            active = _configService;
        }

        /// <summary>
        /// 清空当前激活的 ConfigService。
        /// 主要给退出 PlayMode 或重建 Bootstrap 时使用。
        /// </summary>
        public static void ClearActive()
        {
            active = null;
        }

        /// <summary>
        /// 统一构建类型索引与 Id 索引。重复调用会先清空旧索引再重建。
        /// </summary>
        public void Initialize()
        {
            configsById.Clear();
            configsByType.Clear();

            if (registry == null)
            {
                isInitialized = false;
                return;
            }

            foreach (ConfigBase config in registry.EnumerateAllConfigs())
            {
                if (config == null)
                {
                    continue;
                }

                string configId = config.ConfigId;
                if (ConfigIdUtility.IsValid(configId) == false)
                {
                    continue;
                }

                if (configsById.ContainsKey(configId) == false)
                {
                    configsById.Add(configId, config);
                }

                Type configType = config.GetType();
                if (configsByType.TryGetValue(configType, out List<ConfigBase> typedConfigs) == false)
                {
                    typedConfigs = new List<ConfigBase>();
                    configsByType.Add(configType, typedConfigs);
                }

                typedConfigs.Add(config);
            }

            isInitialized = true;
        }

        public bool HasConfig(string _configId)
        {
            if (isInitialized == false)
            {
                return false;
            }

            return configsById.ContainsKey(ConfigIdUtility.Normalize(_configId));
        }

        public bool TryGetConfig<TConfig>(string _configId, out TConfig _config)
            where TConfig : ConfigBase
        {
            _config = null;

            if (isInitialized == false)
            {
                return false;
            }

            string normalizedId = ConfigIdUtility.Normalize(_configId);
            if (configsById.TryGetValue(normalizedId, out ConfigBase baseConfig) == false)
            {
                return false;
            }

            _config = baseConfig as TConfig;
            return _config != null;
        }

        public TConfig GetConfigOrThrow<TConfig>(string _configId)
            where TConfig : ConfigBase
        {
            if (TryGetConfig(_configId, out TConfig config))
            {
                return config;
            }

            throw new InvalidOperationException($"未找到类型为 {typeof(TConfig).Name}、Id 为 {ConfigIdUtility.Normalize(_configId)} 的配置。");
        }

        public IReadOnlyList<TConfig> GetAllConfigs<TConfig>()
            where TConfig : ConfigBase
        {
            if (isInitialized == false)
            {
                return Array.Empty<TConfig>();
            }

            if (configsByType.TryGetValue(typeof(TConfig), out List<ConfigBase> rawConfigs) == false)
            {
                return Array.Empty<TConfig>();
            }

            List<TConfig> results = new(rawConfigs.Count);
            for (int i = 0; i < rawConfigs.Count; i++)
            {
                if (rawConfigs[i] is TConfig typedConfig)
                {
                    results.Add(typedConfig);
                }
            }

            return results;
        }

        /// <summary>
        /// 给配置主定义表复用的外键检查工具。缺失或类型不匹配都记为 Error。
        /// </summary>
        public void ValidateRequiredReference<TConfig>(ConfigValidationContext _context, string _ownerConfigId, string _referenceId, string _fieldName)
            where TConfig : ConfigBase
        {
            string normalizedReferenceId = ConfigIdUtility.Normalize(_referenceId);
            if (ConfigIdUtility.IsValid(normalizedReferenceId) == false)
            {
                _context.AddError(_ownerConfigId, $"字段 {_fieldName} 不能为空。");
                return;
            }

            if (TryGetConfig<TConfig>(normalizedReferenceId, out _) == false)
            {
                _context.AddError(_ownerConfigId, $"字段 {_fieldName} 指向的配置不存在，期望类型 {typeof(TConfig).Name}，引用 Id = {normalizedReferenceId}。");
            }
        }
    }
}

