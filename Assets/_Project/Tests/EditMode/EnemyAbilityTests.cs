using NUnit.Framework;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// The data-driven enemy/boss ability system and VS resistances (vampire.survivors.wiki/w/Enemies "Resistances";
    /// ability pages cited in EnemyAbilityDefaults): spec defaults, interval rules, resistance decisions, and the boss
    /// assets' VS resistances. Existing enemies must be unchanged (no abilities, no resistances).
    /// </summary>
    public class EnemyAbilityTests
    {
        private const string EnemyFolder = "Assets/_Project/Data/Config/Enemies";

        [Test]
        public void NewSpecAndNewEnemyData_ChangeNothing()
        {
            var spec = new EnemyAbilitySpec();
            Assert.That(spec.IntervalSeconds, Is.EqualTo(0f));
            Assert.That(spec.Count, Is.EqualTo(0));
            Assert.That(spec.Damage, Is.EqualTo(0));
            Assert.That(spec.Prefab, Is.Null);
            Assert.That(spec.ShrinkIntervalWithHealth, Is.False);
            Assert.That(spec.DiscardAbsorbedDamage, Is.False);

            var data = ScriptableObject.CreateInstance<EnemyData>();
            try
            {
                Assert.That(data.Abilities, Is.Not.Null.And.Empty, "A new enemy has no abilities.");
                Assert.That(data.FreezeResistance, Is.EqualTo(0f));
                Assert.That(data.InstantKillResistance, Is.False);
                Assert.That(data.DebuffResistance, Is.False);
                Assert.That(data.DefangResistance, Is.False, "P2-02: res-defang off by default.");
                Assert.That(data.Lives, Is.EqualTo(1), "P2-02: one life by default.");
                Assert.That(data.HealthPerPlayerLevelFraction, Is.EqualTo(0f));
            }
            finally { Object.DestroyImmediate(data); }
        }

        [Test]
        public void KindDefaults_FollowTheVsWiki()
        {
            var summon = new EnemyAbilitySpec(EnemyAbilityKind.Summon).Resolved();
            Assert.That(summon.Count, Is.EqualTo(48), "Trickster: rings of 48.");
            Assert.That(summon.SpawnBursts, Is.EqualTo(2), "Trickster: two rings on spawn.");
            Assert.That(summon.IntervalSeconds, Is.EqualTo(30f), "Trickster: one ring every 30 s.");
            Assert.That(summon.MaxAlive, Is.GreaterThan(0));

            var shoot = new EnemyAbilitySpec(EnemyAbilityKind.Shoot).Resolved();
            Assert.That(shoot.IntervalSeconds, Is.EqualTo(1f), "Twin Skulls: once every second.");
            Assert.That(shoot.Damage, Is.EqualTo(25), "Twin Skulls bullet: 25 damage.");
            Assert.That(shoot.Speed, Is.EqualTo(250f * EnemyAbilityDefaults.VsSpeedToUnits).Within(1e-4), "Bullet movespeed 250.");
            Assert.That(shoot.MinIntervalSeconds, Is.EqualTo(0.5f), "The Ender: down to 0.5 s.");

            var lunge = new EnemyAbilitySpec(EnemyAbilityKind.Lunge).Resolved();
            Assert.That(lunge.IntervalSeconds, Is.EqualTo(5f));
            Assert.That(lunge.MaxIntervalSeconds, Is.EqualTo(10f), "Orochimario: every 5 to 10 s.");

            Assert.That(new EnemyAbilitySpec(EnemyAbilityKind.InvulnerablePhase).Resolved().DurationSeconds, Is.EqualTo(90f), "The Ender: 90 s shield.");

            var boom = new EnemyAbilitySpec(EnemyAbilityKind.SelfDestruct).Resolved();
            Assert.That(boom.FuseSeconds, Is.EqualTo(2f), "Sig.ra Rossi: 2 s.");
            Assert.That(boom.Damage, Is.EqualTo(50), "Sig.ra Rossi: 50 damage.");

            var zone = new EnemyAbilitySpec(EnemyAbilityKind.HazardZone).Resolved();
            Assert.That(zone.Damage, Is.EqualTo(12), "The Ender's zones: 12 damage.");
            Assert.That(zone.DurationSeconds, Is.EqualTo(10f), "The Ender's zones: 10 s.");

            var pulse = new EnemyAbilitySpec(EnemyAbilityKind.HalvingPulse).Resolved();
            Assert.That(pulse.IntervalSeconds, Is.GreaterThan(0f));
            Assert.That(pulse.Radius, Is.GreaterThan(0f));
        }

        [Test]
        public void SetNumbers_OverrideTheDefaults()
        {
            var spec = new EnemyAbilitySpec(EnemyAbilityKind.Shoot) { IntervalSeconds = 2f, Damage = 20, Speed = 4.2f };
            var r = spec.Resolved();
            Assert.That(r.IntervalSeconds, Is.EqualTo(2f));
            Assert.That(r.Damage, Is.EqualTo(20));
            Assert.That(r.Speed, Is.EqualTo(4.2f));
            Assert.That(spec.IntervalSeconds, Is.EqualTo(2f), "Resolving does not change the authored spec.");
            Assert.That(new EnemyAbilitySpec(EnemyAbilityKind.Summon) { SpawnBursts = -1 }.Resolved().SpawnBursts, Is.EqualTo(0), "Below 0 = none on spawn.");
        }

        [Test]
        public void Intervals_ShrinkWithHealth_OrPickBetweenMinAndMax()
        {
            var ender = new EnemyAbilitySpec(EnemyAbilityKind.Shoot) { IntervalSeconds = 5f, ShrinkIntervalWithHealth = true }.Resolved();
            Assert.That(EnemyAbilitySpec.NextInterval(ender, 1f, 0f), Is.EqualTo(5f).Within(1e-4), "Full health: 5 s.");
            Assert.That(EnemyAbilitySpec.NextInterval(ender, 0.5f, 0f), Is.EqualTo(2.75f).Within(1e-4), "Linear with health.");
            Assert.That(EnemyAbilitySpec.NextInterval(ender, 0f, 0f), Is.EqualTo(0.5f).Within(1e-4), "The Ender: minimum 0.5 s.");

            var lunge = new EnemyAbilitySpec(EnemyAbilityKind.Lunge).Resolved();
            Assert.That(EnemyAbilitySpec.NextInterval(lunge, 1f, 0f), Is.EqualTo(5f).Within(1e-4));
            Assert.That(EnemyAbilitySpec.NextInterval(lunge, 1f, 1f), Is.EqualTo(10f).Within(1e-4));
            Assert.That(EnemyAbilitySpec.NextInterval(lunge, 1f, 0.5f), Is.EqualTo(7.5f).Within(1e-4));
        }

        [Test]
        public void Resistances_PentagramClockLancetOrologionGarlic()
        {
            // Instant kill: Pentagram / Soul Toll skip stage bosses and resistant enemies.
            Assert.That(EnemyResistance.ResistsInstantKill(false, false), Is.False);
            Assert.That(EnemyResistance.ResistsInstantKill(false, true), Is.True);
            Assert.That(EnemyResistance.ResistsInstantKill(true, false), Is.True, "Stage bosses stay immune.");

            // Weapon freeze: fails only when resistance is HIGHER than the weapon's freeze chance.
            Assert.That(ClockLancetWeapon.VsFreezeChance, Is.EqualTo(1f));
            Assert.That(EnemyResistance.WeaponFreezes(0f, ClockLancetWeapon.VsFreezeChance), Is.True);
            Assert.That(EnemyResistance.WeaponFreezes(1f, ClockLancetWeapon.VsFreezeChance), Is.True, "Equal is not higher.");
            Assert.That(EnemyResistance.WeaponFreezes(1.1f, ClockLancetWeapon.VsFreezeChance), Is.False, "VS bosses (1.1) resist Clock Lancet.");
            Assert.That(EnemyResistance.OrologionFreezes(100f), Is.True, "Orologion goes through freeze resistance.");

            // Garlic: debuffs blocked by debuff resistance; -0.1 freeze resistance down to 0.
            Assert.That(EnemyResistance.GarlicDebuffApplies(false), Is.True);
            Assert.That(EnemyResistance.GarlicDebuffApplies(true), Is.False);
            Assert.That(EnemyResistance.AfterGarlicHit(1.1f), Is.EqualTo(1f).Within(1e-5));
            Assert.That(EnemyResistance.AfterGarlicHit(0.05f), Is.EqualTo(0f));
            Assert.That(EnemyResistance.WeaponFreezes(EnemyResistance.AfterGarlicHit(1.1f), ClockLancetWeapon.VsFreezeChance), Is.True,
                "One Garlic hit lets Clock Lancet freeze a boss without debuff resistance (VS Garlic tips).");
        }

        [Test]
        public void HalvingPulse_TakesHalfTheCurrentHealth()
        {
            Assert.That(EnemyAbilityZone.HalvingDamage(200), Is.EqualTo(100));
            Assert.That(EnemyAbilityZone.HalvingDamage(51), Is.EqualTo(25));
            Assert.That(EnemyAbilityZone.HalvingDamage(1), Is.EqualTo(0), "Never kills.");
            Assert.That(EnemyAbilityZone.HalvingDamage(0), Is.EqualTo(0));
        }

        [Test]
        public void StageBosses_HaveTheVsBossResistances_AndNoAbilitiesYet()
        {
            int bosses = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:EnemyData", new[] { EnemyFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                string file = System.IO.Path.GetFileName(path);
                // Levels 2-10's VS wave types carry their statbox resistances and shooting (StageEnemyTypeTests).
                if (StageEnemyTables.IsTypeAsset(path)) continue;
                Assert.That(data.Abilities, Is.Not.Null.And.Empty, file + ": no abilities until the owner picks them.");
                if (!file.Contains("_Boss_"))
                {
                    Assert.That(data.FreezeResistance, Is.EqualTo(0f), file);
                    Assert.That(data.InstantKillResistance, Is.False, file);
                    Assert.That(data.DebuffResistance, Is.False, file);
                    continue;
                }
                bosses++;
                Assert.That(data.FreezeResistance, Is.EqualTo(1.1f).Within(1e-5), file + ": VS boss res-freeze 1.1.");
                // Mad Forest's 25:00 boss (Giant Blue Venus, vampire.survivors.wiki/w/Venus) resists only freeze.
                bool stageOne = file.StartsWith("L01");
                Assert.That(data.InstantKillResistance, Is.EqualTo(!stageOne), file);
                Assert.That(data.DebuffResistance, Is.EqualTo(!stageOne), file);
            }
            Assert.That(bosses, Is.EqualTo(10), "Ten stage bosses.");
        }
    }
}
