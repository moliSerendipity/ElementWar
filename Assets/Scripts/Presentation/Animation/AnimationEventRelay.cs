using Game.Gameplay.Weapon;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Presentation.Animation
{
    /// <summary>
    /// 动画事件转发表现桥。
    /// 这里只负责把动画关键帧转成表现事件或调试信号，
    /// 不允许在这里决定命中、伤害、补弹是否合法成立。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnimationEventRelay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponRuntime weaponRuntime;
        [SerializeField] private CharacterAnimationBridge characterAnimationBridge;

        public void OnRemoveMagazine()
        {
            characterAnimationBridge.RemoveMagazine();
        }

        public void OnInsertMagazine()
        {
            characterAnimationBridge.InsertMagazine();
            weaponRuntime.NotifyReloadInsertMagazineKeyframe();
        }
    }
}
