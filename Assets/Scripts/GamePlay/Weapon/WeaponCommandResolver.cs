using Game.Definition.Weapon;
using Game.Gameplay.Character;
using UnityEngine;

namespace Game.Gameplay.Weapon
{
    public enum WeaponFireFailureReason
    {
        None = 0,
        NotInitialized = 1,
        MissingConfig = 2,
        Cooldown = 3,
        EmptyMagazine = 4,
        Reloading = 5,
        FireInputNotSatisfied = 6,
        BlockedByCharacterState = 7,
    }

    public enum WeaponReloadFailureReason
    {
        None = 0,
        NotInitialized = 1,
        MissingConfig = 2,
        AlreadyReloading = 3,
        MagazineFull = 4,
        NoReserveAmmo = 5,
        Disabled = 6,
    }

    /// <summary>
    /// Weapon 域唯一裁决点。
    /// 只把请求、武器运行时和角色事实收敛成 WeaponFramePlan，不在这里改任何长期事实。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponCommandResolver : MonoBehaviour
    {
        [SerializeField] private WeaponRuntime weaponRuntime;

        private void Awake()
        {
            ResolveReferences();
        }

        public WeaponFramePlan Resolve(WeaponRequest _request, CharacterFacts _characterFacts, float _currentTime)
        {
            if (weaponRuntime == null || weaponRuntime.IsInitialized == false || weaponRuntime.WeaponAmmoComponent == null)
            {
                return WeaponFramePlan.CreateInvalid(
                    WeaponFireFailureReason.NotInitialized,
                    WeaponReloadFailureReason.NotInitialized);
            }

            bool reloadTriggered = false;
            bool isEmptyReload = false;
            float reloadDuration = 0f;
            WeaponReloadFailureReason reloadFailureReason = WeaponReloadFailureReason.None;

            if (_request.ReloadTriggered)
            {
                reloadTriggered = CanStartReload(out reloadFailureReason);
                if (reloadTriggered)
                {
                    isEmptyReload = weaponRuntime.WeaponAmmoComponent.IsMagazineEmpty();
                    reloadDuration = weaponRuntime.ResolveReloadDuration(isEmptyReload);
                }
            }

            if (reloadTriggered)
            {
                WeaponFireFailureReason blockedFireReason = _request.HasFireIntent ? WeaponFireFailureReason.Reloading : WeaponFireFailureReason.None;
                return WeaponFramePlan.CreateResolved(
                    false,
                    false,
                    true,
                    false,
                    isEmptyReload,
                    reloadDuration,
                    blockedFireReason,
                    reloadFailureReason);
            }

            bool fireTriggered = false;
            bool dryFireTriggered = false;
            bool autoReloadAfterFire = false;
            WeaponFireFailureReason fireFailureReason = WeaponFireFailureReason.None;

            if (_request.HasFireIntent)
            {
                if (CanFire(_request, _characterFacts, _currentTime, out fireFailureReason))
                {
                    fireTriggered = true;

                    autoReloadAfterFire = weaponRuntime.CurrentMagazineAmmo == 1
                        && weaponRuntime.CurrentReserveAmmo > 0;
                }
                else if (fireFailureReason == WeaponFireFailureReason.EmptyMagazine)
                {
                    dryFireTriggered = true;

                    if (CanStartReload(out WeaponReloadFailureReason autoReloadFailureReason))
                    {
                        reloadTriggered = true;
                        reloadFailureReason = autoReloadFailureReason;
                        isEmptyReload = true;
                        reloadDuration = weaponRuntime.ResolveReloadDuration(true);
                    }
                }
            }

            return WeaponFramePlan.CreateResolved(
                fireTriggered,
                dryFireTriggered,
                reloadTriggered,
                autoReloadAfterFire,
                isEmptyReload,
                reloadDuration,
                fireFailureReason,
                reloadFailureReason);
        }

        private bool CanStartReload(out WeaponReloadFailureReason _failureReason)
        {
            _failureReason = WeaponReloadFailureReason.None;

            if (weaponRuntime.IsReloading)
            {
                _failureReason = WeaponReloadFailureReason.AlreadyReloading;
                return false;
            }

            if (weaponRuntime.WeaponAmmoComponent.IsMagazineFull())
            {
                _failureReason = WeaponReloadFailureReason.MagazineFull;
                return false;
            }

            if (weaponRuntime.WeaponAmmoComponent.HasReserveAmmo() == false)
            {
                _failureReason = WeaponReloadFailureReason.NoReserveAmmo;
                return false;
            }

            return true;
        }

        private bool CanFire(WeaponRequest _request, CharacterFacts _characterFacts, float _currentTime, out WeaponFireFailureReason _failureReason)
        {
            _failureReason = WeaponFireFailureReason.None;

            if (ShouldAcceptFireInput(_request) == false)
            {
                _failureReason = WeaponFireFailureReason.FireInputNotSatisfied;
                return false;
            }

            if (weaponRuntime.IsReloading)
            {
                _failureReason = WeaponFireFailureReason.Reloading;
                return false;
            }

            if (_currentTime < weaponRuntime.NextAllowedFireTime)
            {
                _failureReason = WeaponFireFailureReason.Cooldown;
                return false;
            }

            if (_characterFacts != null)
            {
                if (_characterFacts.IsDead)
                {
                    _failureReason = WeaponFireFailureReason.BlockedByCharacterState;
                    return false;
                }

                if (_characterFacts.IsSprinting && weaponRuntime.CanSprintFire == false)
                {
                    _failureReason = WeaponFireFailureReason.BlockedByCharacterState;
                    return false;
                }

                if (_characterFacts.IsGrounded == false && weaponRuntime.CanFireInAir == false)
                {
                    _failureReason = WeaponFireFailureReason.BlockedByCharacterState;
                    return false;
                }
            }

            if (weaponRuntime.WeaponAmmoComponent.HasAmmoInMagazine() == false)
            {
                _failureReason = WeaponFireFailureReason.EmptyMagazine;
                return false;
            }

            return true;
        }

        private bool ShouldAcceptFireInput(WeaponRequest _request)
        {
            if (weaponRuntime == null || weaponRuntime.WeaponDefinitionConfig == null)
            {
                return false;
            }

            WeaponFireMode fireMode = weaponRuntime.WeaponDefinitionConfig.FireMode;
            return fireMode == WeaponFireMode.Auto
                ? _request.FireHeld || _request.FirePressed
                : _request.FirePressed;
        }

        private void ResolveReferences()
        {
            if (weaponRuntime == null)
            {
                weaponRuntime = GetComponent<WeaponRuntime>();
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
