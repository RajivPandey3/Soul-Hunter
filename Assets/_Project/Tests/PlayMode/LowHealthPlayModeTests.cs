using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner (2026-09-30): "player ka life kam hone per red flash pure screen pe aata hai, use hata do". At low health no
    /// red tint covers the screen (the old LowHealthWarning overlay pulsed at 30% health and below).
    /// </summary>
    public class LowHealthPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator LowHealth_PutsNoRedTintOverTheScreen()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var health = PlayerController.Instance.GetComponent<HealthController>();
            health.TakeDamage(new DamagePacket(Mathf.RoundToInt(health.MaxHealth * 0.9f), health.transform.position, Vector3.zero));
            health.IsInvincible = true;
            yield return new WaitForSeconds(1f);
            Assert.That(health.CurrentHealth, Is.LessThanOrEqualTo(health.MaxHealth * 0.3f), "The hero is at low health.");

            Assert.That(GameObject.Find("LowHealthWarning_Overlay"), Is.Null, "The old low-health overlay is gone.");
            // No reddish image stretched over the whole screen.
            var screenTints = Object.FindObjectsByType<Image>(FindObjectsSortMode.None)
                .Where(i => i.isActiveAndEnabled && i.color.a > 0.05f && i.color.r > 0.5f && i.color.g < 0.3f && i.color.b < 0.3f)
                .Where(i => { var r = i.rectTransform.rect; return r.width >= Screen.width * 0.9f && r.height >= Screen.height * 0.9f; })
                .Select(i => i.name).ToList();
            Assert.That(screenTints, Is.Empty, "Red full-screen images: " + string.Join(", ", screenTints));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
