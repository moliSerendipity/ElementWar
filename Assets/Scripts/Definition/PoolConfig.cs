using Game.Definition.ConfigSystem.Core;
using UnityEngine;

namespace Game.Definition
{
    /// <summary>
    /// 对象池预设配置。
    /// 当前版本先服务池键、预热数量、容量上限与扩容规则等正式配置语义。
    /// 具体实例化来源仍由项目资源接入层负责。
    /// </summary>
    [CreateAssetMenu(fileName = "PoolConfig", menuName = "Game/Configs/Presentation/Pool Config")]
    public sealed class PoolConfig : ConfigBase
    {
        [SerializeField] private string poolKey;
        [SerializeField] private string prefabAddress;
        [SerializeField, Min(0)] private int prewarmCount;
        [SerializeField, Min(0)] private int maxPoolSize;
        [SerializeField] private bool allowExpand = true;

        public string PoolKey => ConfigIdUtility.Normalize(poolKey);
        public string PrefabAddress => prefabAddress;
        public int PrewarmCount => prewarmCount;
        public int MaxPoolSize => maxPoolSize;
        public bool AllowExpand => allowExpand;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (ConfigIdUtility.IsValid(poolKey) == false)
            {
                _context.AddError(ConfigId, "PoolKey 不能为空。");
            }

            if (string.IsNullOrWhiteSpace(prefabAddress))
            {
                _context.AddWarning(ConfigId, "PrefabAddress 为空，当前池配置无法用于资源装载。");
            }

            if (maxPoolSize > 0 && prewarmCount > maxPoolSize)
            {
                _context.AddWarning(ConfigId, "PrewarmCount 大于 MaxPoolSize，运行时会被容量上限截断。");
            }
        }
    }
}
