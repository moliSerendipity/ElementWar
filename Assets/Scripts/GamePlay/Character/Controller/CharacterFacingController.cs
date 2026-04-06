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
    /// 3. 不提交 CharacterFacts，只输出 yaw / pitch 执行结果给 Root 同步表现态；
    /// 4. 灵敏度正式来源为 CharacterStat，不再让 FacingController 自己维护另一套 Gameplay 真相源。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterFacingController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterStat characterStat;

        [Header("Facing Settings")]
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
            ApplyLookDelta(_plan.LookDelta, _plan.AimActive);
            transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        }

        /// <summary>
        /// 根据当前帧是否处于瞄准态，使用不同的 yaw / pitch 灵敏度。
        /// </summary>
        private void ApplyLookDelta(Vector2 _lookDelta, bool _aimActive)
        {
            if (_lookDelta.sqrMagnitude <= 0f)
            {
                return;
            }

            float yawSensitivity = ResolveYawSensitivity(_aimActive);
            float pitchSensitivity = ResolvePitchSensitivity(_aimActive);

            currentYaw += _lookDelta.x * yawSensitivity;
            currentPitch -= _lookDelta.y * pitchSensitivity;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }

        /// <summary>
        /// 解析当前帧的水平灵敏度。
        /// </summary>
        private float ResolveYawSensitivity(bool _aimActive)
        {
            if (characterStat == null)
            {
                return _aimActive ? 0.12f : 0.18f;
            }

            return _aimActive ? characterStat.AimYawSensitivity : characterStat.NormalYawSensitivity;
        }

        /// <summary>
        /// 解析当前帧的垂直灵敏度。
        /// </summary>
        private float ResolvePitchSensitivity(bool _aimActive)
        {
            if (characterStat == null)
            {
                return _aimActive ? 0.08f : 0.12f;
            }

            return _aimActive ? characterStat.AimPitchSensitivity : characterStat.NormalPitchSensitivity;
        }

        private void SyncInitialRotation()
        {
            currentYaw = transform.eulerAngles.y;
            currentPitch = 0f;
        }

        private void ResolveReferences()
        {
            if (characterStat == null)
            {
                characterStat = GetComponent<CharacterStat>();
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
