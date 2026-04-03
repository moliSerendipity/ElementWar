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
        public WeaponFiredEvent(
            GameObject _weaponObject,
            string _weaponConfigId,
            int _remainingMagazineAmmo,
            Vector3 _shotOrigin,
            Vector3 _shotDirection,
            float _shotDistance,
            bool _hadHit,
            bool _hitDamageableTarget,
            CombatHitPartType _hitPartType,
            Vector3 _resolvedImpactPoint,
            Vector3 _resolvedImpactNormal,
            float _cameraKickPitch,
            float _cameraKickYaw,
            float _crosshairKick)
        {
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

        public GameObject WeaponObject { get; }
        public string WeaponConfigId { get; }
        public int RemainingMagazineAmmo { get; }
        public Vector3 ShotOrigin { get; }
        public Vector3 ShotDirection { get; }
        public float ShotDistance { get; }
        public bool HadHit { get; }
        public bool HitDamageableTarget { get; }
        public CombatHitPartType HitPartType { get; }
        public Vector3 ResolvedImpactPoint { get; }
        public Vector3 ResolvedImpactNormal { get; }
        public float CameraKickPitch { get; }
        public float CameraKickYaw { get; }
        public float CrosshairKick { get; }
    }
}
