using UnityEngine;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.Presentation
{
    public enum AudioEventType
    {
        OneShot2D = 0,
        OneShot3D = 1,
        Loop2D = 2,
        Loop3D = 3,
    }

    /// <summary>
    /// 音频事件配置。
    /// 当前版本先服务枪械开火、命中与换弹等表现层音频映射。
    /// </summary>
    [CreateAssetMenu(fileName = "AudioEventConfig", menuName = "Game/Configs/Presentation/Audio Event Config")]
    public sealed class AudioEventConfig : ConfigBase
    {
        [SerializeField] private AudioEventType eventType = AudioEventType.OneShot3D;
        [SerializeField] private AudioClip audioClip;
        [Range(0f, 1f)][SerializeField] private float volume = 1f;
        [Range(0f, 1f)][SerializeField] private float spatialBlend = 1f;
        [SerializeField] private bool loop;
        [Min(1)][SerializeField] private int maxInstanceCount = 8;
        [Min(0f)][SerializeField] private float cooldown;

        public AudioEventType EventType => eventType;
        public AudioClip AudioClip => audioClip;
        public float Volume => volume;
        public float SpatialBlend => spatialBlend;
        public bool Loop => loop;
        public int MaxInstanceCount => maxInstanceCount;
        public float Cooldown => cooldown;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (audioClip == null)
            {
                _context.AddWarning(ConfigId, "AudioClip 为空。当前事件不会播出任何声音。");
            }
        }
    }
}
