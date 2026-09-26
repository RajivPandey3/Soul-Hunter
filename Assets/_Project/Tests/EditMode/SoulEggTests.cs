using System.Collections.Generic;
using NUnit.Framework;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>Soul Eggs follow the VS Golden Egg table: 19 stats, fixed amounts, VS caps, permanent per character.</summary>
    public class SoulEggTests
    {
        private readonly List<GameObject> _cleanup = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _cleanup) if (go != null) Object.DestroyImmediate(go);
            _cleanup.Clear();
        }

        [Test]
        public void EachStat_RisesByTheVsEggAmount()
        {
            Assert.That(SoulEggs.StatCount, Is.EqualTo(19));
            var expected = new Dictionary<SoulEggs.Stat, float>
            {
                { SoulEggs.Stat.MaxHealth, 1f }, { SoulEggs.Stat.Recovery, 0.1f }, { SoulEggs.Stat.Armor, 0.1f },
                { SoulEggs.Stat.Amount, 0.1f }, { SoulEggs.Stat.Revival, 0.1f }, { SoulEggs.Stat.Reroll, 0.1f },
                { SoulEggs.Stat.Skip, 0.2f }, { SoulEggs.Stat.Banish, 0.2f }, { SoulEggs.Stat.Cooldown, 0.005f },
                { SoulEggs.Stat.MoveSpeed, 0.01f }, { SoulEggs.Stat.Might, 0.01f }, { SoulEggs.Stat.Speed, 0.01f },
                { SoulEggs.Stat.Duration, 0.01f }, { SoulEggs.Stat.Area, 0.01f }, { SoulEggs.Stat.Luck, 0.01f },
                { SoulEggs.Stat.Growth, 0.01f }, { SoulEggs.Stat.Greed, 0.01f }, { SoulEggs.Stat.Curse, 0.01f },
                { SoulEggs.Stat.Magnet, 0.01f },
            };
            foreach (var pair in expected)
                Assert.That(SoulEggs.AmountPerEgg(pair.Key), Is.EqualTo(pair.Value).Within(1e-6f), pair.Key.ToString());
        }

        [Test]
        public void TenArmorEggs_MakeOneWholeArmorPoint()
        {
            var data = new GameData();
            for (int i = 0; i < 9; i++) SoulEggs.Add(data, "Kael", SoulEggs.Stat.Armor);
            Assert.That(SoulEggs.Whole(SoulEggs.For(data, "Kael").Armor), Is.EqualTo(0));
            SoulEggs.Add(data, "Kael", SoulEggs.Stat.Armor);
            Assert.That(SoulEggs.Whole(SoulEggs.For(data, "Kael").Armor), Is.EqualTo(1));
            Assert.That(SoulEggs.For(data, "Kael").Eggs, Is.EqualTo(10));
        }

        [Test]
        public void RandomEggs_CoverEveryStat()
        {
            var data = new GameData();
            var rng = new System.Random(1);
            var seen = new HashSet<SoulEggs.Stat>();
            for (int i = 0; i < 2000; i++) seen.Add(SoulEggs.Add(data, "Kael", rng));
            Assert.That(seen.Count, Is.EqualTo(19));
            Assert.That(SoulEggs.For(data, "Kael").Eggs, Is.EqualTo(2000));
        }

        [Test]
        public void Apply_RaisesTheRunStats_WithVsCaps()
        {
            var player = new GameObject("EggPlayer");
            _cleanup.Add(player);
            var health = player.AddComponent<HealthController>();
            health.Initialize(100);
            var stats = player.AddComponent<PlayerStats>();
            float might = stats.Might, cooldown = stats.Cooldown, area = stats.Area;
            int armor = stats.Armor, amount = stats.Amount;

            var eggs = new SoulEggRecord
            {
                Character = "Kael", MaxHealth = 3f, Armor = 1.05f, Amount = 25f, Might = 12f, Area = 0.25f, Cooldown = 0.02f, Recovery = 0.3f
            };
            SoulEggs.Apply(eggs, stats);

            Assert.That(health.MaxHealth, Is.EqualTo(103));
            Assert.That(stats.Armor, Is.EqualTo(armor + 1));
            Assert.That(stats.Amount, Is.EqualTo(amount + 10), "VS caps egg Amount at +10.");
            Assert.That(stats.Might, Is.EqualTo(might + 9f).Within(1e-4f), "VS caps egg Might at +900%.");
            Assert.That(stats.Area, Is.EqualTo(area + 0.25f).Within(1e-4f));
            Assert.That(stats.Cooldown, Is.EqualTo(cooldown - 0.02f).Within(1e-4f));
            Assert.That(stats.Regen, Is.EqualTo(0.3f).Within(1e-4f));
        }

        [Test]
        public void Eggs_SurviveTheSaveFile_AndDamagedRecordsAreDropped()
        {
            var data = new GameData();
            SoulEggs.Add(data, "Kael", SoulEggs.Stat.Might);
            var loaded = JsonUtility.FromJson<GameData>(JsonUtility.ToJson(data));
            Assert.That(SoulEggs.For(loaded, "Kael").Might, Is.EqualTo(0.01f).Within(1e-6f));

            loaded.SoulEggs.Add(null);
            loaded.SoulEggs.Add(new SoulEggRecord { Character = "Aria", Might = float.NaN });
            loaded.SoulEggs.Add(new SoulEggRecord { Character = "" });
            SaveService.Migrate(loaded);
            Assert.That(loaded.SoulEggs.Count, Is.EqualTo(1));

            var old = JsonUtility.FromJson<GameData>("{\"SaveVersion\":1,\"Currency\":5}");
            old.SoulEggs = null;
            SaveService.Migrate(old);
            Assert.That(old.SoulEggs, Is.Not.Null.And.Empty, "A save from before Soul Eggs gets an empty list.");
        }
    }
}
