using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Weapons;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Release item P2-01 (2026-10-09): the 22 base Arcanas with Soul Hunter's own names (owner: never the VS names; the
    /// first four keep theirs), the assets in GameContent, and every card's numbers from its own wiki page (raw, fetched
    /// 2026-10-09; quoted in ArcanaEffects.cs).
    /// </summary>
    public class ArcanaCatalogTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private readonly List<Object> _created = new List<Object>();
        private readonly List<ArcanaManager> _managers = new List<ArcanaManager>();

        private T Track<T>(T obj) where T : Object { _created.Add(obj); return obj; }

        [TearDown]
        public void TearDown()
        {
            foreach (var manager in _managers) if (manager != null) typeof(ArcanaManager).GetMethod("OnDestroy", Private).Invoke(manager, null);
            _managers.Clear();
            foreach (var obj in _created) if (obj != null) Object.DestroyImmediate(obj);
            _created.Clear();
            AutoAttackWeapon.SlashArcanaActive = false;
        }

        private ArcanaManager NewManager(params ArcanaData.ArcanaType[] held)
        {
            var manager = Track(new GameObject("Arcana")).AddComponent<ArcanaManager>();
            typeof(ArcanaManager).GetMethod("Awake", Private).Invoke(manager, null);
            _managers.Add(manager);
            foreach (var type in held) manager.ApplyArcana(Card(type));
            return manager;
        }

        private ArcanaData Card(ArcanaData.ArcanaType type)
        {
            var card = Track(ScriptableObject.CreateInstance<ArcanaData>());
            card.Type = type;
            card.CardName = ArcanaCatalog.Of(type).Name;
            card.CardNumber = ArcanaCatalog.Of(type).Numeral;
            return card;
        }

        private static readonly string[] Numerals =
            { "0", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII", "XIII", "XIV", "XV", "XVI", "XVII", "XVIII", "XIX", "XX", "XXI" };

        [Test]
        public void TheCatalog_Has22Cards_OneForEachNumeral_WithSoulHunterNames()
        {
            Assert.That(ArcanaCatalog.All.Length, Is.EqualTo(22));
            Assert.That(ArcanaCatalog.All.Select(c => c.Numeral), Is.EqualTo(Numerals), "The cards in numeral order, each numeral once.");
            Assert.That(ArcanaCatalog.All.Select(c => c.Type).Distinct().Count(), Is.EqualTo(22));
            Assert.That(ArcanaCatalog.All.Select(c => c.Name).Distinct().Count(), Is.EqualTo(22), "Every name is its own.");
            // Owner 2026-10-09 kept four VS names; 2026-10-30 (release audit, "Sirf VS naam badlo") renamed Iron Blue Will and Waltz of Pearls. Awake and Gemini are common words and stay.
            var kept = new[] { ArcanaData.ArcanaType.Awake, ArcanaData.ArcanaType.Gemini };
            foreach (var card in ArcanaCatalog.All)
            {
                if (kept.Contains(card.Type)) Assert.That(card.Name, Is.EqualTo(card.VsName), "Awake and Gemini keep their names.");
                else
                {
                    Assert.That(card.Name, Is.Not.EqualTo(card.VsName).IgnoreCase, $"{card.Numeral} must not use the VS name.");
                    foreach (var word in card.VsName.Split(' ', '&').Where(w => w.Length > 4))
                        Assert.That(card.Name, Does.Not.Contain(word).IgnoreCase, $"{card.Name} reuses '{word}' from the VS name.");
                }
                Assert.That(card.Description, Is.Not.Empty);
                Assert.That(card.Description, Does.Not.Contain(card.VsName), "The text never names the VS card.");
            }
            Assert.That(ArcanaCatalog.Of(ArcanaData.ArcanaType.IronBlueWill).Numeral, Is.EqualTo("VII"));
        }

        [Test]
        public void GameContent_ListsAll22Arcanas_WithTheCatalogsNameNumeralAndText()
        {
            var content = GameContentCatalog.Load();
            Assert.IsNotNull(content);
            Assert.That(content.Arcanas, Has.Length.EqualTo(22));
            Assert.That(content.Arcanas.Select(a => a.CardNumber), Is.EqualTo(Numerals), "Collection order is the numeral order.");
            foreach (var arcana in content.Arcanas)
            {
                var card = ArcanaCatalog.Of(arcana.Type);
                Assert.That(arcana.CardNumber, Is.EqualTo(card.Numeral), arcana.name);
                Assert.That(arcana.CardName, Is.EqualTo(card.Name), arcana.name);
                Assert.That(arcana.Description, Is.EqualTo(card.Description), arcana.name);
            }
            // The Collection lists every Arcana (CollectionCatalog builds from GameContent).
            var collection = CollectionCatalog.Build(content).Where(e => e.Kind == CollectionKind.Arcana).ToList();
            Assert.That(collection.Count, Is.EqualTo(22));
        }

        [Test]
        public void TheListedWeapons_AreSoulHunterWeapons_IdentifiedFromTheirScripts()
        {
            T Weapon<T>(string name = null) where T : AutoAttackWeapon
            {
                var go = Track(new GameObject(name ?? typeof(T).Name));
                return go.AddComponent<T>();
            }
            Assert.That(ArcanaManager.Identify(Weapon<KnifeWeapon>()), Is.EqualTo(ArcanaWeapon.Knife));
            Assert.That(ArcanaManager.Identify(Weapon<CrossWeapon>("HeavenSword(Clone)")), Is.EqualTo(ArcanaWeapon.HeavenSword));
            Assert.That(ArcanaManager.Identify(Weapon<CrossWeapon>("Cross_Weapon(Clone)")), Is.EqualTo(ArcanaWeapon.Cross));
            Assert.That(ArcanaManager.Identify(Weapon<BibleWeapon>("UnholyVespers(Clone)")), Is.EqualTo(ArcanaWeapon.UnholyVespers));
            Assert.That(ArcanaManager.Identify(Weapon<GarlicWeapon>("SoulEater(Clone)")), Is.EqualTo(ArcanaWeapon.SoulEater));
            Assert.That(ArcanaManager.Identify(Weapon<GarlicWeapon>("Garlic_Weapon")), Is.EqualTo(ArcanaWeapon.Garlic));
            Assert.That(ArcanaManager.Identify(Weapon<CrimsonShroudWeapon>()), Is.EqualTo(ArcanaWeapon.CrimsonShroud));
            Assert.That(ArcanaManager.Identify(Weapon<StormboundHaloWeapon>()), Is.EqualTo(ArcanaWeapon.StormboundHalo));
            Assert.That(ArcanaManager.BaseTypeOf(ArcanaWeapon.BloodyTear), Is.EqualTo(UpgradeData.UpgradeType.Whip));
            Assert.That(ArcanaManager.BaseTypeOf(ArcanaWeapon.DrowningChalice), Is.EqualTo(UpgradeData.UpgradeType.SantaWater));
            // Each page's list, limited to the weapons Soul Hunter has.
            Assert.That(ArcanaCatalog.Of(ArcanaData.ArcanaType.TwilightRequiem).Weapons, Is.EquivalentTo(new[] {
                ArcanaWeapon.Bible, ArcanaWeapon.UnholyVespers, ArcanaWeapon.LightningRing, ArcanaWeapon.StormboundHalo,
                ArcanaWeapon.Peachone, ArcanaWeapon.Runetracer, ArcanaWeapon.Bone }));
            Assert.That(ArcanaCatalog.Of(ArcanaData.ArcanaType.TragicPrincess).Weapons, Is.EquivalentTo(new[] {
                ArcanaWeapon.Garlic, ArcanaWeapon.SoulEater, ArcanaWeapon.SantaWater, ArcanaWeapon.DrowningChalice,
                ArcanaWeapon.LightningRing, ArcanaWeapon.StormboundHalo }));
            Assert.That(ArcanaCatalog.Of(ArcanaData.ArcanaType.DivineBloodline).Weapons, Is.EquivalentTo(new[] {
                ArcanaWeapon.Cross, ArcanaWeapon.Bible, ArcanaWeapon.Garlic, ArcanaWeapon.SantaWater, ArcanaWeapon.LightningRing, ArcanaWeapon.SongOfMana }));
            Assert.That(ArcanaCatalog.Of(ArcanaData.ArcanaType.JailOfCrystal).Weapons, Is.EquivalentTo(new[] {
                ArcanaWeapon.MagicWand, ArcanaWeapon.HolyWand, ArcanaWeapon.Runetracer, ArcanaWeapon.SeveredFate,
                ArcanaWeapon.AzureRepeater })); // VS Eight The Sparrow (2026-10-12)
            Assert.That(ArcanaManager.Identify(Weapon<AzureRepeaterWeapon>()), Is.EqualTo(ArcanaWeapon.AzureRepeater), "Not the Gun: no Heart of Fire.");
            Assert.That(ArcanaManager.Identify(Weapon<DuskRavenWeapon>()), Is.EqualTo(ArcanaWeapon.Peachone), "VS Ebony Wings has Peachone's cards.");
            Assert.That(ArcanaCatalog.Of(ArcanaData.ArcanaType.HeartOfFire).Weapons, Is.EquivalentTo(new[] {
                ArcanaWeapon.FireWand, ArcanaWeapon.HellmawComet, ArcanaWeapon.Gun }));
            Assert.That(ArcanaCatalog.Of(ArcanaData.ArcanaType.Beginning).Weapons, Is.EquivalentTo(new[] { ArcanaWeapon.Bone, ArcanaWeapon.CherryBomb }));
        }

        [Test]
        public void EveryCardsNumbers_FollowItsWikiPage()
        {
            // Twilight Requiem (II): "around 13.5 damage" per +100% Curse "starting at 10 damage for +0% Curse".
            Assert.That(ArcanaManager.TwilightDamage(1f), Is.EqualTo(10f).Within(0.001f));
            Assert.That(ArcanaManager.TwilightDamage(3f), Is.EqualTo(37f).Within(0.001f));
            // Tragic Princess (III): faster with speed (balance: +50% at base Move Speed).
            Assert.That(ArcanaManager.TragicPrincessRate(5f), Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(ArcanaManager.TragicPrincessRate(10f), Is.GreaterThan(ArcanaManager.TragicPrincessRate(5f)));
            // V / XVII: x0.5..x1.5 over 10 s; XVIII: x0.75..x1.25; +1% per level.
            Assert.That(ArcanaManager.Sway(ArcanaManager.ChaosSway, 5f), Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(ArcanaManager.Sway(ArcanaManager.PaintingSway, 15f), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(ArcanaManager.Sway(ArcanaManager.BoogalooSway, 5f), Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(ArcanaManager.SwayPerLevel, Is.EqualTo(0.01f));
            // Sarabande (VI): healing x2, pulses every 0.5 s.
            Assert.That(ArcanaManager.SarabandeHealFactor, Is.EqualTo(2f));
            Assert.That(ArcanaManager.SarabandeCooldown, Is.EqualTo(0.5f));
            // Mad Groove (VIII): every 2 minutes.
            Assert.That(ArcanaManager.MadGrooveIntervalSeconds, Is.EqualTo(120f));
            // Divine Bloodline (IX): +5 per Armor up to +250 for listed weapons; +0.1 per missing HP for all.
            Assert.That(ArcanaManager.DivineFlatDamage(true, 60, 0), Is.EqualTo(250f).Within(0.001f));
            Assert.That(ArcanaManager.DivineFlatDamage(true, 2, 10), Is.EqualTo(11f).Within(0.001f));
            Assert.That(ArcanaManager.DivineFlatDamage(false, 10, 10), Is.EqualTo(1f).Within(0.001f));
            Assert.That(ArcanaManager.DivineTouchDamagePerArmor, Is.EqualTo(10f));
            Assert.That(ArcanaManager.DivineMaxHealthPerKill, Is.EqualTo(0.5f));
            // Beginning (X): +3 main weapon, +1 listed.
            Assert.That(ArcanaManager.BeginningMainAmount, Is.EqualTo(3));
            Assert.That(ArcanaManager.BeginningListedAmount, Is.EqualTo(1));
            // Out of Bounds (XII): 10 + seconds / 30.
            Assert.That(ArcanaManager.OutOfBoundsDamage(60f), Is.EqualTo(12f).Within(0.001f));
            Assert.That(ArcanaManager.OutOfBoundsDamage(1800f), Is.EqualTo(70f).Within(0.001f));
            // Wicked Season (XIII): Growth, Luck, Greed, Curse, 10 s each.
            Assert.That(ArcanaManager.WickedSeasonPhase(0f), Is.EqualTo(0));
            Assert.That(ArcanaManager.WickedSeasonPhase(10f), Is.EqualTo(1));
            Assert.That(ArcanaManager.WickedSeasonPhase(35f), Is.EqualTo(3));
            Assert.That(ArcanaManager.WickedSeasonPhase(40f), Is.EqualTo(0));
            // Jail of Crystal (XIV): 25% x Luck.
            Assert.That(ArcanaManager.JailFreezes(1f, 0.24f), Is.True);
            Assert.That(ArcanaManager.JailFreezes(1f, 0.26f), Is.False);
            Assert.That(ArcanaManager.JailFreezes(2f, 0.49f), Is.True);
            // Heart of Fire (XIX): 10 base, +5 per listed weapon from the second; +10% per Armor.
            Assert.That(ArcanaManager.HeartBlastBase(0), Is.EqualTo(10f));
            Assert.That(ArcanaManager.HeartBlastBase(1), Is.EqualTo(10f));
            Assert.That(ArcanaManager.HeartBlastBase(3), Is.EqualTo(20f));
            Assert.That(ArcanaManager.RetaliationFactor(5), Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(ArcanaManager.HeartImpactShare, Is.EqualTo(0.5f));
            // Silent Old Sanctuary (XX): +20% Might / -8% Cooldown per empty slot, at most 5.
            Assert.That(ArcanaManager.SanctuaryEmptySlots(6, 1), Is.EqualTo(5));
            Assert.That(ArcanaManager.SanctuaryEmptySlots(6, 0), Is.EqualTo(5));
            Assert.That(ArcanaManager.SanctuaryEmptySlots(6, 6), Is.EqualTo(0));
            // Blood Astronomia (XXI): 5 x Amount every 2 s; "base Amount * 3 + player Amount" zones.
            Assert.That(ArcanaManager.AstronomiaBaseZoneDamage(0), Is.EqualTo(5f));
            Assert.That(ArcanaManager.AstronomiaBaseZoneDamage(2), Is.EqualTo(15f));
            Assert.That(ArcanaManager.AstronomiaZoneCount(2), Is.EqualTo(5));
            Assert.That(ArcanaManager.AstronomiaCooldown, Is.EqualTo(2f));
        }

        [Test]
        public void GameKiller_ChestsHoldAtLeastThree_AndArcanaChestsTwoMore()
        {
            Assert.That(ArcanaManager.ChestItemCount(1), Is.EqualTo(1), "No Arcana: the roll stands.");
            NewManager(ArcanaData.ArcanaType.GameKiller);
            Assert.That(ArcanaManager.GameKillerActive, Is.True);
            Assert.That(ArcanaManager.ChestItemCount(1), Is.EqualTo(3));
            Assert.That(ArcanaManager.ChestItemCount(5), Is.EqualTo(5));
            Assert.That(ArcanaManager.ArcanaChestExtraItems, Is.EqualTo(2));
        }

        [Test]
        public void OutOfBounds_DoublesOrologion_AndHeartOfFire_QuadruplesHellfireBreath()
        {
            float freeze = LightSourceDrops.Weight(LightSourceDrops.Drop.TimeFreeze, 10, 1f);
            float breath = LightSourceDrops.Weight(LightSourceDrops.Drop.FireBreath, 10, 1f);
            NewManager(ArcanaData.ArcanaType.OutOfBounds, ArcanaData.ArcanaType.HeartOfFire);
            Assert.That(LightSourceDrops.Weight(LightSourceDrops.Drop.TimeFreeze, 10, 1f), Is.EqualTo(freeze * 2f).Within(0.001f));
            Assert.That(LightSourceDrops.Weight(LightSourceDrops.Drop.FireBreath, 10, 1f), Is.EqualTo(breath * 4f).Within(0.001f));
            Assert.That(LightSourceDrops.Weight(LightSourceDrops.Drop.GoldCoin, 10, 1f), Is.EqualTo(50f));
        }

        [Test]
        public void Slash_TurnsOnTheWeaponsSlashCrits_AndANewRunStartsWithout()
        {
            var manager = NewManager(ArcanaData.ArcanaType.Slash);
            Assert.That(AutoAttackWeapon.SlashArcanaActive, Is.True);
            typeof(ArcanaManager).GetMethod("OnDestroy", Private).Invoke(manager, null);
            _managers.Remove(manager);
            Assert.That(AutoAttackWeapon.SlashArcanaActive, Is.False);
        }

        [Test]
        public void ArcanaStatFactors_MultiplyOnTop_AndShadowKaelsCopyNeverTakesThem()
        {
            var stats = Track(new GameObject("Stats")).AddComponent<PlayerStats>();
            stats.ArcanaAreaFactor = 1.25f;
            stats.ArcanaLuckFactor = 2f;
            stats.ArcanaSpeedFactor = 0.5f;
            stats.ArcanaCooldownBonus = 0.4f;
            stats.ArcanaMightBonus = 1f;
            stats.ArcanaDurationBonus = 0.01f;
            Assert.That(stats.Area, Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(stats.Luck, Is.EqualTo(2f).Within(0.001f));
            Assert.That(stats.ProjectileSpeed, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(stats.Cooldown, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(stats.Might, Is.EqualTo(2f).Within(0.001f));
            Assert.That(stats.Duration, Is.EqualTo(1.01f).Within(0.001f));
            stats.AddLuck(0.5f);
            Assert.That(stats.Luck, Is.EqualTo(3f).Within(0.001f), "A raw gain is doubled too (Wicked Season doubles the whole stat).");
            var mirror = Track(new GameObject("Mirror")).AddComponent<PlayerStats>();
            mirror.CopyFrom(stats);
            Assert.That(mirror.Area, Is.EqualTo(1f).Within(0.001f), "Shadow Kael's copy has no Arcana factor.");
            Assert.That(mirror.Luck, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(mirror.Might, Is.EqualTo(1f).Within(0.001f));
            Assert.That(mirror.Cooldown, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void HealModifier_DoublesHeals_AndEveryHealIsReported_EvenAtFullHealth()
        {
            var health = Track(new GameObject("Hero")).AddComponent<HealthController>();
            health.Initialize(100);
            var reported = new List<int>();
            health.OnHealAttempted += reported.Add;
            health.HealModifier = x => x * 2;
            health.Heal(10);
            Assert.That(reported, Is.EqualTo(new[] { 20 }), "Doubled, and reported at full health ('does not require missing health').");
            health.TakeDamage(new DamagePacket(50, Vector3.zero, Vector3.zero) { IgnoresHitInvulnerability = true });
            health.Heal(10);
            Assert.That(health.CurrentHealth, Is.EqualTo(70));
            health.AddMaxHealthWithoutHealing(0.5f);
            Assert.That(health.MaxHealth, Is.EqualTo(100), "Half a point waits for the next.");
            health.AddMaxHealthWithoutHealing(0.5f);
            Assert.That(health.MaxHealth, Is.EqualTo(101));
            Assert.That(health.CurrentHealth, Is.EqualTo(70), "Divine Bloodline: current health is not restored.");
        }
    }
}
