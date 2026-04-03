namespace Game.Definition.ConfigSystem.Core
{
    /// <summary>
    /// 单条配置校验消息。
    /// Source 通常写配置 Id；如果配置 Id 本身非法，则退回资产名，保证日志能定位到来源。
    /// </summary>
    public readonly struct ConfigValidationMessage
    {
        public ConfigValidationMessage(ConfigValidationSeverity _severity, string _source, string _message)
        {
            Severity = _severity;
            Source = _source;
            Message = _message;
        }

        public ConfigValidationSeverity Severity { get; }

        public string Source { get; }

        public string Message { get; }
    }
}