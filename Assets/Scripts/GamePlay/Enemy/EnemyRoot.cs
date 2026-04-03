using Game.Definition.ConfigSystem.Core;
using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.Enemy
{
    /// <summary>
    /// 敌人最小装配根
    /// 当前阶段只负责显式初始化 EnemyStat 与生命链，避免依赖隐式执行顺序
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyRoot : MonoBehaviour
    {
        [SerializeField] private EnemyStat enemyStat;
        [SerializeField] private HealthComponent damageReceiver;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            enemyStat.TryInitialize(ConfigService.Active);
            damageReceiver.TryInitialize(ConfigService.Active);
        }

        private void ResolveReferences()
        {
            if (enemyStat == null)
            {
                enemyStat = GetComponent<EnemyStat>();
            }

            if (damageReceiver == null)
            {
                damageReceiver = GetComponent<HealthComponent>();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif
    }
}
