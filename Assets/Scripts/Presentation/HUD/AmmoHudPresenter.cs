using Game.Gameplay.Character;
using Game.Gameplay.Weapon;
using Game.Definition.Combat;
using TMPro;
using UnityEngine;

namespace Game.Presentation.HUD
{
    /// <summary>
    /// 武器弹药 HUD 展示器。
    /// 当前阶段统一读取“当前装备武器”的 WeaponViewState，不直接下钻 WeaponRuntime 或执行器内部状态。
    ///
    /// 约束：
    /// 1. 它不再通过 FindObjectOfType 在场景里随便抓一个 WeaponViewState；
    /// 2. 它只从 CharacterActionController 提供的“当前装备武器视图”读取显示数据；
    /// 3. 后续接入 WeaponSlotController 时，只需要在角色动作层切换当前武器，HUD 无需改主逻辑。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AmmoHudPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterActionController sourceActionController;
        [SerializeField] private TMP_Text weaponNameTmpText;
        [SerializeField] private TMP_Text magazineAmmoTmpText;
        [SerializeField] private TMP_Text reserveAmmoTmpText;
        [SerializeField] private TMP_Text reloadStateTmpText;

        [Header("Behaviour")]
        [SerializeField] private bool hideReloadStateWhenIdle = true;

        private WeaponViewState currentWeaponViewState;

        private void Awake()
        {
            ResolveReferences();
            RefreshView(forceClearWhenInvalid: true);
        }

        private void LateUpdate()
        {
            WeaponViewState resolvedWeaponViewState = sourceActionController != null
                ? sourceActionController.CurrentWeaponViewState
                : null;

            // 当前武器发生切换时，HUD 只更新绑定目标，不重新走场景搜索。
            if (ReferenceEquals(currentWeaponViewState, resolvedWeaponViewState) == false)
            {
                currentWeaponViewState = resolvedWeaponViewState;
            }

            RefreshView(forceClearWhenInvalid: true);
        }

        /// <summary>
        /// 允许外部显式设置动作源。
        /// 如果后续 HUD 是由玩家角色动态接管的，应通过这个入口切换，而不是让 HUD 自己搜索角色对象。
        /// </summary>
        public void SetSourceActionController(CharacterActionController _sourceActionController)
        {
            sourceActionController = _sourceActionController;
            currentWeaponViewState = sourceActionController != null ? sourceActionController.CurrentWeaponViewState : null;
            RefreshView(forceClearWhenInvalid: true);
        }

        private void RefreshView(bool forceClearWhenInvalid)
        {
            if (currentWeaponViewState == null)
            {
                if (forceClearWhenInvalid)
                {
                    ApplyInvalidState();
                }

                return;
            }

            string weaponName = string.IsNullOrWhiteSpace(currentWeaponViewState.WeaponDefinitionConfigId)
                ? "Weapon"
                : currentWeaponViewState.WeaponDefinitionConfigId;
            if (currentWeaponViewState.CurrentAmmoElement != ElementType.None)
            {
                // WPN-010 的最小调试反馈；正式元素图标随后续 HUD 切片接入。
                weaponName = $"{weaponName} [{currentWeaponViewState.CurrentAmmoElement}]";
            }

            SetText(weaponNameTmpText, weaponName);
            SetText(magazineAmmoTmpText, currentWeaponViewState.CurrentMagazineAmmo.ToString());
            SetText(reserveAmmoTmpText, currentWeaponViewState.CurrentReserveAmmo.ToString());

            if (currentWeaponViewState.IsReloading)
            {
                SetReloadState("Reloading", visible: true);
            }
            else if (hideReloadStateWhenIdle)
            {
                SetReloadState(string.Empty, visible: false);
            }
            else
            {
                SetReloadState("Ready", visible: true);
            }
        }

        private void ApplyInvalidState()
        {
            SetText(weaponNameTmpText, "--");
            SetText(magazineAmmoTmpText, "0");
            SetText(reserveAmmoTmpText, "0");

            if (hideReloadStateWhenIdle)
            {
                SetReloadState(string.Empty, visible: false);
            }
            else
            {
                SetReloadState("No Weapon", visible: true);
            }
        }

        private void SetReloadState(string _text, bool visible)
        {
            SetText(reloadStateTmpText, _text);

            if (reloadStateTmpText != null)
            {
                reloadStateTmpText.enabled = visible;
            }
        }

        private static void SetText(TMP_Text _tmpText, string _value)
        {
            if (_tmpText != null)
            {
                _tmpText.text = _value;
            }
        }

        private void ResolveReferences()
        {
            if (sourceActionController == null)
            {
                sourceActionController = FindObjectOfType<CharacterActionController>(true);
            }

            currentWeaponViewState = sourceActionController != null ? sourceActionController.CurrentWeaponViewState : null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            currentWeaponViewState = sourceActionController != null ? sourceActionController.CurrentWeaponViewState : null;
            RefreshView(forceClearWhenInvalid: true);
        }
#endif
    }
}
