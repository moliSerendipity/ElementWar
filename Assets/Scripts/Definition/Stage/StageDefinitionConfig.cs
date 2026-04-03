using UnityEngine;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.Stage
{
    /// <summary>
    /// 关卡主定义表。
    /// 当前阶段先承载场景地址与流程外键入口，等波次与刷怪表落地后再继续细化。
    /// </summary>
    [CreateAssetMenu(fileName = "StageDefinitionConfig", menuName = "Game/Configs/Stage/Stage Definition Config")]
    public sealed class StageDefinitionConfig : ConfigBase
    {
        [SerializeField] private string sceneAddress;
        [SerializeField] private string waveGroupId;
        [SerializeField] private string resultPanelWidgetConfigId;

        public string SceneAddress => sceneAddress != null ? sceneAddress.Trim() : string.Empty;
        public string WaveGroupId => ConfigIdUtility.Normalize(waveGroupId);
        public string ResultPanelWidgetConfigId => ConfigIdUtility.Normalize(resultPanelWidgetConfigId);

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (string.IsNullOrWhiteSpace(sceneAddress))
            {
                _context.AddError(ConfigId, "sceneAddress 不能为空。");
            }
        }
    }
}
