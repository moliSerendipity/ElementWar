using System.Collections.Generic;

namespace Game.Definition.ConfigSystem.Core
{
    /// <summary>
    /// 一次完整配置校验的消息收集容器。
    /// ValidationRunner 负责创建它，具体配置资产与服务层只向其中追加消息。
    /// </summary>
    public sealed class ConfigValidationContext
    {
        private readonly List<ConfigValidationMessage> messages = new();

        public IReadOnlyList<ConfigValidationMessage> Messages => messages;

        public bool HasError
        {
            get
            {
                for (int i = 0; i < messages.Count; i++)
                {
                    if (messages[i].Severity == ConfigValidationSeverity.Error)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void AddInfo(string _source, string _message)
        {
            messages.Add(new ConfigValidationMessage(ConfigValidationSeverity.Info, _source, _message));
        }

        public void AddWarning(string _source, string _message)
        {
            messages.Add(new ConfigValidationMessage(ConfigValidationSeverity.Warning, _source, _message));
        }

        public void AddError(string _source, string _message)
        {
            messages.Add(new ConfigValidationMessage(ConfigValidationSeverity.Error, _source, _message));
        }
    }
}
