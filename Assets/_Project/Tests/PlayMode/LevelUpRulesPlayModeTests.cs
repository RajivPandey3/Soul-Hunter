using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS DOT Phase 1 row 6 (2026-10-01). vampire.survivors.wiki/w/Reroll: "The items that were discarded by a reroll
    /// cannot appear in the new set of items." wiki Level_up: "Skips and Rerolls are unavailable when the player has maxed
    /// out their inventory"; "the option to always receive either Floor Chickens or Gold once the player has received 100
    /// Big Coin Bags from any source"; "Always Floor Chicken" gives gold at full health.
    /// </summary>
    public class LevelUpRulesPlayModeTests : GameplayPlayModeTestBase
    {
        private LevelUpManager _levelUp;
        private PlayerExperience _experience;

        private IEnumerator Boot(System.Action<GameData> save = null)
        {
            yield return BootIntoGameplay(save);
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;
            _levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            _experience = PlayerController.Instance.GetComponent<PlayerExperience>();
        }

        private List<UpgradeData> OpenLevelUp()
        {
            List<UpgradeData> choices = null;
            System.Action<List<UpgradeData>> grab = round => choices = round;
            _levelUp.OnLevelUpUIRound += grab;
            _experience.AddXP(_experience.XPToNextLevel - _experience.CurrentXP);
            _levelUp.OnLevelUpUIRound -= grab;
            return choices;
        }

        private void BanishEverything()
        {
            var banished = (HashSet<UpgradeData.UpgradeType>)typeof(LevelUpManager).GetField("_banishedTypes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_levelUp);
            foreach (UpgradeData.UpgradeType type in System.Enum.GetValues(typeof(UpgradeData.UpgradeType))) banished.Add(type);
        }

        [UnityTest]
        public IEnumerator AReroll_NeverOffersTheDiscardedItemsAgain()
        {
            yield return Boot();
            _levelUp.AddCharges(5, 0, 0);
            var first = OpenLevelUp();
            Assert.That(first, Is.Not.Null.And.Count.GreaterThanOrEqualTo(3));
            for (int i = 0; i < 5; i++)
            {
                List<UpgradeData> next = null;
                System.Action<List<UpgradeData>> grab = round => next = round;
                _levelUp.OnLevelUpUIRound += grab;
                Assert.That(_levelUp.Reroll(), Is.True);
                _levelUp.OnLevelUpUIRound -= grab;
                var overlap = next.Select(u => u.Type).Intersect(first.Select(u => u.Type)).ToList();
                Assert.That(overlap, Is.Empty, $"Reroll {i + 1}: discarded items came back: {string.Join(", ", overlap)}");
                first = next;
            }
            _levelUp.SelectUpgrade(first[0]);
            yield return null;
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator MaxedOut_NoRerollOrSkip_AndAfter100Bags_AlwaysChickenPaysOutWithoutAScreen()
        {
            yield return Boot(save => save.BigCoinBagsReceived = 0);
            var save = GameServices.Instance.Get<SaveService>();
            _levelUp.AddCharges(2, 2, 0);
            BanishEverything();

            // Nothing left to upgrade: Gold Pouch / Roast Chicken only; Reroll and Skip are off.
            var cards = OpenLevelUp();
            Assert.That(cards.Select(c => c.UpgradeName), Is.EqualTo(new[] { "Gold Pouch", "Roast Chicken" }), "Two cards before 100 bags.");
            Assert.That(_levelUp.CanReroll, Is.False, "No reroll on the maxed-out cards.");
            Assert.That(_levelUp.CanSkip, Is.False, "No skip on the maxed-out cards.");
            Assert.That(_levelUp.Reroll(), Is.False);
            Assert.That(_levelUp.Skip(), Is.False);
            _levelUp.SelectUpgrade(cards[0]); // Gold Pouch: a Big Coin Bag
            yield return null;
            Assert.That(save.CurrentData.BigCoinBagsReceived, Is.EqualTo(1), "The Gold Pouch counts as a Big Coin Bag.");

            // 100 bags: two "always" cards join.
            save.CurrentData.BigCoinBagsReceived = 100;
            cards = OpenLevelUp();
            Assert.That(cards.Count, Is.EqualTo(4), "Always Gold / Always Chicken offered after 100 bags.");
            var alwaysChicken = cards.First(c => c.UpgradeName == "Always Roast Chicken");
            int gold = save.CurrentData.Currency;
            _levelUp.SelectUpgrade(alwaysChicken); // at full health: gold instead (wiki)
            yield return null;
            Assert.That(save.CurrentData.Currency, Is.GreaterThan(gold), "Always Chicken at full health gives gold.");

            // From now on a maxed level-up pays out with no screen: hurt, then level up.
            var health = PlayerController.Instance.GetComponent<HealthController>();
            health.TakeDamage(new DamagePacket { Amount = 40, IgnoresHitInvulnerability = true });
            int hurt = health.CurrentHealth;
            Assert.That(OpenLevelUp(), Is.Null, "No level-up screen with an 'always' choice.");
            Assert.That(_levelUp.IsLevelUpActive, Is.False);
            Assert.That(health.CurrentHealth, Is.GreaterThan(hurt), "Always Chicken heals when health is missing.");
            Assert.That(GameTime.IsPaused, Is.False, "The game was not left paused.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
