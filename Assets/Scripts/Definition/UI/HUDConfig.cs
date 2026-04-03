using UnityEngine;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.UI
{
    /// <summary>
    /// HUD 表现开关与基础样式引用。
    /// 这是表现层配置，不允许反向控制 Gameplay 逻辑。
    /// </summary>
    [CreateAssetMenu(fileName = "HUDConfig", menuName = "Game/Configs/UI/HUD Config")]
    public sealed class HUDConfig : ConfigBase
    {
        [SerializeField] private bool showCrosshair = true;
        [SerializeField] private bool showAmmo = true;
        [SerializeField] private bool showBuffBar = true;
        [SerializeField] private bool showTeammateStatus = true;
        [SerializeField] private bool showDamageNumber = true;
        [SerializeField] private string damageNumberStyleId;

        public bool ShowCrosshair => showCrosshair;
        public bool ShowAmmo => showAmmo;
        public bool ShowBuffBar => showBuffBar;
        public bool ShowTeammateStatus => showTeammateStatus;
        public bool ShowDamageNumber => showDamageNumber;
        public string DamageNumberStyleId => ConfigIdUtility.Normalize(damageNumberStyleId);
    }
}
