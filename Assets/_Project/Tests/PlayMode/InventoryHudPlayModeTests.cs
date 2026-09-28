using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// The HUD inventory row in the real game. Found 2026-09-28: HUD_Canvas had no slots wired and no WeaponManager, so no
    /// weapon or passive icon was ever shown.
    /// </summary>
    public class InventoryHudPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator Hud_ShowsTheStartingWeapon_ThenEachNewItem_InOrder()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var hud = Object.FindFirstObjectByType<InventoryHud>();
            Assert.That(hud, Is.Not.Null, "The HUD builds its inventory row.");
            var weapons = WeaponManager.Instance;

            Assert.That(hud.ShownWeapons.Select(s => s.Type), Is.EqualTo(new[] { UpgradeType.Whip }), "Kael's starting Whip.");
            Assert.That(hud.WeaponIcon(0).enabled && hud.WeaponIcon(0).sprite != null, Is.True, "The Whip shows its icon.");
            Assert.That(hud.ShownPassives, Is.Empty);

            var knife = ScriptableObject.CreateInstance<UpgradeData>();
            knife.Type = UpgradeType.Knife; knife.Level = 1;
            var spinach = ScriptableObject.CreateInstance<UpgradeData>();
            spinach.Type = UpgradeType.Spinach; spinach.Level = 1;
            try
            {
                weapons.ApplyUpgrade(spinach);
                weapons.ApplyUpgrade(knife);
                yield return null;
                Assert.That(hud.ShownWeapons.Select(s => s.Type), Is.EqualTo(new[] { UpgradeType.Whip, UpgradeType.Knife }));
                Assert.That(hud.ShownPassives.Select(s => s.Type), Is.EqualTo(new[] { UpgradeType.Spinach }));
                Assert.That(hud.WeaponIcon(1).sprite, Is.Not.Null.And.Not.EqualTo(hud.WeaponIcon(0).sprite), "Each item has its own icon.");
                Assert.That(hud.WeaponIcon(2).enabled, Is.False, "Empty slots show no icon.");
            }
            finally
            {
                Object.Destroy(knife);
                Object.Destroy(spinach);
            }

            // The row sits under the Gold label, on screen, and does not take clicks.
            var rect = (RectTransform)hud.transform;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var gold = GameObject.Find("Gold_Text");
            if (gold != null)
            {
                var goldCorners = new Vector3[4];
                ((RectTransform)gold.transform).GetWorldCorners(goldCorners);
                Assert.That(corners[1].y, Is.LessThanOrEqualTo(goldCorners[0].y + 1f), "Below the Gold label.");
            }
            Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(0f));
            Assert.That(corners[0].y, Is.GreaterThan(Screen.height * 0.5f), "In the top half of the screen.");
            Assert.That(hud.WeaponFrame(0).raycastTarget, Is.False);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
