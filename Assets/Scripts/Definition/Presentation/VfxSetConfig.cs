using UnityEngine;
using Game.Definition.ConfigSystem.Core;

namespace Game.Definition.Presentation
{
    /// <summary>
    /// VFX 资源集合配置。
    /// 当前版本直接持有表现资源和可选池配置引用，
    /// 不再让表现层在运行时继续解释字符串 Id。
    /// </summary>
    [CreateAssetMenu(fileName = "VfxSetConfig", menuName = "Game/Configs/Presentation/Vfx Set Config")]
    public sealed class VfxSetConfig : ConfigBase
    {
        [SerializeField] private GameObject spawnVfxPrefab;
        [SerializeField] private GameObject loopVfxPrefab;
        [SerializeField] private GameObject hitVfxPrefab;
        [SerializeField] private GameObject breakVfxPrefab;
        [SerializeField] private GameObject destroyVfxPrefab;

        [SerializeField] private PoolConfig spawnVfxPoolConfig;
        [SerializeField] private PoolConfig loopVfxPoolConfig;
        [SerializeField] private PoolConfig hitVfxPoolConfig;
        [SerializeField] private PoolConfig breakVfxPoolConfig;
        [SerializeField] private PoolConfig destroyVfxPoolConfig;

        [SerializeField, Min(0.01f)] private float defaultLifeTime = 1.5f;

        public GameObject SpawnVfxPrefab => spawnVfxPrefab;
        public GameObject LoopVfxPrefab => loopVfxPrefab;
        public GameObject HitVfxPrefab => hitVfxPrefab;
        public GameObject BreakVfxPrefab => breakVfxPrefab;
        public GameObject DestroyVfxPrefab => destroyVfxPrefab;

        public PoolConfig SpawnVfxPoolConfig => spawnVfxPoolConfig;
        public PoolConfig LoopVfxPoolConfig => loopVfxPoolConfig;
        public PoolConfig HitVfxPoolConfig => hitVfxPoolConfig;
        public PoolConfig BreakVfxPoolConfig => breakVfxPoolConfig;
        public PoolConfig DestroyVfxPoolConfig => destroyVfxPoolConfig;

        public float DefaultLifeTime => defaultLifeTime;

        public override void Validate(ConfigValidationContext _context, ConfigService _configService)
        {
            base.Validate(_context, _configService);

            if (spawnVfxPrefab == null && loopVfxPrefab == null && hitVfxPrefab == null && breakVfxPrefab == null && destroyVfxPrefab == null)
            {
                _context.AddWarning(ConfigId, "当前 VfxSetConfig 未绑定任何 Prefab。");
            }
        }
    }
}
