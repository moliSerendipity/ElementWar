using System.IO;
using NUnit.Framework;

namespace Game.Tests.EditMode.Gameplay.Weapon
{
    /// <summary>
    /// 验证 WPN-010 复用的正式输入资源仍提供批准的元素弹药切换入口。
    /// </summary>
    public sealed class WeaponElementInputContractTests
    {
        private const string InputActionsAssetPath =
            "Assets/Settings/Input System/MyInputSystem.inputactions";

        /// <summary>`Combat/IsSwitchAmmo` 必须继续由键盘 T 触发。</summary>
        [Test]
        public void SwitchAmmoActionUsesApprovedTBinding()
        {
            string inputActionsJson = File.ReadAllText(InputActionsAssetPath);

            StringAssert.Contains("\"name\": \"IsSwitchAmmo\"", inputActionsJson);
            StringAssert.Contains("\"path\": \"<Keyboard>/t\"", inputActionsJson);
            StringAssert.Contains("\"action\": \"IsSwitchAmmo\"", inputActionsJson);
        }
    }
}
