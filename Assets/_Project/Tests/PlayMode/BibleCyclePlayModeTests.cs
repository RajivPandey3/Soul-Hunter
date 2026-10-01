using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Weapons;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS DOT Phase 1 row 8 (2026-10-01). vampire.survivors.wiki/w/King_Bible: the books orbit for the Duration (3 s),
    /// then rest; "its Cooldown is extended by its Duration of 3 seconds to 6 seconds total"; "1.7 seconds Hitbox Delay".
    /// </summary>
    public class BibleCyclePlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator TheBooks_OrbitFor3s_RestUntil6s_ThenComeBack()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            WeaponManager.Instance.ApplyUpgrade(WeaponDefinitionLookup.Find(UpgradeData.UpgradeType.Bible).GetLevel(1));
            yield return null;
            var bible = player.GetComponentsInChildren<BibleWeapon>().First();
            Assert.That(bible.VisibleSeconds, Is.EqualTo(3f).Within(0.01f), "Out for 3 s (Kael has no Duration bonus).");
            Assert.That(bible.CycleSeconds, Is.EqualTo(6f).Within(0.01f), "A new cycle every 3 s cooldown + 3 s duration.");
            Assert.That(bible.BooksOut, Is.True, "Out when gained.");
            var touch = bible.GetComponentsInChildren<TouchDamage>(true).First();
            Assert.That(touch.DamageInterval, Is.EqualTo(BibleWeapon.VsHitboxDelay), "1.7 s hitbox delay.");

            yield return new WaitForSeconds(3.3f);
            Assert.That(bible.BooksOut, Is.False, "Resting after 3 s.");
            yield return new WaitForSeconds(3.0f);
            Assert.That(bible.BooksOut, Is.True, "Back after 6 s.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
