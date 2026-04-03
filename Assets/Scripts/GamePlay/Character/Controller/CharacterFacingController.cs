using UnityEngine;

namespace Game.Gameplay.Character
{
    /// <summary>
    /// 角色朝向执行器。
    /// 统一维护 yaw / pitch 缓存，并让角色水平朝向默认跟随相机 yaw。
    ///
    /// 约束：
    /// 1. 当前帧只显式消费 CharacterFramePlan；
    /// 2. 不再自行回头读取 Resolver 当前计划；
    /// 3. 不提交 CharacterFacts，只输出 yaw / pitch 执行结果给 Root 同步表现态。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterFacingController : MonoBehaviour
    {
        [Header("Facing Settings")]
        [SerializeField] private float lookYawSensitivity = 0.18f;
        [SerializeField] private float lookPitchSensitivity = 0.12f;
        [SerializeField] private float minPitch = -100f;
        [SerializeField] private float maxPitch = 150f;

        private float currentYaw;
        private float currentPitch;

        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;
        public float CurrentYaw => currentYaw;
        public float CurrentPitch => currentPitch;

        private void Awake()
        {
            ResolveReferences();
            SyncInitialRotation();
        }

        /// <summary>
        /// 提交一次真实后坐力增量。
        /// 该增量已经由 Weapon 域裁决完成，这里只负责把结果写入真实视角。
        /// </summary>
        public void ApplyRecoil(float _pitchDelta, float _yawDelta)
        {
            // 真实后坐力直接写入当前控制视角；后续是否抵消完全由玩家输入决定。
            currentYaw += _yawDelta;
            currentPitch += _pitchDelta;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

            // 当前帧若在 Action 后追加了后坐力，需要立刻把角色水平朝向同步到最新 yaw。
            transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        }

        /// <summary>
        /// 执行当前帧已裁决的看向输入。
        /// </summary>
        public void Execute(in CharacterFramePlan _plan)
        {
            ApplyLookDelta(_plan.LookDelta);

            transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        }

        private void ApplyLookDelta(Vector2 _lookDelta)
        {
            if (_lookDelta.sqrMagnitude <= 0f)
            {
                return;
            }

            currentYaw += _lookDelta.x * lookYawSensitivity;
            currentPitch -= _lookDelta.y * lookPitchSensitivity;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }

        private void SyncInitialRotation()
        {
            currentYaw = transform.eulerAngles.y;
            currentPitch = 0f;
        }

        private void ResolveReferences()
        {

        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif
    }
}
