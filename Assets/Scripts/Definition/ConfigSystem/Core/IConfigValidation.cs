namespace Game.Definition.ConfigSystem.Core
{
    /// <summary>
    /// 允许配置资产在通用校验之外补充自身规则校验。
    /// 这里显式传入 ConfigService，是为了让主定义表能做外键存在性检查。
    /// </summary>
    public interface IConfigValidation
    {
        void Validate(ConfigValidationContext _context, ConfigService _configService);
    }
}