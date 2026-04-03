using Game.Definition.ConfigSystem.Core;
using Game.Definition.ConfigSystem.Registry;
using Game.Definition.ConfigSystem.Validation;
using UnityEngine;

namespace Game.Bootstrap
{
    /// <summary>
    /// Bootstrap 场景中的最小配置初始化入口。
    /// 挂在 __Bootstrap 物体上，拖入 ConfigRegistry_Default 资产即可。
    /// 这一步只负责装配与校验，不在这里写任何具体玩法初始化。
    /// </summary>
    public sealed class BootstrapConfigInstaller : MonoBehaviour
    {
        [SerializeField] private ConfigRegistry configRegistry;
        [SerializeField] private bool logValidationMessages = true;
        [SerializeField] private bool stopPlayModeOnValidationError = false;

        private ConfigService configService;

        public ConfigService ConfigService => configService;

        private void Awake()
        {
            ConfigService.ClearActive();

            if (configRegistry == null)
            {
                Debug.LogError("[BootstrapConfigInstaller] 未指定 ConfigRegistry。", this);
                return;
            }

            configService = new ConfigService(configRegistry);
            configService.Initialize();

            ConfigValidationRunner runner = new(configRegistry, configService);
            ConfigValidationContext context = runner.Run();

            if (logValidationMessages)
            {
                for (int i = 0; i < context.Messages.Count; i++)
                {
                    ConfigValidationMessage message = context.Messages[i];
                    string logText = $"[ConfigValidation][{message.Severity}] {message.Source} - {message.Message}";

                    switch (message.Severity)
                    {
                        case ConfigValidationSeverity.Info:
                            Debug.Log(logText, this);
                            break;
                        case ConfigValidationSeverity.Warning:
                            Debug.LogWarning(logText, this);
                            break;
                        case ConfigValidationSeverity.Error:
                            Debug.LogError(logText, this);
                            break;
                    }
                }
            }

            if (context.HasError)
            {
                Debug.LogError("[BootstrapConfigInstaller] 配置校验失败，禁止继续基于错误配置开发后续运行时逻辑。", this);

#if UNITY_EDITOR
                if (stopPlayModeOnValidationError)
                {
                    UnityEditor.EditorApplication.isPlaying = false;
                }
#endif
                return;
            }

            ConfigService.SetActive(configService);
            Debug.Log("[BootstrapConfigInstaller] 配置初始化与校验完成。", this);
        }
        private void OnDestroy()
        {
            if (ConfigService.Active == configService)
            {
                ConfigService.ClearActive();
            }
        }
    }
}

