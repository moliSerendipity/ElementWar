using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.Weapon.Events
{
    /// <summary>
    /// 已提交的开火事件。
    /// 仅在真正完成扣弹与命中请求抛出后发布。
    /// </summary>
    public readonly struct WeaponFiredEvent
    {
        /// <summary>
        /// 构造一条已提交的开火事实事件。
        /// </summary>
        /// <param name="_executionId">本次成立开火的攻击执行身份。</param>
        /// <param name="_instigatorId">开火时冻结的责任实体身份。</param>
        /// <param name="_targetId">命中查询时冻结的目标身份；未命中战斗目标时无效。</param>
        /// <param name="_weaponObject">发布本次开火的武器对象。</param>
        /// <param name="_weaponConfigId">本次开火使用的武器配置 ID。</param>
        /// <param name="_remainingMagazineAmmo">扣弹后的弹匣余量。</param>
        /// <param name="_shotOrigin">逻辑射线起点的世界坐标。</param>
        /// <param name="_shotDirection">逻辑射线的世界空间方向。</param>
        /// <param name="_shotDistance">本次射线采用的最大射程。</param>
        /// <param name="_hadHit">射线是否命中任意 Collider。</param>
        /// <param name="_hitDamageableTarget">命中上下文是否解析到生命组件。</param>
        /// <param name="_hitPartType">命中上下文解析出的部位类型。</param>
        /// <param name="_resolvedImpactPoint">表现层应使用的命中或射线终点。</param>
        /// <param name="_resolvedImpactNormal">表现层应使用的表面法线或反向射线方向。</param>
        /// <param name="_cameraKickPitch">本次开火的相机俯仰反馈量。</param>
        /// <param name="_cameraKickYaw">本次开火的相机水平反馈量。</param>
        /// <param name="_crosshairKick">本次开火的准星反馈量。</param>
        public WeaponFiredEvent(
            AttackExecutionId _executionId,
            CombatantId _instigatorId,
            CombatantId _targetId,
            GameObject _weaponObject,
            string _weaponConfigId,
            int _remainingMagazineAmmo,
            Vector3 _shotOrigin,
            Vector3 _shotDirection,
            float _shotDistance,
            bool _hadHit,
            bool _hitDamageableTarget,
            HitPartType _hitPartType,
            Vector3 _resolvedImpactPoint,
            Vector3 _resolvedImpactNormal,
            float _cameraKickPitch,
            float _cameraKickYaw,
            float _crosshairKick)
        {
            ExecutionId = _executionId;
            InstigatorId = _instigatorId;
            TargetId = _targetId;
            WeaponObject = _weaponObject;
            WeaponConfigId = _weaponConfigId;
            RemainingMagazineAmmo = _remainingMagazineAmmo;
            ShotOrigin = _shotOrigin;
            ShotDirection = _shotDirection;
            ShotDistance = _shotDistance;
            HadHit = _hadHit;
            HitDamageableTarget = _hitDamageableTarget;
            HitPartType = _hitPartType;
            ResolvedImpactPoint = _resolvedImpactPoint;
            ResolvedImpactNormal = _resolvedImpactNormal;
            CameraKickPitch = _cameraKickPitch;
            CameraKickYaw = _cameraKickYaw;
            CrosshairKick = _crosshairKick;
        }

        /// <summary>本次成立开火的攻击执行身份。</summary>
        public AttackExecutionId ExecutionId { get; }

        /// <summary>开火时冻结的责任实体身份。</summary>
        public CombatantId InstigatorId { get; }

        /// <summary>命中查询时冻结的目标身份；未命中战斗目标时无效。</summary>
        public CombatantId TargetId { get; }
        public GameObject WeaponObject { get; }
        public string WeaponConfigId { get; }
        public int RemainingMagazineAmmo { get; }
        public Vector3 ShotOrigin { get; }
        public Vector3 ShotDirection { get; }
        public float ShotDistance { get; }
        public bool HadHit { get; }
        public bool HitDamageableTarget { get; }
        public HitPartType HitPartType { get; }
        public Vector3 ResolvedImpactPoint { get; }
        public Vector3 ResolvedImpactNormal { get; }
        public float CameraKickPitch { get; }
        public float CameraKickYaw { get; }
        public float CrosshairKick { get; }
    }
}
