using UnityEngine;

namespace Game.Definition.ConfigSystem.Core
{
    /// <summary>
    /// 所有配置资产的统一基类。
    /// 运行时严禁修改配置资产内容；业务层只能通过 ConfigService 做只读查询。
    /// </summary>
    public abstract class ConfigBase : ScriptableObject, IConfigValidation
    {
        [SerializeField] private string configId;
        [SerializeField] private string displayName;
        [SerializeField][TextArea(2, 5)] private string description;
        [SerializeField] private bool isEnabled = true;

        public string ConfigId => ConfigIdUtility.Normalize(configId);
        public string DisplayName => displayName;
        public string Description => description;
        public bool IsEnabled => isEnabled;

        /// <summary>
        /// 默认只做基类通用校验；子类可覆写补充自身规则。
        /// </summary>
        public virtual void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            if (ConfigIdUtility.IsValid(configId) == false)
            {
                _context.AddError(name, "ConfigId 不能为空。");
            }
        }
    }
}
