using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Draw calls (2026-10-15): every damage number on screen is drawn by DamagePopupBatcher in one draw. Each number keeps
    /// its own TextMeshPro (enabled, not drawn), still rises, faces the camera and fades; switching the batcher off hands
    /// every number back to its own renderer.
    /// </summary>
    public class DamagePopupBatcherPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator DamageNumbers_AreDrawnInOneDraw_AndHandBackWhenSwitchedOff()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            PlayerController.Instance.GetComponent<SoulHunter.Gameplay.Combat.HealthController>().IsInvincible = true;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<SoulHunter.Gameplay.Combat.AutoAttackWeapon>(true))
                weapon.enabled = false;
            Assert.That(DamagePopupBatcher.Enabled, Is.True, "On by default.");
            Vector3 centre = PlayerController.Instance.transform.position + Vector3.up;
            for (int i = 0; i < 20; i++)
                DamagePopupManager.Instance.ShowDamage(10 + i * 37, centre + Quaternion.Euler(0f, i * 18f, 0f) * Vector3.forward * 4f);
            yield return null;

            var popups = Object.FindObjectsByType<DamagePopup>(FindObjectsSortMode.None).Where(p => p.gameObject.activeInHierarchy).ToList();
            Assert.That(popups.Count, Is.GreaterThanOrEqualTo(20));
            Assert.That(DamagePopupBatcher.Exists, Is.True, "The batcher is made with the first number.");
            foreach (var popup in popups)
            {
                var text = popup.GetComponent<TMPro.TextMeshPro>();
                Assert.That(text.renderer.enabled && text.renderer.forceRenderingOff, Is.True,
                    "A number's own renderer stays enabled but is drawn by the batcher.");
            }
            Assert.That(DamagePopupBatcher.LastPopups, Is.GreaterThanOrEqualTo(20), "Every number on screen is in the shared mesh.");
            Assert.That(DamagePopupBatcher.LastDraws, Is.EqualTo(1), "All numbers in one draw.");

            // The numbers still rise and fade.
            var first = popups[0];
            Vector3 before = first.transform.position;
            yield return new WaitForSeconds(0.2f);
            Assert.That(first.transform.position.y, Is.GreaterThan(before.y + 0.05f), "A batched number still rises.");

            DamagePopupBatcher.Enabled = false;
            yield return null;
            yield return null;
            foreach (var popup in popups.Where(p => p != null && p.gameObject.activeInHierarchy))
                Assert.That(popup.GetComponent<TMPro.TextMeshPro>().renderer.forceRenderingOff, Is.False, "Switched off, a number draws itself again.");
            Assert.That(DamagePopupBatcher.LastPopups, Is.EqualTo(0));

            DamagePopupBatcher.Enabled = true;
            DamagePopupManager.Instance.ShowDamage(99, centre);
            yield return null;
            Assert.That(DamagePopupBatcher.LastDraws, Is.EqualTo(1), "On again, numbers are batched again.");

            // Every number ends and leaves the batch.
            yield return new WaitForSeconds(DamagePopup.MaxLifetime + 0.3f);
            Assert.That(DamagePopupBatcher.LastPopups, Is.EqualTo(0), "Ended numbers are not drawn.");
            Assert.That(DamagePopupBatcher.LastDraws, Is.EqualTo(0));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
