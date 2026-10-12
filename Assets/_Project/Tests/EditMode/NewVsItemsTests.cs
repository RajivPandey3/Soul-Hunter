using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Hazards;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using UnityEditor;
using UnityEngine;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// The VS items added on 2026-10-12 (NewVsItemsBuilder; raw wiki pages fetched 2026-10-12): Miser's Idol (Stone
    /// Mask), Ring of Long Hours and Ring of Dark Hours (Silver Ring, Gold Ring), Sundered Hour (Infinite Corridor),
    /// Dusk Raven (Ebony Wings), Twilight Flock (Vandalier), Azure Repeater (Eight The Sparrow) and Revenant Wheel
    /// (Phieraggi). The numbers here are the wiki's; the "Built" tests fail until Soul Hunter/Content/Build New VS
    /// Items has run.
    /// </summary>
    public class NewVsItemsTests
    {
        private GameObject _player;

        [TearDown]
        public void TearDown()
        {
            if (_player != null) Object.DestroyImmediate(_player);
        }

        private T Weapon<T>(int level = 1) where T : AutoAttackWeapon
        {
            if (_player == null) _player = new GameObject("Holder");
            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(_player.transform, false);
            var weapon = go.AddComponent<T>();
            weapon.CurrentLevel = level;
            return weapon;
        }

        private static UpgradeData Level(UpgradeType type, int level)
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeData>();
            upgrade.Type = type;
            upgrade.Level = level;
            upgrade.UpgradeName = $"{type} Lv.{level}";
            return upgrade;
        }

        // ---------------------------------------------------------------- passives

        [Test]
        public void RaritiesAndMaxLevels_AreTheWikiInfoboxValues()
        {
            Assert.That(UpgradeRarity.VsWeight(UpgradeType.MisersIdol), Is.EqualTo(100), "Stone Mask: rarity 100.");
            Assert.That(UpgradeRarity.VsWeight(UpgradeType.RingOfLongHours), Is.EqualTo(10), "Silver Ring: rarity 10.");
            Assert.That(UpgradeRarity.VsWeight(UpgradeType.RingOfDarkHours), Is.EqualTo(10), "Gold Ring: rarity 10.");
            Assert.That(UpgradeRarity.VsWeight(UpgradeType.DuskRaven), Is.EqualTo(50), "Ebony Wings: rarity 50.");
            Assert.That(UpgradeRarity.VsWeight(UpgradeType.AzureRepeater), Is.EqualTo(50), "Eight The Sparrow: rarity 50.");
            Assert.That(UpgradeRarity.VsPassiveMaxLevel(UpgradeType.RingOfLongHours), Is.EqualTo(9));
            Assert.That(UpgradeRarity.VsPassiveMaxLevel(UpgradeType.RingOfDarkHours), Is.EqualTo(9));
            foreach (var ring in new[] { UpgradeType.RingOfLongHours, UpgradeType.RingOfDarkHours })
            {
                Assert.That(UpgradeRarity.IsStageItemOnly(ring), Is.True, $"{ring}: never from a level-up.");
                Assert.That(WeaponManager.IsStagePassive(ring), Is.True);
                Assert.That(WeaponManager.LegacyIsWeaponType(ring), Is.False);
            }
            Assert.That(WeaponManager.LegacyIsWeaponType(UpgradeType.MisersIdol), Is.False, "A passive.");
            Assert.That(UpgradeRarity.IsStageItemOnly(UpgradeType.MisersIdol), Is.False, "Stone Mask is in the level-up pool.");
            foreach (var weapon in new[] { UpgradeType.DuskRaven, UpgradeType.AzureRepeater, UpgradeType.TwilightFlock })
                Assert.That(WeaponManager.LegacyIsWeaponType(weapon), Is.True, weapon.ToString());
            Assert.That(WeaponUnionData.IsUnionResult(UpgradeType.TwilightFlock), Is.True);
            Assert.That(WeaponUnionData.IsUnionResult(UpgradeType.DuskRaven), Is.False);
        }

        [Test]
        public void ThePassives_GiveTheirWikiStats_AndTheRingsNothingAtLevel1()
        {
            _player = new GameObject("Player");
            var stats = _player.AddComponent<PlayerStats>();
            _player.AddComponent<HealthController>();
            var manager = _player.AddComponent<WeaponManager>();
            manager.DefinitionLookup = _ => null; // the built-in rules, whether or not the assets are built yet
            float greed = stats.Greed, duration = stats.RawDuration, area = stats.RawArea, curse = stats.Curse;

            var made = new System.Collections.Generic.List<UpgradeData>();
            UpgradeData Make(UpgradeType type, int level) { var u = Level(type, level); made.Add(u); return u; }
            try
            {
                // Stone Mask: +10% Greed per level.
                manager.ApplyUpgrade(Make(UpgradeType.MisersIdol, 1));
                manager.ApplyUpgrade(Make(UpgradeType.MisersIdol, 2));
                Assert.That(stats.Greed, Is.EqualTo(greed + 0.2f).Within(1e-4f));

                // Silver Ring: nothing at level 1, then +5% Duration and Area per level.
                Assert.That(manager.AcquireStageItem(Make(UpgradeType.RingOfLongHours, 1)), Is.True, "A stage item is picked up at level 0.");
                Assert.That(stats.RawDuration, Is.EqualTo(duration).Within(1e-4f), "Level 1 gives nothing.");
                Assert.That(stats.RawArea, Is.EqualTo(area).Within(1e-4f));
                for (int level = 2; level <= 9; level++) manager.ApplyUpgrade(Make(UpgradeType.RingOfLongHours, level));
                Assert.That(stats.RawDuration, Is.EqualTo(duration + 0.4f).Within(1e-4f), "+40% at level 9.");
                Assert.That(stats.RawArea, Is.EqualTo(area + 0.4f).Within(1e-4f));
                Assert.That(manager.GetWeaponLevel(UpgradeType.RingOfLongHours), Is.EqualTo(9));

                // Gold Ring: nothing at level 1, then +5% Curse per level.
                Assert.That(manager.AcquireStageItem(Make(UpgradeType.RingOfDarkHours, 1)), Is.True);
                Assert.That(stats.Curse, Is.EqualTo(curse).Within(1e-4f), "Level 1 gives nothing.");
                for (int level = 2; level <= 9; level++) manager.ApplyUpgrade(Make(UpgradeType.RingOfDarkHours, level));
                Assert.That(stats.Curse, Is.EqualTo(curse + 0.4f).Within(1e-4f), "+40% at level 9.");

                // A ring is never taken from a level-up at level 0.
                var other = new GameObject("Other");
                other.transform.SetParent(_player.transform, false);
                var second = other.AddComponent<WeaponManager>();
                second.DefinitionLookup = _ => null;
                second.ApplyUpgrade(Make(UpgradeType.RingOfDarkHours, 1));
                Assert.That(second.GetWeaponLevel(UpgradeType.RingOfDarkHours), Is.EqualTo(0));
            }
            finally { foreach (var u in made) Object.DestroyImmediate(u); }
        }

        [Test]
        public void TheRings_LieNorthAndSouth_AndMisersIdolEastOnLevel2()
        {
            for (int stage = 1; stage <= 10; stage++)
            {
                var plan = StageItems.Plan(stage);
                var silver = plan.Single(e => e.Upgrade == UpgradeType.RingOfLongHours);
                var gold = plan.Single(e => e.Upgrade == UpgradeType.RingOfDarkHours);
                Assert.That(silver.Offset.z, Is.EqualTo(StageItems.RingDistance).Within(0.01f), "Silver Ring: north.");
                Assert.That(silver.Offset.x, Is.EqualTo(0f).Within(0.01f));
                Assert.That(gold.Offset.z, Is.EqualTo(-StageItems.RingDistance).Within(0.01f), "Gold Ring: south.");
                var idol = plan.Where(e => e.Upgrade == UpgradeType.MisersIdol).ToList();
                Assert.That(idol.Count, Is.EqualTo(stage == 2 ? 1 : 0), $"Stage {stage}");
                if (stage == 2) Assert.That(idol[0].Offset, Is.EqualTo(Vector3.right * 100f), "Inlaid Library: 5 tilesets east of the start.");
            }
        }

        // ---------------------------------------------------------------- the two birds and the two guns

        [Test]
        public void DuskRaven_HasPeachonesTable_AndCirclesTheOtherWay()
        {
            var orbit = typeof(PeachoneWeapon).GetProperty("OrbitDegreesPerSecond", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(orbit, Is.Not.Null);
            for (int level = 1; level <= 8; level++)
            {
                var peachone = Weapon<PeachoneWeapon>(level);
                var raven = Weapon<DuskRavenWeapon>(level);
                Assert.That(raven.BaseDamage, Is.EqualTo(peachone.BaseDamage), $"Level {level} damage");
                Assert.That(raven.BombCount, Is.EqualTo(peachone.BombCount), $"Level {level} amount");
                Assert.That(raven.LevelArea, Is.EqualTo(peachone.LevelArea), $"Level {level} area");
                Assert.That(raven.CooldownForLevel, Is.EqualTo(peachone.CooldownForLevel), $"Level {level} cooldown");
                Assert.That((float)orbit.GetValue(raven), Is.EqualTo(-(float)orbit.GetValue(peachone)), "Opposite directions, same pace.");
                Assert.That((float)orbit.GetValue(peachone), Is.LessThan(0f), "Peachone: clockwise (a falling angle, seen from the camera).");
            }
            var maxed = Weapon<DuskRavenWeapon>(8);
            Assert.That(maxed.MaxLevel, Is.EqualTo(8));
            Assert.That(maxed.BaseDamage, Is.EqualTo(30f), "max-damage 30 (+20)");
            Assert.That(maxed.BombCount, Is.EqualTo(11), "max-amount 11 (+7)");
            Assert.That(maxed.LevelArea, Is.EqualTo(2.2f).Within(1e-4f), "max-area 220%");
            Assert.That(maxed.CooldownForLevel, Is.EqualTo(0.5f).Within(1e-4f), "max-cooldown 0.5 s");
            Assert.That(maxed.Knockback, Is.EqualTo(2f));
        }

        [Test]
        public void AzureRepeater_HasTheGunsTable()
        {
            for (int level = 1; level <= 8; level++)
            {
                var gun = Weapon<GunWeapon>(level);
                var azure = Weapon<AzureRepeaterWeapon>(level);
                Assert.That(azure.BaseDamage, Is.EqualTo(gun.BaseDamage), $"Level {level} damage");
                Assert.That(azure.BulletsPerArm, Is.EqualTo(gun.BulletsPerArm), $"Level {level} amount");
                Assert.That(azure.Pierce, Is.EqualTo(gun.Pierce), $"Level {level} pierce");
                Assert.That(azure.LevelSpeedMultiplier, Is.EqualTo(gun.LevelSpeedMultiplier), $"Level {level} speed");
            }
            var maxed = Weapon<AzureRepeaterWeapon>(8);
            Assert.That(maxed.MaxLevel, Is.EqualTo(8));
            Assert.That(maxed.BaseDamage, Is.EqualTo(15f), "max-damage 15 (+10)");
            Assert.That(maxed.BulletsPerArm, Is.EqualTo(3), "max-amount 3 (+2)");
            Assert.That(maxed.Pierce, Is.EqualTo(7), "max-pierce 7 (+6)");
            Assert.That(maxed.LevelSpeedMultiplier, Is.EqualTo(1.5f), "max-speed 150%");
            Assert.That(maxed.Knockback, Is.EqualTo(0.1f));
        }

        // ---------------------------------------------------------------- the unions

        [Test]
        public void TwilightFlock_FollowsTheVandalierTable()
        {
            Assert.That(TwilightFlockWeapon.VsBaseDamage, Is.EqualTo(28f));
            Assert.That(TwilightFlockWeapon.VsAmount, Is.EqualTo(20));
            Assert.That(TwilightFlockWeapon.VsDuration, Is.EqualTo(4f));
            Assert.That(TwilightFlockWeapon.VsInterval, Is.EqualTo(0.025f));
            float[] area = { 2.2f, 2.4f, 2.4f, 2.6f, 2.6f, 2.8f, 2.8f, 3.0f };
            float[] cooldown = { 1f, 1f, 0.75f, 0.75f, 0.5f, 0.5f, 0.25f, 0.25f };
            for (int level = 1; level <= 8; level++)
            {
                var flock = Weapon<TwilightFlockWeapon>(level);
                Assert.That(flock.LevelArea, Is.EqualTo(area[level - 1]).Within(1e-4f), $"Level {level} area");
                Assert.That(flock.CooldownForLevel, Is.EqualTo(cooldown[level - 1]).Within(1e-4f), $"Level {level} cooldown");
                Assert.That(flock.BombCount, Is.EqualTo(20));
            }
            var one = Weapon<TwilightFlockWeapon>();
            Assert.That(one.MaxLevel, Is.EqualTo(8));
            Assert.That(one.Knockback, Is.EqualTo(2f));
            // Two zones on opposite sides of the holder, at the orbit's radius.
            Vector3 a = one.ZonePosition(0) - one.transform.position, b = one.ZonePosition(1) - one.transform.position;
            Assert.That(a.magnitude, Is.EqualTo(one.CircleRadius).Within(1e-3f));
            Assert.That(Vector3.Distance(a, -b), Is.LessThan(1e-3f));
        }

        [Test]
        public void RevenantWheel_FollowsThePhieraggiInfobox_AndScalesWithRevivals()
        {
            Assert.That(RevenantWheelWeapon.VsBaseDamage, Is.EqualTo(15f));
            Assert.That(RevenantWheelWeapon.VsAmount, Is.EqualTo(4));
            Assert.That(RevenantWheelWeapon.VsPierce, Is.EqualTo(7));
            Assert.That(RevenantWheelWeapon.VsCooldown, Is.EqualTo(1.4f));
            Assert.That(RevenantWheelWeapon.VsInterval, Is.EqualTo(0.1f));
            Assert.That(RevenantWheelWeapon.VsHitboxDelay, Is.EqualTo(0.2f));
            Assert.That(RevenantWheelWeapon.RevivalBonus(0), Is.EqualTo(0));
            Assert.That(RevenantWheelWeapon.RevivalBonus(3), Is.EqualTo(3));
            Assert.That(RevenantWheelWeapon.RevivalBonus(25), Is.EqualTo(10), "caps at +10 each");
            // Permanent uptime "starts at -42% Cooldown": the lasers last as long as that cooldown.
            Assert.That(RevenantWheelWeapon.UptimeSeconds, Is.EqualTo(RevenantWheelWeapon.VsCooldown * 0.58f).Within(0.02f));

            _player = new GameObject("Player");
            var stats = _player.AddComponent<PlayerStats>();
            var wheel = Weapon<RevenantWheelWeapon>();
            Assert.That(wheel.Knockback, Is.EqualTo(0.05f));
            Assert.That(wheel.LaserCount, Is.EqualTo(4));
            Assert.That(wheel.BaseDamage, Is.EqualTo(15f));
            Assert.That(wheel.Pierce, Is.EqualTo(7));
            stats.AddRevival(3);
            Assert.That(wheel.LaserCount, Is.EqualTo(7), "+1 Amount per Revival");
            Assert.That(wheel.BaseDamage, Is.EqualTo(18f), "+1 Base Damage per Revival");
            stats.AddRevival(20);
            Assert.That(wheel.LaserCount, Is.EqualTo(14), "max-amount 14");
            Assert.That(wheel.BaseDamage, Is.EqualTo(25f), "max-damage 25");
        }

        [Test]
        public void ARevenantLaser_HitsAtMostItsPierce_InOneHitboxDelayWindow()
        {
            _player = new GameObject("Laser");
            var laser = _player.AddComponent<RevenantWheelLaser>();
            laser.Pierce = 2;
            var targets = Enumerable.Range(0, 3).Select(i =>
            {
                var go = new GameObject("Target" + i);
                go.transform.SetParent(_player.transform, false);
                return go.transform;
            }).ToList();
            Assert.That(laser.Gate(targets[0]), Is.True);
            Assert.That(laser.Gate(targets[1]), Is.True);
            Assert.That(laser.Gate(targets[2]), Is.False, "The third enemy waits for the next window.");
            Assert.That(laser.WindowHits, Is.EqualTo(2));
        }

        [Test]
        public void SunderedHour_SendsItsPulseWithEveryThirteenthBeam_AndHalvesHealth()
        {
            Assert.That(SunderedHourWeapon.VsCooldown, Is.EqualTo(1f));
            Assert.That(SunderedHourWeapon.VsFreezeDuration, Is.EqualTo(6f));
            Assert.That(SunderedHourWeapon.BeamsPerRotation, Is.EqualTo(12));
            for (int beam = 0; beam < 12; beam++) Assert.That(SunderedHourWeapon.BeginsNewRotation(beam), Is.False, $"Beam {beam}: the first rotation.");
            Assert.That(SunderedHourWeapon.BeginsNewRotation(12), Is.True, "Back at 12 o'clock after a full rotation.");
            Assert.That(SunderedHourWeapon.BeginsNewRotation(13), Is.False);
            Assert.That(SunderedHourWeapon.BeginsNewRotation(24), Is.True);
            Assert.That(SunderedHourWeapon.HalvingDamage(1000), Is.EqualTo(500));
            Assert.That(SunderedHourWeapon.HalvingDamage(7), Is.EqualTo(3));
            Assert.That(SunderedHourWeapon.HalvingDamage(1), Is.EqualTo(0));

            var hour = Weapon<SunderedHourWeapon>();
            Assert.That(hour.Knockback, Is.EqualTo(0f));
            Assert.That(hour.LimitBreakBaseDuration, Is.EqualTo(6f));
            // The_Stalker: Infinite Corridor defeats it and "cause[s] a high-quality chest to drop".
            Assert.That(StageHazardTable.KillerOf(hour, null), Is.EqualTo(ShadowKiller.SunderedHour));
            Assert.That(StageHazardTable.ShadowChest(ShadowKiller.SunderedHour), Is.EqualTo((SoulHunter.Gameplay.Core.ChestTiers?)SoulHunter.Gameplay.Core.ChestTiers.StalkerHighQuality));
        }

        [Test]
        public void LimitBreak_OffersEachNewWeapon_ItsOwnWikiRows()
        {
            LimitBreak.Reset();
            var raven = Weapon<DuskRavenWeapon>(8);
            foreach (var stat in new[] { LimitBreak.Stat.BaseDamage, LimitBreak.Stat.Area, LimitBreak.Stat.Speed, LimitBreak.Stat.Duration, LimitBreak.Stat.Amount })
                Assert.That(LimitBreak.CanRaise(raven, stat), Is.True, $"Dusk Raven {stat}");
            Assert.That(LimitBreak.CanRaise(raven, LimitBreak.Stat.Pierce), Is.False);
            Assert.That(LimitBreak.StepFor(raven, LimitBreak.Stat.BaseDamage), Is.EqualTo(0.5f));

            var flock = Weapon<TwilightFlockWeapon>(8);
            Assert.That(LimitBreak.StepFor(flock, LimitBreak.Stat.BaseDamage), Is.EqualTo(1f), "Vandalier: Might +1%.");
            Assert.That(LimitBreak.CanRaise(flock, LimitBreak.Stat.Duration), Is.True, "4 s up to 9 s.");

            var azure = Weapon<AzureRepeaterWeapon>(8);
            Assert.That(LimitBreak.CanRaise(azure, LimitBreak.Stat.Pierce), Is.True);
            Assert.That(LimitBreak.CanRaise(azure, LimitBreak.Stat.Duration), Is.False);

            var wheel = Weapon<RevenantWheelWeapon>(8);
            Assert.That(LimitBreak.StepFor(wheel, LimitBreak.Stat.BaseDamage), Is.EqualTo(1f), "Phieraggi: Might +1%.");
            Assert.That(LimitBreak.CanRaise(wheel, LimitBreak.Stat.Pierce), Is.True);
            Assert.That(LimitBreak.CanRaise(wheel, LimitBreak.Stat.Amount), Is.True);

            var hour = Weapon<SunderedHourWeapon>(7);
            Assert.That(LimitBreak.CanRaise(hour, LimitBreak.Stat.Duration), Is.True, "Only Duration, up to 10 s.");
            foreach (var stat in new[] { LimitBreak.Stat.BaseDamage, LimitBreak.Stat.Area, LimitBreak.Stat.Speed, LimitBreak.Stat.Amount, LimitBreak.Stat.Pierce })
                Assert.That(LimitBreak.CanRaise(hour, stat), Is.False, $"Sundered Hour {stat}");
        }

        [Test]
        public void AUnion_CanNeedAPassive_AndItsRecipeListsIt()
        {
            _player = new GameObject("Player");
            var manager = _player.AddComponent<WeaponManager>();
            manager.DefinitionLookup = _ => null;
            var union = ScriptableObject.CreateInstance<WeaponUnionData>();
            try
            {
                union.name = "Union_Test";
                union.WeaponA = UpgradeType.Gun;
                union.WeaponB = UpgradeType.AzureRepeater;
                Assert.That(union.PassiveMet(manager), Is.True, "No passive needed.");
                union.NeedsPassive = true;
                union.RequiredPassive = UpgradeType.Tiragisu;
                Assert.That(union.PassiveMet(manager), Is.False);
                manager.GiveWeapon(UpgradeType.Tiragisu, 1);
                Assert.That(union.PassiveMet(manager), Is.True);
                var recipe = EvolutionRecipe.From(union);
                Assert.That(recipe.IsUnion, Is.True);
                Assert.That(recipe.Items, Is.EqualTo(new[] { UpgradeType.Gun, UpgradeType.AzureRepeater, UpgradeType.Tiragisu }));
            }
            finally { Object.DestroyImmediate(union); }
        }

        // ---------------------------------------------------------------- the built assets

        [Test]
        public void Built_EveryNewItem_HasItsDefinitionLevelsAndName()
        {
            var catalog = GameContentCatalog.Load();
            Assert.IsNotNull(catalog);
            var expected = new (UpgradeType Type, string Name, int Levels, bool Passive)[]
            {
                (UpgradeType.MisersIdol, "Miser's Idol", 5, true),
                (UpgradeType.RingOfLongHours, "Ring of Long Hours", 9, true),
                (UpgradeType.RingOfDarkHours, "Ring of Dark Hours", 9, true),
                (UpgradeType.DuskRaven, "Dusk Raven", 8, false),
                (UpgradeType.AzureRepeater, "Azure Repeater", 8, false),
                (UpgradeType.TwilightFlock, "Twilight Flock", 8, false),
            };
            foreach (var item in expected)
            {
                var definition = catalog.WeaponDefinitions.FirstOrDefault(d => d != null && d.Type == item.Type);
                Assert.That(definition, Is.Not.Null, $"{item.Type}: run Soul Hunter/Content/Build New VS Items.");
                Assert.That(definition.DisplayName, Is.EqualTo(item.Name));
                Assert.That(definition.IsPassive, Is.EqualTo(item.Passive));
                Assert.That(definition.MaxLevel, Is.EqualTo(item.Levels));
                Assert.That(definition.Levels.Select(u => u.Level), Is.EqualTo(Enumerable.Range(1, item.Levels)), $"{item.Type} levels");
                Assert.That(definition.Icon, Is.Not.Null, $"{item.Type} has a (placeholder) icon.");
                Assert.That(definition.Rarity, Is.EqualTo(UpgradeRarity.VsWeight(item.Type)));
                foreach (var level in definition.Levels)
                {
                    Assert.That(catalog.Upgrades, Does.Contain(level), $"{level.name} is in GameContent.Upgrades.");
                    Assert.That(UpgradeText.DisplayName(level), Is.EqualTo(item.Name));
                }
                if (!item.Passive)
                {
                    Assert.That(definition.WeaponPrefab, Is.Not.Null);
                    Assert.That(definition.WeaponPrefab.GetComponent<AutoAttackWeapon>().MaxLevel, Is.EqualTo(item.Levels));
                }
            }
            Assert.That(catalog.WeaponDefinitions.First(d => d.Type == UpgradeType.DuskRaven).WeaponPrefab.GetComponent<DuskRavenWeapon>().BombPrefab, Is.Not.Null);
            Assert.That(catalog.WeaponDefinitions.First(d => d.Type == UpgradeType.AzureRepeater).WeaponPrefab.GetComponent<AzureRepeaterWeapon>().BulletPrefab, Is.Not.Null);
            Assert.That(catalog.WeaponDefinitions.First(d => d.Type == UpgradeType.TwilightFlock).WeaponPrefab.GetComponent<TwilightFlockWeapon>().BombPrefab, Is.Not.Null);
        }

        [Test]
        public void Built_SunderedHour_NeedsTheMaxedLancetAndBothRingsAtMaxLevel()
        {
            var evolution = AssetDatabase.LoadAssetAtPath<WeaponEvolutionData>("Assets/_Project/Data/Weapons/Evo_SunderedHour.asset");
            Assert.That(evolution, Is.Not.Null, "Run Soul Hunter/Content/Build New VS Items.");
            Assert.That(evolution.BaseWeapon, Is.EqualTo(UpgradeType.ClockLancet));
            Assert.That(evolution.BaseWeaponMaxLevel, Is.EqualTo(7), "The Clock Lancet has 7 levels.");
            Assert.That(evolution.RequiredPassive, Is.EqualTo(UpgradeType.RingOfLongHours));
            Assert.That(evolution.RequiredPassiveMaxLevel, Is.EqualTo(9), "Silver Ring at max level.");
            Assert.That(evolution.AdditionalPassives.Select(p => (p.Type, p.MinimumLevel)), Is.EqualTo(new[] { (UpgradeType.RingOfDarkHours, 9) }), "Gold Ring at max level.");
            Assert.That(evolution.EvolvedWeaponPrefab.GetComponent<SunderedHourWeapon>(), Is.Not.Null);
            Assert.That(evolution.EvolvedWeaponPrefab.GetComponent<SunderedHourWeapon>().FreezeBeamPrefab, Is.Not.Null);
            Assert.That(WeaponEvolutionData.DisplayName(evolution.EvolvedName), Is.EqualTo("Sundered Hour"));
            var catalog = GameContentCatalog.Load();
            Assert.That(catalog.Evolutions, Does.Contain(evolution));
            Assert.That(catalog.CollectionEvolutions, Does.Contain(evolution));
        }

        [Test]
        public void Built_TheTwoUnions_AreTheVsRecipes()
        {
            var catalog = GameContentCatalog.Load();
            Assert.That(catalog.Unions, Is.Not.Null.And.Length.EqualTo(2), "Run Soul Hunter/Content/Build New VS Items.");
            var flock = catalog.Unions.Single(u => u.name == "Union_TwilightFlock");
            Assert.That(new[] { flock.WeaponA, flock.WeaponB }, Is.EquivalentTo(new[] { UpgradeType.Peachone, UpgradeType.DuskRaven }));
            Assert.That(flock.WeaponAMaxLevel, Is.EqualTo(8));
            Assert.That(flock.WeaponBMaxLevel, Is.EqualTo(8));
            Assert.That(flock.NeedsPassive, Is.False, "Vandalier needs no passive.");
            Assert.That(flock.HasResultType && flock.ResultType == UpgradeType.TwilightFlock, Is.True, "It has levels of its own.");
            Assert.That(flock.UnionWeaponPrefab.GetComponent<TwilightFlockWeapon>(), Is.Not.Null);
            Assert.That(WeaponEvolutionData.DisplayName(flock.UnionName), Is.EqualTo("Twilight Flock"));

            var wheel = catalog.Unions.Single(u => u.name == "Union_RevenantWheel");
            Assert.That(new[] { wheel.WeaponA, wheel.WeaponB }, Is.EquivalentTo(new[] { UpgradeType.Gun, UpgradeType.AzureRepeater }));
            Assert.That(wheel.WeaponAMaxLevel, Is.EqualTo(8));
            Assert.That(wheel.WeaponBMaxLevel, Is.EqualTo(8));
            Assert.That(wheel.NeedsPassive && wheel.RequiredPassive == UpgradeType.Tiragisu, Is.True, "Phieraggi requires Tirajisu.");
            Assert.That(wheel.HasResultType, Is.False);
            Assert.That(wheel.UnionWeaponPrefab.GetComponent<RevenantWheelWeapon>(), Is.Not.Null);
            Assert.That(WeaponEvolutionData.DisplayName(wheel.UnionName), Is.EqualTo("Revenant Wheel"));

            // The Collection lists Revenant Wheel as a union entry; Twilight Flock is a weapon entry of its own.
            var entries = CollectionCatalog.Build(catalog);
            Assert.That(entries.Count(e => e.Name == "Revenant Wheel"), Is.EqualTo(1));
            Assert.That(entries.Count(e => e.Name == "Twilight Flock"), Is.EqualTo(1));
            Assert.That(entries.Any(e => e.Name == "Sundered Hour" && e.Kind == CollectionKind.Evolution), Is.True);
        }
    }
}
