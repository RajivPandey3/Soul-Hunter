using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: CharacterStatBlockTests verifies the VS-style character stat block: every stat
    /// applies in play, the select screen's sheet matches it (character + gold power-ups), level-up
    /// bonuses and special abilities work, and every character's description ability is real data.
    /// </summary>
    public class CharacterStatBlockTests
    {
        private GameObject _object;
        private CharacterData _character;

        [TearDown]
        public void TearDown()
        {
            if (_object != null) Object.DestroyImmediate(_object);
            if (_character != null) Object.DestroyImmediate(_character);
        }

        private PlayerStats CreatePlayer()
        {
            _object = new GameObject("Player");
            _object.AddComponent<HealthController>().Initialize(100);
            _object.AddComponent<PlayerExperience>();
            return _object.AddComponent<PlayerStats>();
        }

        private static CharacterData Catalog(string name)
        {
            var character = GameContentCatalog.FindCharacterExact(name);
            Assert.That(character, Is.Not.Null, $"{name} catalog mein nahi hai.");
            return character;
        }

        [Test]
        public void ApplyCharacter_AppliesTheWholeStatBlock()
        {
            // Learning Comment: VS ki tarah har stat (sirf Might/Area nahi) run mein lagu ho.
            var stats = CreatePlayer();
            _character = ScriptableObject.CreateInstance<CharacterData>();
            _character.BaseMoveSpeed = 1.2f;
            _character.StartingRecovery = 0.5f;
            _character.StartingSpeed = 1.1f;
            _character.StartingDuration = 1.3f;
            _character.StartingAmount = 2;
            _character.StartingRevivals = 1;
            _character.StartingMagnet = 1.5f;
            _character.StartingLuck = 1.2f;
            _character.StartingGrowth = 1.2f;
            _character.StartingGreed = 1.4f;
            _character.StartingCurse = 1.1f;

            stats.ApplyCharacter(_character);
            stats.ApplyCharacter(_character); // must not stack

            Assert.That(stats.MoveSpeedMultiplier, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(stats.Regen, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(stats.ProjectileSpeed, Is.EqualTo(1.1f).Within(0.001f));
            Assert.That(stats.Duration, Is.EqualTo(1.3f).Within(0.001f));
            Assert.That(stats.Amount, Is.EqualTo(2));
            Assert.That(stats.Revivals, Is.EqualTo(1));
            Assert.That(stats.Magnet, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(stats.Luck, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(stats.ExpBonus, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(stats.Greed, Is.EqualTo(1.4f).Within(0.001f));
            Assert.That(stats.Curse, Is.EqualTo(1.1f).Within(0.001f));
        }

        [Test]
        public void StatSheet_AddsGoldPowerUps_AndFormatsLikeVS()
        {
            // Learning Comment: Select screen wahi stats dikhaye jin se run shuru hoga (character + power-ups).
            var isolde = Catalog("Isolde");
            // VS PowerUps (PowerUps page): Might +5% per rank, Armor +1, Greed +10%, Revival +1.
            var bought = new SoulHunter.Core.Persistence.GameData();
            SoulHunter.Core.Persistence.PowerUps.SetRank(bought, SoulHunter.Core.Persistence.PowerUpId.Might, 2);
            SoulHunter.Core.Persistence.PowerUps.SetRank(bought, SoulHunter.Core.Persistence.PowerUpId.Armor, 1);
            SoulHunter.Core.Persistence.PowerUps.SetRank(bought, SoulHunter.Core.Persistence.PowerUpId.Greed, 1);
            SoulHunter.Core.Persistence.PowerUps.SetRank(bought, SoulHunter.Core.Persistence.PowerUpId.Revival, 1);
            var sheet = CharacterStatSheet.Build(isolde, bought);
            Assert.That(sheet.Might, Is.EqualTo(isolde.StartingMight + 0.1f).Within(0.001f));
            Assert.That(sheet.Armor, Is.EqualTo(isolde.StartingArmor + 1));
            Assert.That(sheet.Greed, Is.EqualTo(isolde.StartingGreed + 0.1f).Within(0.001f));
            Assert.That(sheet.Revivals, Is.EqualTo(1));

            var rows = sheet.DisplayRows().ToDictionary(r => r.Label);
            Assert.That(rows.Keys, Is.EquivalentTo(new[] { "Max Health", "Recovery", "Armour", "Move Speed", "Might", "Speed",
                "Duration", "Area", "Cooldown", "Amount", "Revival", "Magnet", "Luck", "Growth", "Greed", "Curse" }));
            Assert.That(rows["Cooldown"].Value, Is.EqualTo("-15%"));
            Assert.That(rows["Cooldown"].Changed, Is.True);
            Assert.That(rows["Armour"].Value, Is.EqualTo("+1"));
            Assert.That(rows["Revival"].Value, Is.EqualTo("+1"));
            Assert.That(rows["Area"].Value, Is.EqualTo("-"));
            Assert.That(rows["Area"].Changed, Is.False);

            var draven = CharacterStatSheet.Build(Catalog("Draven")).DisplayRows().ToDictionary(r => r.Label);
            Assert.That(draven["Amount"].Value, Is.EqualTo("+2"));
            var lumina = CharacterStatSheet.Build(Catalog("Lumina")).DisplayRows().ToDictionary(r => r.Label);
            Assert.That(lumina["Recovery"].Value, Is.EqualTo("1.0"));
        }

        /// <summary>
        /// vampire.survivors.wiki/w/Characters, "Character stats" (raw, 2026-10-12): character and Golden Egg values are
        /// white, PowerUp values yellow; the list ends with Reroll, Skip and Banish.
        /// </summary>
        [Test]
        public void SelectRows_MarkPowerUpValues_IncludeEggs_AndShowChargesOnceOwned()
        {
            var isolde = Catalog("Isolde"); // her own Cooldown -15%
            var data = new SoulHunter.Core.Persistence.GameData();
            var plain = CharacterStatSheet.SelectRows(isolde, data).ToDictionary(r => r.Label);
            Assert.That(plain.ContainsKey("Reroll") || plain.ContainsKey("Skip") || plain.ContainsKey("Banish"), Is.False, "No charges: no rows.");
            Assert.That(plain["Cooldown"].Value, Is.EqualTo("-15%"));
            Assert.That(plain["Cooldown"].FromPowerUps, Is.False, "A character's own value is not a PowerUp value.");

            SoulHunter.Core.Persistence.PowerUps.SetRank(data, SoulHunter.Core.Persistence.PowerUpId.Might, 2);
            SoulHunter.Core.Persistence.PowerUps.SetRank(data, SoulHunter.Core.Persistence.PowerUpId.Reroll, 1);
            SoulEggs.Add(data, isolde.CharacterName, SoulEggs.Stat.Area);
            SoulEggs.Add(data, isolde.CharacterName, SoulEggs.Stat.Area);
            var rows = CharacterStatSheet.SelectRows(isolde, data).ToDictionary(r => r.Label);
            Assert.That(rows["Might"].Value, Is.EqualTo("+10%"));
            Assert.That(rows["Might"].FromPowerUps, Is.True, "Bought in the shop: the highlight colour.");
            Assert.That(rows["Area"].Value, Is.EqualTo("+2%"), "Two Soul Eggs: +1% Area each, now shown.");
            Assert.That(rows["Area"].FromPowerUps, Is.False, "Egg values are white like the character's own.");
            Assert.That(rows["Cooldown"].FromPowerUps, Is.False);
            Assert.That(rows["Reroll"].Value, Is.EqualTo("+2"), "Reroll rank 1 gives 2.");
            Assert.That(rows["Reroll"].FromPowerUps, Is.True);
            Assert.That(rows.ContainsKey("Skip") || rows.ContainsKey("Banish"), Is.False);
            Assert.That(rows.Keys.Last(), Is.EqualTo("Reroll"), "The charges come last.");
            Assert.That(rows.Keys.Contains("Magnet"), Is.True, "Magnet is in the wiki's list.");
        }

        [Test]
        public void StatSheet_MatchesWhatPlayerStatsApplies()
        {
            // Learning Comment: Screen par dikhne wale numbers aur game ke asli numbers ek hi hon.
            foreach (var character in GameContentCatalog.Load().Characters.Where(c => c != null))
            {
                var stats = CreatePlayer();
                stats.ApplyCharacter(character);
                var sheet = CharacterStatSheet.Build(character);
                string n = character.CharacterName;
                Assert.That(stats.Might, Is.EqualTo(sheet.Might).Within(0.001f), n);
                Assert.That(stats.Area, Is.EqualTo(sheet.Area).Within(0.001f), n);
                Assert.That(stats.Cooldown, Is.EqualTo(sheet.Cooldown).Within(0.001f), n);
                Assert.That(stats.Armor, Is.EqualTo(sheet.Armor), n);
                Assert.That(stats.MoveSpeedMultiplier, Is.EqualTo(sheet.MoveSpeed).Within(0.001f), n);
                Assert.That(stats.Regen, Is.EqualTo(sheet.Recovery).Within(0.001f), n);
                Assert.That(stats.Amount, Is.EqualTo(sheet.Amount), n);
                Assert.That(stats.ExpBonus, Is.EqualTo(sheet.Growth).Within(0.001f), n);
                Object.DestroyImmediate(_object);
            }
        }

        [Test]
        public void LevelBonus_GrantsAtEveryNthLevel_ThroughLevelUps()
        {
            // Learning Comment: Corvan ko har 10 level par +10% Might mile, beech ke levels par nahi.
            var stats = CreatePlayer();
            stats.ApplyCharacter(Catalog("Corvan"));
            float start = stats.Might;
            var experience = _object.GetComponent<PlayerExperience>();

            while (experience.CurrentLevel < 9) experience.AddXP(experience.XPToNextLevel);
            Assert.That(stats.Might, Is.EqualTo(start).Within(0.001f), "Level 9 tak bonus nahi.");
            experience.AddXP(experience.XPToNextLevel);
            Assert.That(experience.CurrentLevel, Is.EqualTo(10));
            Assert.That(stats.Might, Is.EqualTo(start + 0.1f).Within(0.001f), "Level 10 par +10% Might.");
        }

        [Test]
        public void LevelBonus_StopsAtItsCap_LikeVsAntonio()
        {
            // VS wiki Antonio_Belpaese: "+10% Might every 10 levels until level 50. The maximum Might gained this way is +50%."
            var stats = CreatePlayer();
            var corvan = Catalog("Corvan");
            Assert.That(corvan.LevelBonusUntilLevel, Is.EqualTo(50));
            stats.ApplyCharacter(corvan);
            float start = stats.Might;
            for (int level = 2; level <= 100; level++) stats.ApplyLevelBonus(level);
            Assert.That(stats.Might, Is.EqualTo(start + 0.5f).Within(0.001f), "+10% at 10, 20, 30, 40 and 50; nothing after.");
        }

        [Test]
        public void LevelBonus_HasNoCap_WhenUntilLevelIsZero()
        {
            var character = ScriptableObject.CreateInstance<CharacterData>();
            try
            {
                character.LevelBonus = CharacterData.LevelBonusStat.Might;
                character.LevelBonusEveryLevels = 10;
                character.LevelBonusAmount = 0.1f;
                Assert.That(character.GrantsLevelBonusAt(90), Is.True);
                Assert.That(character.GrantsLevelBonusAt(95), Is.False);
                character.LevelBonusUntilLevel = 50;
                Assert.That(character.GrantsLevelBonusAt(50), Is.True);
                Assert.That(character.GrantsLevelBonusAt(60), Is.False);
            }
            finally { Object.DestroyImmediate(character); }
        }

        [Test]
        public void LevelBonus_ArmorEveryFifthLevel()
        {
            // Learning Comment: Valerius ko har 5 level par +1 Armor.
            var stats = CreatePlayer();
            stats.ApplyCharacter(Catalog("Valerius"));
            int start = stats.Armor;
            for (int level = 2; level <= 10; level++) stats.ApplyLevelBonus(level);
            Assert.That(stats.Armor, Is.EqualTo(start + 2));
            Assert.That(_object.GetComponent<HealthController>().Armor, Is.EqualTo(start + 2));
            // VS Divano Thelma has the same +1 Armor every 5 levels "until level 25" (+5 at most): Valerius stops there too.
            for (int level = 11; level <= 60; level++) stats.ApplyLevelBonus(level);
            Assert.That(stats.Armor, Is.EqualTo(start + 5), "+1 at 5, 10, 15, 20 and 25; nothing after.");
        }

        [Test]
        public void LowHealthSpeed_GrowsAsHealthDrops()
        {
            // Learning Comment: Rurik jitna zakhmi, utna tez.
            var stats = CreatePlayer();
            stats.ApplyCharacter(Catalog("Rurik"));
            Assert.That(stats.LowHealthSpeedFactor(100, 100), Is.EqualTo(1f).Within(0.001f));
            Assert.That(stats.LowHealthSpeedFactor(50, 100), Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(stats.LowHealthSpeedFactor(0, 100), Is.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void StartingShield_BlocksDamage_UntilItExpires()
        {
            // Learning Comment: Sylas ki shield ke dauran koi hit HP na kaate.
            var stats = CreatePlayer();
            stats.ApplyCharacter(Catalog("Sylas"));
            Assert.That(stats.StartingShieldSeconds, Is.GreaterThan(0f));
            var health = _object.GetComponent<HealthController>();

            health.ShieldedUntil = Time.time + stats.StartingShieldSeconds;
            health.TakeDamage(new DamagePacket(30, Vector3.zero, Vector3.zero));
            Assert.That(health.CurrentHealth, Is.EqualTo(100));

            health.ShieldedUntil = Time.time - 1f;
            health.TakeDamage(new DamagePacket(30, Vector3.zero, Vector3.zero));
            Assert.That(health.CurrentHealth, Is.EqualTo(70));
        }

        [Test]
        public void CritMultiplier_IsTheCharacters_OrTheDefault()
        {
            // Learning Comment: Kaelen ke crits 3x, baaki sab ke 2x.
            Assert.That(AutoAttackWeapon.CritMultiplierFor(null), Is.EqualTo(2f));
            var stats = CreatePlayer();
            Assert.That(AutoAttackWeapon.CritMultiplierFor(stats), Is.EqualTo(2f));
            stats.ApplyCharacter(Catalog("Kaelen"));
            Assert.That(AutoAttackWeapon.CritMultiplierFor(stats), Is.EqualTo(3f));
        }

        [Test]
        public void Projectile_AddBounces_AddsBouncesAndHits()
        {
            // Learning Comment: Seraphina ke projectiles 3 baar zyada bounce karein.
            _object = new GameObject("Projectile", typeof(Rigidbody), typeof(SphereCollider));
            var projectile = _object.AddComponent<Projectile>();
            projectile.ConfigureContacts(1, 0, false);
            projectile.AddBounces(3);
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            Assert.That((int)typeof(Projectile).GetField("_maxBounces", flags).GetValue(projectile), Is.EqualTo(3));
            Assert.That((int)typeof(Projectile).GetField("_remainingHits", flags).GetValue(projectile), Is.EqualTo(4));
            Assert.That(Catalog("Seraphina").ExtraProjectileBounces, Is.EqualTo(3));
        }

        [Test]
        public void EveryDescriptionAbility_IsBackedByData()
        {
            // Learning Comment: Description jo wada kare, data mein wahi ho.
            Assert.That(Catalog("Aria").StartingGrowth, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(Catalog("Draven").StartingAmount, Is.EqualTo(2));
            Assert.That(Catalog("Gideon").StartingArea, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(Catalog("Gideon").StartingMight, Is.EqualTo(1f).Within(0.001f), "Gideon ki description sirf Area ka wada karti hai.");
            Assert.That(Catalog("Sylas").ShieldVisualPrefab, Is.Not.Null, "Sylas ki shield dikhni chahiye.");
            Assert.That(Catalog("Isolde").StartingCooldown, Is.EqualTo(0.85f).Within(0.001f));
            Assert.That(Catalog("Kaelen").CritDamageMultiplier, Is.EqualTo(3f));
            Assert.That(Catalog("Lumina").StartingRecovery, Is.EqualTo(1f));
            Assert.That(Catalog("Rurik").LowHealthSpeedBonus, Is.GreaterThan(0f));
            Assert.That(Catalog("Sylas").StartingShieldSeconds, Is.GreaterThan(0f));
            var valerius = Catalog("Valerius");
            Assert.That(valerius.LevelBonus, Is.EqualTo(CharacterData.LevelBonusStat.Armor));
            Assert.That(valerius.LevelBonusEveryLevels, Is.EqualTo(5));
            var corvan = Catalog("Corvan");
            Assert.That(corvan.LevelBonus, Is.EqualTo(CharacterData.LevelBonusStat.Might));
            Assert.That(corvan.LevelBonusEveryLevels, Is.EqualTo(10));
        }

        [Test]
        public void LegacyAntonioSave_ResolvesToCorvan()
        {
            // Learning Comment: Purane save mein "Antonio" ho to bhi Corvan mile, default nahi.
            Assert.That(GameContentCatalog.FindCharacterExact("Antonio"), Is.SameAs(Catalog("Corvan")));
            Assert.That(GameContentCatalog.CurrentName("Kael"), Is.EqualTo("Kael"));
        }

        [Test]
        public void StartingShieldVisual_AppearsOnlyWhileShielded()
        {
            // Learning Comment: Shield ke dauran visual dikhe; shield na ho to kuch na bane.
            CreatePlayer();
            var health = _object.GetComponent<HealthController>();
            var prefab = new GameObject("ShieldPrefab");
            try
            {
                Assert.That(StartingShieldVisual.Attach(health, prefab), Is.Null, "Shield nahi to visual nahi.");
                health.ShieldedUntil = Time.time + 10f;
                var shield = StartingShieldVisual.Attach(health, prefab);
                Assert.That(shield, Is.Not.Null);
                Assert.That(shield.transform.parent, Is.SameAs(_object.transform));
                Assert.That(shield.Visual, Is.Not.Null);
                Assert.That(shield.Visual.activeSelf, Is.True);
            }
            finally { Object.DestroyImmediate(prefab); }
        }

        [Test]
        public void EveryUpgrade_HasAnIcon()
        {
            // Learning Comment: Select screen cards aur HUD slots mein har weapon/passive ka icon dikhe.
            var upgrades = Resources.LoadAll<UpgradeData>("Upgrades");
            Assert.That(upgrades, Is.Not.Empty);
            foreach (var upgrade in upgrades)
                Assert.That(upgrade.Icon, Is.Not.Null, $"{upgrade.name} ka icon nahi hai.");
        }

        [Test]
        public void NoUpgradeIcon_ShowsAFileNameLabel()
        {
            // Learning Comment: kuch Art/UI sprites ke neeche file ka naam likha tha ("VFX_Slash.png", "Lamp_Post.png"),
            // jo level-up cards par dikhta tha (owner editor play, 2026-09-26). Label = picture ke neeche ek chhota alag block.
            var textures = Resources.LoadAll<UpgradeData>("Upgrades").Where(u => u.Icon != null).Select(u => u.Icon.texture).Distinct();
            foreach (var texture in textures)
            {
                string path = UnityEditor.AssetDatabase.GetAssetPath(texture);
                var copy = new Texture2D(2, 2);
                try
                {
                    Assert.That(copy.LoadImage(System.IO.File.ReadAllBytes(path)), path);
                    Assert.That(LabelRowsAtBottom(copy), Is.EqualTo(0), $"{path} has a file-name label under the picture.");
                }
                finally { Object.DestroyImmediate(copy); }
            }
        }

        /// <summary>Height of a small block at the bottom that is separated from the picture by empty rows (0 if none).</summary>
        private static int LabelRowsAtBottom(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            int w = texture.width, h = texture.height; // row 0 is the bottom
            bool Filled(int row) { for (int x = 0; x < w; x++) if (pixels[row * w + x].a > 25) return true; return false; }
            int start = 0;
            while (start < h && !Filled(start)) start++;
            int end = start;
            while (end < h && Filled(end)) end++;
            bool pictureAbove = false;
            for (int row = end; row < h && !pictureAbove; row++) pictureAbove = Filled(row);
            int height = end - start;
            return start < h && pictureAbove && height <= 14 && height <= 0.25f * h ? end : 0;
        }

        [Test]
        public void EveryCharacter_HasItsOwnAnimatedHeroModel_AndPortrait()
        {
            // Learning Comment: Har character ka apna 3D model aur apni portrait ho; koi placeholder nahi.
            var characters = GameContentCatalog.Load().Characters.Where(c => c != null).ToList();
            foreach (var character in characters)
            {
                string n = character.CharacterName;
                var model = character.CharacterModelPrefab;
                Assert.That(model, Is.Not.Null, $"{n} model.");
                Assert.That(model.GetComponentInChildren<SkinnedMeshRenderer>(true), Is.Not.Null, $"{n} ka model 3D hero nahi hai.");
                var animator = model.GetComponentInChildren<Animator>(true);
                Assert.That(animator, Is.Not.Null, $"{n} Animator.");
                Assert.That(animator.runtimeAnimatorController, Is.Not.Null, $"{n} animator controller.");
                Assert.That(model.GetComponentInChildren<SoulHunter.Gameplay.Animation.EntityAnimator>(true), Is.Not.Null, $"{n} EntityAnimator.");
                Assert.That(character.CharacterIcon, Is.Not.Null, $"{n} portrait.");
                Assert.That(character.CharacterIcon.texture.name, Does.StartWith("Portrait_"), $"{n} ki portrait abhi bhi placeholder hai.");
            }
            Assert.That(characters.Select(c => c.CharacterModelPrefab).Distinct().Count(), Is.EqualTo(characters.Count), "Har character ka model alag ho.");
            Assert.That(characters.Select(c => c.CharacterIcon).Distinct().Count(), Is.EqualTo(characters.Count), "Har character ki portrait alag ho.");
        }

        [Test]
        public void EveryHeroModel_HoldsItsCharactersStartingWeapon()
        {
            // Learning Comment: Model ke haath mein wahi hathiyar ho jo character asal mein chalata hai.
            string manifest = System.IO.File.ReadAllText(System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.dataPath), "ArtSource", "SH10_Heroes_v01", "manifest.json"));
            var held = System.Text.RegularExpressions.Regex.Matches(manifest, @"""character"":\s*""(\w+)"",\s*""weapon"":\s*""(\w+)""")
                .Cast<System.Text.RegularExpressions.Match>()
                .ToDictionary(m => "SH10_Hero_" + m.Groups[1].Value, m => m.Groups[2].Value);
            foreach (var character in GameContentCatalog.Load().Characters.Where(c => c != null))
            {
                string model = character.CharacterModelPrefab.name;
                Assert.That(held.ContainsKey(model), Is.True, $"{character.CharacterName}: {model} manifest mein nahi hai.");
                Assert.That(held[model], Is.EqualTo(character.StartingWeapon.ToString()),
                    $"{character.CharacterName} ke haath mein {held[model]} hai, par weapon {character.StartingWeapon} hai.");
            }
        }

        [Test]
        public void MoveSpeed_IsAMultiplier_ForEveryCharacter_AndNoVSNames()
        {
            // Learning Comment: Owner decision 2026-09-25: move speed multiplier hai (1.0 = base speed).
            foreach (var character in GameContentCatalog.Load().Characters.Where(c => c != null))
            {
                Assert.That(character.BaseMoveSpeed, Is.InRange(0.5f, 2f), $"{character.CharacterName} move speed multiplier nahi lagta.");
                Assert.That(character.CharacterName, Is.Not.EqualTo("Antonio"), "VS character ka naam copy nahi karna.");
            }
        }
    }
}
