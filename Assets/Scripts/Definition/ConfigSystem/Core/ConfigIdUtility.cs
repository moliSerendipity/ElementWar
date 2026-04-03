using System;

namespace Game.Definition.ConfigSystem.Core
{
    /// <summary>
    /// 配置 Id 归一化与基础合法性工具。
    /// Step 2 阶段只做最基础的空值与字符串比较处理，
    /// 不在这里引入项目特定前缀规则，避免和配置文档后续调整互相绑定。
    /// </summary>
    public static class ConfigIdUtility
    {
        public static string Normalize(string _configId)
        {
            return string.IsNullOrWhiteSpace(_configId)
                ? string.Empty
                : _configId.Trim();
        }

        public static bool IsValid(string _configId)
        {
            return string.IsNullOrWhiteSpace(Normalize(_configId)) == false;
        }

        public static bool EqualsNormalized(string _left, string _right)
        {
            return string.Equals(Normalize(_left), Normalize(_right), StringComparison.Ordinal);
        }
    }
}
