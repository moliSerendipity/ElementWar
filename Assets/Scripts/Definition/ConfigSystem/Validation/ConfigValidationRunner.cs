using System.Collections.Generic;
using Game.Definition.ConfigSystem.Core;
using Game.Definition.ConfigSystem.Registry;

namespace Game.Definition.ConfigSystem.Validation
{
    /// <summary>
    /// 统一配置校验执行器。
    /// 固定输入：ConfigRegistry + 已 Initialize 的 ConfigService。
    /// 固定输出：ConfigValidationContext。
    /// </summary>
    public sealed class ConfigValidationRunner
    {
        private readonly ConfigRegistry registry;
        private readonly ConfigService configService;

        public ConfigValidationRunner(ConfigRegistry _registry, ConfigService _configService)
        {
            registry = _registry;
            configService = _configService;
        }

        public ConfigValidationContext Run()
        {
            ConfigValidationContext context = new();
            HashSet<string> uniqueIds = new();

            if (registry == null)
            {
                context.AddError(nameof(ConfigRegistry), "ConfigRegistry 为空，无法执行配置校验。");
                return context;
            }

            if (configService == null || configService.IsInitialized == false)
            {
                context.AddError(nameof(ConfigService), "ConfigService 尚未初始化，无法执行配置校验。");
                return context;
            }

            foreach (ConfigBase config in registry.EnumerateAllConfigs())
            {
                if (config == null)
                {
                    continue;
                }

                if (ConfigIdUtility.IsValid(config.ConfigId) == false)
                {
                    context.AddError(config.name, "ConfigId 不能为空。");
                    continue;
                }

                if (uniqueIds.Add(config.ConfigId) == false)
                {
                    context.AddError(config.ConfigId, "发现重复 ConfigId。");
                }

                config.Validate(context, configService);
            }

            return context;
        }
    }
}
