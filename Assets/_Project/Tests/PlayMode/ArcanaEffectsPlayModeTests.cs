using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Weapons;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// The four Arcanas Soul Hunter has, in the real game (each card's wiki page, raw, fetched 2026-10-09):
    /// - Awake_(IV): "Gives +3 Revivals. Consuming a Revival gives +10% Max Health, +1 Armor, and +5% Might, Area, Duration,
    ///   and Speed" (the Overview: "1.1x Max Health"; the bonus "applies to all revives").
    /// - Iron_Blue_Will_(VII) / Waltz_of_Pearls_(XI): "Listed weapon projectiles gain up to 3 bounces", listed weapons only,
    ///   and never Shadow Kael's mirrored shots.
    /// - Gemini_(I): "Listed weapons come with a counterpart" that levels with them, takes no slot, is not in the weapons
    ///   list and cannot evolve; Nduja Fritta Tanto (Hellfire Breath) breathes a second stream from the back.
    /// Every test plays on a throw-away save (GameplayPlayModeTestBase).
    /// </summary>
    public class ArcanaEffectsPlayModeTests : GameplayPlayModeTestBase
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private readonly List<Object> _cards = new List<Object>();

        [TearDown]
        public void DestroyCards()
        {
            foreach (var card in _cards) if (card != null) Object.Destroy(card);
            _cards.Clear();
        }

        private ArcanaData Grant(ArcanaData.ArcanaType type)
        {
            var card = ScriptableObject.CreateInstance<ArcanaData>();
            card.CardName = "Test " + type;
            card.Type = type;
            _cards.Add(card);
            var arcanas = ArcanaManager.Instance;
            Assert.IsNotNull(arcanas, "No ArcanaManager in Main_Gameplay.");
            arcanas.ApplyArcana(card);
            Assert.That(arcanas.HasArcana(type), Is.True);
            return card;
        }

        /// <summary>A quiet field: no spawner, the starting weapons off, no horde or gems (no level-ups), the hero unhurt.</summary>
        private IEnumerator QuietField()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            foreach (var enemy in EnemyController.ActiveEnemies.ToList()) if (enemy != null) enemy.gameObject.SetActive(false);
            foreach (var gem in Object.FindObjectsByType<SoulHunter.Gameplay.Pickups.XPGem>(FindObjectsSortMode.None)) gem.gameObject.SetActive(false);
            yield return CloseOpenLevelUps();
            yield return null;
        }

        /// <summary>Harmless targets that cannot die, so aimed weapons fire and nothing levels the hero.</summary>
        private static void PlaceTargets(Vector3 around, float distance = 4f)
        {
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var enemyPrefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            for (int i = 0; i < 8; i++)
            {
                var enemy = Object.Instantiate(enemyPrefab, around + Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * distance, Quaternion.identity);
                enemy.GetComponent<HealthController>().Initialize(1000000);
                var touch = enemy.GetComponent<TouchDamage>();
                if (touch != null) touch.enabled = false;
                var body = enemy.GetComponent<Rigidbody>();
                if (body != null) body.isKinematic = true;
            }
        }

        private static T Equip<T>(UpgradeType type) where T : AutoAttackWeapon
        {
            var weapons = WeaponManager.Instance;
            weapons.GiveWeapon(type, 1);
            string name = weapons.GetWeaponObject(type).name;
            var weapon = PlayerController.Instance.GetComponentsInChildren<T>(true)
                .FirstOrDefault(w => w.gameObject.name.StartsWith(name) && w.GetComponent<GeminiCounterpart>() == null);
            Assert.IsNotNull(weapon, $"{type} is equipped.");
            weapon.gameObject.SetActive(true);
            return weapon;
        }

        /// <summary>Fires one attack now and returns the projectiles it spawned in this frame (not shots already flying).</summary>
        private static List<Projectile> FireNow(AutoAttackWeapon weapon)
        {
            var flying = new HashSet<Projectile>(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Where(p => p.isActiveAndEnabled));
            typeof(AutoAttackWeapon).GetMethod("Attack", Private).Invoke(weapon, null);
            return Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)
                .Where(p => p.isActiveAndEnabled && p.SourceWeapon == weapon && !flying.Contains(p)).ToList();
        }

        /// <summary>The bounces of the next Magic Wand volley's first bolt (the wand runs its own volley cycle in Update).</summary>
        private static IEnumerator MagicWandBounces(MagicWandWeapon wand, System.Action<int> result)
        {
            int bounces = -1;
            System.Action<Projectile> fired = p => { if (bounces < 0) bounces = p.MaxBounces; };
            wand.OnProjectileFired += fired;
            typeof(MagicWandWeapon).GetField("_remainingShots", Private).SetValue(wand, 0);
            typeof(MagicWandWeapon).GetField("_cooldownTimer", Private).SetValue(wand, 0f);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (bounces < 0 && Time.realtimeSinceStartup < deadline) yield return null;
            wand.OnProjectileFired -= fired;
            Assert.That(bounces, Is.GreaterThanOrEqualTo(0), "The Magic Wand fired.");
            result(bounces);
        }

        // ------------------------------------------------------------------ Awake (IV)

        [UnityTest]
        public IEnumerator Awake_GivesThreeRevivals_AndEachRevivalUsedGivesTheStatBonus_Stacking()
        {
            yield return QuietField();
            var player = PlayerController.Instance;
            var stats = player.GetComponent<PlayerStats>();
            var health = player.GetComponent<HealthController>();
            while (stats.UseRevival()) { }

            // A Revival used before the card is held gives nothing.
            stats.AddRevival(1);
            int maxBefore = health.MaxHealth;
            yield return Kill(health);
            Assert.That(health.MaxHealth, Is.EqualTo(maxBefore), "No Awake held: a Revival gives no bonus.");

            int armor = stats.RawArmor;
            float might = stats.RawMight, area = stats.RawArea, duration = stats.RawDuration, speed = stats.RawProjectileSpeed;
            int maxHealth = health.MaxHealth;
            Grant(ArcanaData.ArcanaType.Awake);
            Assert.That(stats.Revivals, Is.EqualTo(3), "Awake: \"Gives +3 Revivals.\"");
            Assert.That(health.MaxHealth, Is.EqualTo(maxHealth), "No Max Health when the card is taken (it was +10% at once before).");
            Assert.That(stats.RawArmor, Is.EqualTo(armor), "No Armor when the card is taken.");
            Assert.That(stats.RawMight, Is.EqualTo(might));

            yield return Kill(health);
            Assert.That(health.CurrentHealth, Is.GreaterThan(0), "Revived. " + Errors());
            Assert.That(stats.Revivals, Is.EqualTo(2));
            Assert.That(health.MaxHealth, Is.EqualTo(Mathf.RoundToInt(maxHealth * 1.1f)), "Max Health x1.1 per Revival used.");
            Assert.That(stats.RawArmor, Is.EqualTo(armor + 1), "+1 Armor per Revival used.");
            Assert.That(stats.RawMight, Is.EqualTo(might + 0.05f).Within(1e-4f), "+5% Might.");
            Assert.That(stats.RawArea, Is.EqualTo(area + 0.05f).Within(1e-4f), "+5% Area.");
            Assert.That(stats.RawDuration, Is.EqualTo(duration + 0.05f).Within(1e-4f), "+5% Duration.");
            Assert.That(stats.RawProjectileSpeed, Is.EqualTo(speed + 0.05f).Within(1e-4f), "+5% Speed.");

            yield return Kill(health);
            Assert.That(stats.Revivals, Is.EqualTo(1));
            Assert.That(health.MaxHealth, Is.EqualTo(Mathf.RoundToInt(maxHealth * 1.1f * 1.1f)), "The bonus stacks.");
            Assert.That(stats.RawArmor, Is.EqualTo(armor + 2));
            Assert.That(stats.RawMight, Is.EqualTo(might + 0.10f).Within(1e-4f));
            Assert.That(ArcanaManager.Instance.AwakeBonusesGiven, Is.EqualTo(2));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private IEnumerator Kill(HealthController health)
        {
            health.EndRevivalInvulnerability();
            health.ShieldedUntil = -1f;
            health.IsInvincible = false;
            var packet = new DamagePacket(health.MaxHealth * 10, health.transform.position, Vector3.zero);
            packet.IgnoresHitInvulnerability = true;
            health.TakeDamage(packet);
            yield return null;
            var screen = SoulHunter.Gameplay.UI.ReviveScreen.Current;
            Assert.That(screen != null && screen.IsOpen, Is.True, "A death with a Revival left shows the Revive screen. " + Errors());
            Click(screen.ReviveButton, "Revive");
            yield return null;
        }

        // ------------------------------------------------------------------ Iron Blue Will (VII), Waltz of Pearls (XI)

        [UnityTest]
        public IEnumerator IronBlueWill_KnifeBouncesThreeTimes_TheMagicWandDoesNot()
        {
            yield return QuietField();
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            PlaceTargets(player.transform.position);
            int character = Mathf.Max(0, player.GetComponent<PlayerStats>().ExtraProjectileBounces);
            var knife = Equip<KnifeWeapon>(UpgradeType.Knife);
            var wand = Equip<MagicWandWeapon>(UpgradeType.MagicWand);
            yield return null;

            var before = FireNow(knife);
            Assert.That(before, Is.Not.Empty, "The Knife threw.");
            Assert.That(before.All(p => p.MaxBounces == character), Is.True, "No card: no Arcana bounces.");

            Grant(ArcanaData.ArcanaType.IronBlueWill);
            var shots = FireNow(knife);
            Assert.That(shots, Is.Not.Empty);
            Assert.That(shots.All(p => p.MaxBounces == character + 3), Is.True,
                "Iron Blue Will: the Knife's shots gain 3 bounces. Got " + string.Join(",", shots.Select(p => p.MaxBounces)));

            int wandBounces = -1;
            yield return MagicWandBounces(wand, b => wandBounces = b);
            Assert.That(wandBounces, Is.EqualTo(character), "The Magic Wand is not on Iron Blue Will's list.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator WaltzOfPearls_MagicWandBouncesThreeTimes_CrossOffTheScreenTwice_KnifeNothing()
        {
            yield return QuietField();
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            PlaceTargets(player.transform.position);
            int character = Mathf.Max(0, player.GetComponent<PlayerStats>().ExtraProjectileBounces);
            var knife = Equip<KnifeWeapon>(UpgradeType.Knife);
            var wand = Equip<MagicWandWeapon>(UpgradeType.MagicWand);
            var cross = Equip<CrossWeapon>(UpgradeType.Cross);
            yield return null;

            Grant(ArcanaData.ArcanaType.WaltzOfPearls);
            int wandBounces = -1;
            yield return MagicWandBounces(wand, b => wandBounces = b);
            Assert.That(wandBounces, Is.EqualTo(character + 3), "Waltz of Pearls: the Magic Wand hits 3 more targets per shot.");

            var knives = FireNow(knife);
            Assert.That(knives, Is.Not.Empty);
            Assert.That(knives.All(p => p.MaxBounces == character), Is.True, "The Knife is not on Waltz of Pearls' list.");

            var thrown = new HashSet<BoomerangProjectile>(Object.FindObjectsByType<BoomerangProjectile>(FindObjectsSortMode.None).Where(b => b.isActiveAndEnabled));
            typeof(AutoAttackWeapon).GetMethod("Attack", Private).Invoke(cross, null);
            var boomerang = Object.FindObjectsByType<BoomerangProjectile>(FindObjectsSortMode.None)
                .FirstOrDefault(b => b.isActiveAndEnabled && !thrown.Contains(b));
            Assert.IsNotNull(boomerang, "The Cross was thrown.");
            Assert.That(boomerang.ScreenBouncesLeft, Is.EqualTo(2), "Waltz of Pearls: the Cross bounces off the screen edges twice.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator ShadowKaelsMirroredKnife_GetsNoPlayerArcanaBounces()
        {
            yield return QuietField();
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            Grant(ArcanaData.ArcanaType.IronBlueWill);
            Grant(ArcanaData.ArcanaType.WaltzOfPearls);

            // A boss holding a mirrored Knife, as ShadowKaelController.CaptureFrom builds it (a weapon under the enemy).
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var enemyPrefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            var boss = Object.Instantiate(enemyPrefab, player.transform.position + Vector3.forward * 6f, Quaternion.identity);
            boss.GetComponent<HealthController>().Initialize(1000000);
            var bossKnife = Object.Instantiate(WeaponManager.Instance.GetWeaponObject(UpgradeType.Knife), boss.transform).GetComponent<KnifeWeapon>();
            bossKnife.gameObject.SetActive(true);
            yield return null;
            Assert.That(bossKnife.IsHeldByEnemy, Is.True);

            var shots = FireNow(bossKnife);
            Assert.That(shots, Is.Not.Empty, "The mirrored Knife threw.");
            Assert.That(shots.All(p => p.TargetsPlayer), Is.True);
            Assert.That(shots.All(p => p.MaxBounces == 0), Is.True,
                "The player's Arcanas never reach Shadow Kael's shots. Got " + string.Join(",", shots.Select(p => p.MaxBounces)));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // ------------------------------------------------------------------ Gemini (I)

        [UnityTest]
        public IEnumerator Gemini_PeachoneAndGunGetACounterpart_ThatLevelsWithThem_AndTakesNoSlot()
        {
            yield return QuietField();
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            PlaceTargets(player.transform.position);
            var weapons = WeaponManager.Instance;
            var arcanas = ArcanaManager.Instance;

            Grant(ArcanaData.ArcanaType.Gemini);
            var peachone = Equip<PeachoneWeapon>(UpgradeType.Peachone);
            var gun = Equip<GunWeapon>(UpgradeType.Gun);
            var knife = Equip<KnifeWeapon>(UpgradeType.Knife);
            yield return null;
            int slots = weapons.GetActiveWeaponsCount();
            int hudWeapons = weapons.GetInventory(false).Count;
            Assert.That(weapons.GetInventory(false).Count(s => s.Type == UpgradeType.Peachone), Is.EqualTo(1), "One Peachone in the weapons list.");
            Assert.That(weapons.GetInventory(false).Count(s => s.Type == UpgradeType.Gun), Is.EqualTo(1), "One Gun in the weapons list.");

            var birdTwin = arcanas.GeminiCounterpartOf(peachone) as PeachoneWeapon;
            var gunTwin = arcanas.GeminiCounterpartOf(gun) as GunWeapon;
            Assert.IsNotNull(birdTwin, "Peachone comes with a counterpart. " + Errors());
            Assert.IsNotNull(gunTwin, "The Gun comes with a counterpart.");
            Assert.IsNull(arcanas.GeminiCounterpartOf(knife), "The Knife is not on Gemini's list.");
            Assert.That(birdTwin.isActiveAndEnabled && gunTwin.isActiveAndEnabled, Is.True);
            Assert.That(birdTwin.CurrentLevel, Is.EqualTo(1));
            Assert.That(birdTwin.CircleRadius, Is.EqualTo(peachone.CircleRadius + PeachoneWeapon.GeminiExtraRadius).Within(1e-4f),
                "\"Attacks slightly further outward than Peachone.\"");
            Assert.That(gunTwin.ArmOffsetDegrees, Is.EqualTo(GunWeapon.GeminiArmOffsetDegrees), "\"Fires at a slightly different angle.\"");
            Assert.That(birdTwin.LimitBreakSource, Is.SameAs(peachone), "Peachone's counterpart shares its Limit Break bonuses.");
            Assert.That(gunTwin.LimitBreakSource, Is.Null, "The Gun's counterpart does not (wiki: only with Phieraggi).");

            // Not a slot, not in the weapons list, not mirrored by Shadow Kael.
            Assert.That(weapons.GetEquippedWeapons().Any(p => p.Value == birdTwin || p.Value == gunTwin), Is.False,
                "Counterparts are not in the equipped list Shadow Kael copies.");
            Assert.That(weapons.GetActiveWeaponsCount(), Is.EqualTo(slots));
            Assert.That(weapons.GetInventory(false).Count, Is.EqualTo(hudWeapons));
            Assert.That(birdTwin.GetComponent<GeminiCounterpart>().Original, Is.SameAs(peachone));

            // "level up at the same time"
            weapons.GiveWeapon(UpgradeType.Peachone, 3);
            weapons.GiveWeapon(UpgradeType.Gun, 4);
            yield return null;
            yield return null;
            Assert.That(birdTwin.CurrentLevel, Is.EqualTo(3), "The counterpart levels with Peachone.");
            Assert.That(gunTwin.CurrentLevel, Is.EqualTo(4), "The counterpart levels with the Gun.");

            // "will only provide 1 counterpart"
            arcanas.RefreshGemini();
            yield return null;
            Assert.That(weapons.GetActiveWeaponsCount(), Is.EqualTo(slots), "Still no slot taken.");
            Assert.That(player.GetComponentsInChildren<PeachoneWeapon>(true).Length, Is.EqualTo(2), "One Peachone and one counterpart.");
            Assert.That(player.GetComponentsInChildren<GunWeapon>(true).Length, Is.EqualTo(2), "One Gun and one counterpart.");

            // The Gun's counterpart fires its own volley, turned by its offset.
            var twinShots = FireNow(gunTwin);
            Assert.That(twinShots, Is.Not.Empty, "The Gun's counterpart fires.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// CORE-02 (2026-10-19). VS wiki Gemini_(I) (raw): counterparts "cannot be evolved or united, and remain as
        /// independent weapons if their counterparts unite"; "If the Arcana is picked up with Phieraggi ... the
        /// counterparts of their constituents are given at max level; with Vandalier, both are given at the level of it and
        /// level up with Vandalier." So counterparts made before a union keep going; Gemini taken while holding Twilight
        /// Flock or Revenant Wheel used to give nothing.
        /// </summary>
        [UnityTest]
        public IEnumerator Gemini_CounterpartsStayAfterAUnion_AndGeminiWithAUnionGivesItsConstituentsCounterparts()
        {
            UpgradeData Level(UpgradeType type, int level) =>
                SoulHunter.Gameplay.Data.GameContentCatalog.Load().Upgrades.First(u => u != null && u.Type == type && u.Level == level);
            AutoAttackWeapon[] Twins() => PlayerController.Instance.GetComponentsInChildren<GeminiCounterpart>(true)
                .Select(c => c.Weapon).ToArray();

            // Gemini first, then the union: the birds' counterparts stay, on their own.
            yield return QuietField();
            var weapons = WeaponManager.Instance;
            Grant(ArcanaData.ArcanaType.Gemini);
            weapons.GiveWeapon(UpgradeType.Peachone, 8);
            weapons.GiveWeapon(UpgradeType.DuskRaven, 8);
            yield return null;
            Assert.That(Twins().Length, Is.EqualTo(2), "Peachone and Dusk Raven come with counterparts. " + Errors());
            Assert.That(weapons.TryEvolveWeapon(out string united), Is.True);
            Assert.That(united, Is.EqualTo("Union_TwilightFlock"));
            yield return null;
            Assert.That(Twins().Length, Is.EqualTo(2), "No counterpart of the union itself.");
            Assert.That(Twins().All(t => t.isActiveAndEnabled && t.CurrentLevel == 8), Is.True, "They remain as independent weapons.");

            // The unions first, then Gemini: each constituent gets a counterpart.
            yield return QuietField();
            weapons = WeaponManager.Instance;
            weapons.GiveWeapon(UpgradeType.Peachone, 8);
            weapons.GiveWeapon(UpgradeType.DuskRaven, 8);
            yield return null;
            Assert.That(weapons.TryEvolveWeapon(out _), Is.True, "Twilight Flock.");
            weapons.ApplyUpgrade(Level(UpgradeType.TwilightFlock, 2));
            weapons.GiveWeapon(UpgradeType.Gun, 8);
            weapons.GiveWeapon(UpgradeType.AzureRepeater, 8);
            weapons.ApplyUpgrade(Level(UpgradeType.Tiragisu, 1));
            yield return null;
            Assert.That(weapons.TryEvolveWeapon(out string wheelName), Is.True);
            Assert.That(wheelName, Is.EqualTo("Union_RevenantWheel"));
            yield return null;
            var flock = PlayerController.Instance.GetComponentInChildren<TwilightFlockWeapon>();
            var wheel = PlayerController.Instance.GetComponentInChildren<RevenantWheelWeapon>();
            Assert.That(flock != null && wheel != null, Is.True);
            Assert.That(Twins(), Is.Empty);

            Grant(ArcanaData.ArcanaType.Gemini);
            yield return null;
            var twins = Twins();
            var birds = twins.OfType<PeachoneWeapon>().ToArray();
            Assert.That(birds.Length, Is.EqualTo(2), "Vandalier: both birds get a counterpart. " + Errors());
            Assert.That(birds.Count(b => b is DuskRavenWeapon), Is.EqualTo(1));
            Assert.That(birds.All(b => b.CurrentLevel == 2 && b.GetComponent<GeminiCounterpart>().Original == flock), Is.True,
                "Given at the union's level, following it.");
            Assert.That(birds.All(b => b.LimitBreakSource == flock), Is.True, "Vandalier passes its Limit Break bonuses on.");
            var guns = twins.OfType<GunWeapon>().ToArray();
            Assert.That(guns.Length, Is.EqualTo(2), "Phieraggi: both guns get a counterpart.");
            Assert.That(guns.Count(g => g is AzureRepeaterWeapon), Is.EqualTo(1));
            Assert.That(guns.All(g => g.CurrentLevel == g.MaxLevel && g.isActiveAndEnabled), Is.True, "Given at max level.");
            Assert.That(weapons.GetEquippedWeapons().Any(p => twins.Contains(p.Value)), Is.False, "No slot, not mirrored.");

            weapons.ApplyUpgrade(Level(UpgradeType.TwilightFlock, 3));
            yield return null;
            yield return null;
            Assert.That(birds.All(b => b.CurrentLevel == 3), Is.True, "They level up with Vandalier.");
            weapons.ApplyUpgrade(Level(UpgradeType.TwilightFlock, 4));
            ArcanaManager.Instance.RefreshGemini();
            yield return null;
            Assert.That(Twins().Length, Is.EqualTo(4), "One counterpart per kind.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Gemini_HellfireBreath_BreathesASecondStreamFromTheBack()
        {
            yield return QuietField();
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            var spawnFlame = typeof(FireBreath).GetMethod("SpawnFlame", Private, null, System.Type.EmptyTypes, null);
            Assert.IsNotNull(spawnFlame);

            var breath = FireBreath.Begin(player.transform);
            yield return null;
            breath.StopAllCoroutines();
            List<Projectile> Flames() => Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)
                .Where(p => p.isActiveAndEnabled && p.SourceWeapon == breath).ToList();
            foreach (var old in Flames()) old.gameObject.SetActive(false);

            spawnFlame.Invoke(breath, null);
            Assert.That(Flames().Count, Is.EqualTo(1), "Without Gemini: one stream.");
            foreach (var old in Flames()) old.gameObject.SetActive(false);

            Grant(ArcanaData.ArcanaType.Gemini);
            spawnFlame.Invoke(breath, null);
            var flames = Flames();
            Assert.That(flames.Count, Is.EqualTo(2), "Gemini: another stream of flame.");
            Assert.That(Vector3.Angle(flames[0].transform.forward, flames[1].transform.forward), Is.GreaterThan(180f - FireBreath.ConeDegrees - 1f),
                "The second stream comes from the back side of the character.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
