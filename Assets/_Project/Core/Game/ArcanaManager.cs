using UnityEngine;
using System.Collections.Generic;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Weapons;
using System;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Learning Comment:
    /// ArcanaManager game ke shuru mein ya chest (11:00 or 21:00 min) se naye cards (Arcanas) apply karta hai.
    /// Ye player stats aur weapons dono par gehra asar daalta hai.
    /// VS Arcana mode (vampire.survivors.wiki/w/Arcanas, 2026-09-27): Arcanas exist only with the mode on. The run
    /// starts with one Arcana, and Arcana chests (dropped by the 11:00 and 21:00 chest elites, EnemySpawner) give one
    /// more each, until the player holds three; after that those elites drop regular chests. VS lets the player choose
    /// from four; Soul Hunter has no Arcana choice screen yet, so a random unowned Arcana is given.
    /// Before 2026-09-27 this manager gave the next Arcana on every stage's 11:00 and 21:00 whatever the mode.
    /// </summary>
    public class ArcanaManager : MonoBehaviour
    {
        public static ArcanaManager Instance { get; private set; }
        // Player ke paas maujood active cards
        public List<ArcanaData> ActiveArcanas { get; private set; } = new List<ArcanaData>();

        [Tooltip("Test karne ke liye shuru mein kon sa card dena hai? (Optional)")]
        [SerializeField] private ArcanaData _startingArcanaTest;
        [SerializeField] private List<ArcanaData> _availableArcanas = new List<ArcanaData>();
        /// <summary>VS: with three or more Arcanas, the Arcana elites drop regular chests.</summary>
        public const int MaxArcanasFromChests = 3;
        public event Action<ArcanaData> OnArcanaAwarded;
        /// <summary>The Arcana given at the start of an Arcana-mode run (null otherwise).</summary>
        public ArcanaData StartingArcana { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);
        }

        private void Start()
        {
            // Agar test card diya hai toh start mein apply kardo
            if (_startingArcanaTest != null)
            {
                ApplyArcana(_startingArcanaTest);
            }
            // VS Arcana mode: the run starts with one Arcana (RunModes is set in LevelProgressionManager.Awake).
            else if (RunModes.Arcana)
            {
                StartingArcana = GrantRandomAvailableArcana(UnityEngine.Random.value);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_revivalPlayer != null) _revivalPlayer.OnRevived -= OnPlayerRevived;
            if (_geminiWeapons != null) _geminiWeapons.OnInventoryChanged -= RefreshGemini;
        }

        /// <summary>True while an Arcana chest still gives an Arcana (fewer than three held and one left to give).</summary>
        public bool CanGrantFromChest => ActiveArcanas.Count < MaxArcanasFromChests && AvailableCount > 0;

        public int AvailableCount
        {
            get
            {
                int count = 0;
                foreach (var arcana in _availableArcanas)
                    if (arcana != null && !ActiveArcanas.Contains(arcana)) count++;
                return count;
            }
        }

        /// <summary>Gives a random Arcana the player does not hold yet. <paramref name="roll01"/> is uniform in [0, 1).</summary>
        public ArcanaData GrantRandomAvailableArcana(float roll01)
        {
            var available = new List<ArcanaData>();
            foreach (var arcana in _availableArcanas)
                if (arcana != null && !ActiveArcanas.Contains(arcana) && !available.Contains(arcana)) available.Add(arcana);
            if (available.Count == 0) return null;
            var pick = available[Mathf.Clamp(Mathf.FloorToInt(roll01 * available.Count), 0, available.Count - 1)];
            ApplyArcana(pick);
            return pick;
        }

        /// <summary>Tests and tools: the Arcanas this manager can give.</summary>
        public void SetAvailableArcanas(IEnumerable<ArcanaData> arcanas)
        {
            _availableArcanas = new List<ArcanaData>(arcanas);
        }

        public bool GrantNextAvailableArcana()
        {
            foreach (var arcana in _availableArcanas)
                if (arcana != null && !ActiveArcanas.Contains(arcana))
                {
                    ApplyArcana(arcana);
                    return true;
                }
            return false;
        }

        public void ApplyArcana(ArcanaData arcana)
        {
            if (arcana == null || ActiveArcanas.Contains(arcana)) return;

            ActiveArcanas.Add(arcana);
            Debug.Log($"[ArcanaManager] Card Selected: {arcana.CardNumber} - {arcana.CardName}");

            // Card ka jadu (Effect) yahan se shuru hota hai
            ExecuteArcanaEffect(arcana.Type);
            OnArcanaAwarded?.Invoke(arcana);
        }

        private void ExecuteArcanaEffect(ArcanaData.ArcanaType type)
        {
            switch (type)
            {
                case ArcanaData.ArcanaType.Awake:
                {
                    // vampire.survivors.wiki/w/Awake_(IV) (raw, fetched 2026-10-09): "Gives +3 Revivals. Consuming a Revival
                    // gives +10% Max Health, +1 Armor, and +5% Might, Area, Duration, and Speed." Only the Revivals come now;
                    // the stats come each time a Revival is used (OnPlayerRevived). Before 2026-10-09 the card gave +1 Armor
                    // and +10% Max Health at once and nothing on revival. The player's stats, never Shadow Kael's copy.
                    var player = PlayerController.Instance;
                    var playerStats = player != null ? player.Stats : null;
                    if (playerStats == null) return;
                    playerStats.AddRevival(AwakeRevivals);
                    HookRevivals(player);
                    break;
                }

                case ArcanaData.ArcanaType.IronBlueWill:
                case ArcanaData.ArcanaType.WaltzOfPearls:
                    // Read per weapon when it fires (EnemyBouncesFor, ScreenBouncesFor): only each card's listed weapons
                    // bounce. These cards used to set one global bounce count that every Projectile read, Shadow Kael's too.
                    break;

                case ArcanaData.ArcanaType.Gemini:
                    // Counterparts for the listed weapons held now and gained later (RefreshGemini).
                    HookGemini();
                    RefreshGemini();
                    break;
            }
        }

        // ------------------------------------------------------------------ Awake (IV)

        /// <summary>Awake (IV): "Gives +3 Revivals."</summary>
        public const int AwakeRevivals = 3;
        /// <summary>Awake (IV), per Revival consumed: Max Health x1.1 (the page's Overview: "1.1x Max Health"), multiplied.</summary>
        public const float AwakeMaxHealthFactor = 1.1f;
        /// <summary>Awake (IV), per Revival consumed: "+1 Armor".</summary>
        public const int AwakeArmor = 1;
        /// <summary>Awake (IV), per Revival consumed: "+5% Might, Area, Duration, and Speed" (added, as VS stat bonuses are).</summary>
        public const float AwakeStatBonus = 0.05f;
        /// <summary>Revivals used while Awake was held; each gave the bonus once (tests).</summary>
        public int AwakeBonusesGiven { get; private set; }

        private PlayerController _revivalPlayer;

        private void HookRevivals(PlayerController player)
        {
            if (player == null || _revivalPlayer == player) return;
            if (_revivalPlayer != null) _revivalPlayer.OnRevived -= OnPlayerRevived;
            _revivalPlayer = player;
            player.OnRevived += OnPlayerRevived;
        }

        /// <summary>
        /// Awake (IV): "The aforementioned bonus applies to all revives, including those from the Tirajisú, the Revival
        /// PowerUp", so it hooks PlayerController.OnRevived, which every Revival goes through (ConfirmRevive). The bonus
        /// stacks with each Revival. The wiki does not say whether the higher Max Health is filled: like Hollow Heart
        /// ("does not heal to fill up the newly increased Max Health") it is not, so the Revival's own heal stays the
        /// only heal (Soul Hunter's choice; the wiki is silent).
        /// </summary>
        private void OnPlayerRevived(int healed)
        {
            if (!HasArcana(ArcanaData.ArcanaType.Awake) || _revivalPlayer == null) return;
            var stats = _revivalPlayer.Stats;
            var health = _revivalPlayer.GetComponent<HealthController>();
            if (health != null) health.MultiplyMaxHealth(AwakeMaxHealthFactor, healByIncrease: false);
            if (stats != null)
            {
                stats.AddArmor(AwakeArmor);
                stats.AddMight(AwakeStatBonus);
                stats.AddArea(AwakeStatBonus);
                stats.AddDuration(AwakeStatBonus);
                stats.AddProjectileSpeed(AwakeStatBonus); // VS "Speed" is projectile Speed (wiki Player_stats), not Move Speed
            }
            AwakeBonusesGiven++;
        }

        // ------------------------------------------------------------------ Iron Blue Will (VII), Waltz of Pearls (XI)

        /// <summary>Both cards: "Listed weapon projectiles gain up to 3 bounces."</summary>
        public const int ArcanaEnemyBounces = 3;

        /// <summary>
        /// vampire.survivors.wiki/w/Iron_Blue_Will_(VII) (raw, fetched 2026-10-09), the weapons Soul Hunter has from its list:
        /// Knife, Thousand Edge, Axe and Phiera Der Tuphello (Soul Hunter's Gun) bounce off enemies; Death Spiral instead
        /// "bounces off the edges of the screen once". Eight The Sparrow, Carrello and the DLC weapons are not in Soul Hunter.
        /// "the ability to pass through enemies and ignore walls": a bounce is an extra hit, so the shot goes on through
        /// the enemy it hit; these shots already ignore walls (Projectile is not blocked by walls for these weapons, and
        /// thrown axes fly on a layer the Obstacle layer does not collide with). The wiki gives no pass-through chance.
        /// </summary>
        public static bool IronBlueWillBouncesOffEnemies(AutoAttackWeapon weapon) =>
            weapon is KnifeWeapon || weapon is ThousandEdgeWeapon || weapon is AxeWeapon || weapon is GunWeapon;

        /// <summary>
        /// vampire.survivors.wiki/w/Waltz_of_Pearls_(XI) (raw, fetched 2026-10-09): Magic Wand, Holy Wand and Fire Wand
        /// "can hit 3 more targets per shot"; Hellfire (Soul Hunter's Hellmaw Comet) bounces off the screen edges once,
        /// Cross and Heaven Sword twice ("They already have infinite pierce"). Carrello and the DLC weapons are not in
        /// Soul Hunter.
        /// </summary>
        public static bool WaltzBouncesOffEnemies(AutoAttackWeapon weapon) =>
            weapon is MagicWandWeapon || weapon is HolyWandWeapon || weapon is FireWandWeapon;

        /// <summary>Enemy bounces a weapon's shots gain from the player's Arcanas: 3 for a listed weapon, else 0.</summary>
        public static int EnemyBouncesFor(AutoAttackWeapon weapon)
        {
            // Shadow Kael's mirrored copies get none: the Arcanas are the player's, not part of the loadout he copies.
            var manager = Instance;
            if (weapon == null || weapon.IsHeldByEnemy || manager == null) return 0;
            if (manager.HasArcana(ArcanaData.ArcanaType.IronBlueWill) && IronBlueWillBouncesOffEnemies(weapon)) return ArcanaEnemyBounces;
            if (manager.HasArcana(ArcanaData.ArcanaType.WaltzOfPearls) && WaltzBouncesOffEnemies(weapon)) return ArcanaEnemyBounces;
            return 0;
        }

        /// <summary>Screen-edge bounces a weapon's shots gain: Death Spiral 1 (Iron Blue Will); Hellmaw Comet 1, Cross and Heaven Sword 2 (Waltz).</summary>
        public static int ScreenBouncesFor(AutoAttackWeapon weapon)
        {
            var manager = Instance;
            if (weapon == null || weapon.IsHeldByEnemy || manager == null) return 0;
            if (weapon is DeathSpiralWeapon) return manager.HasArcana(ArcanaData.ArcanaType.IronBlueWill) ? 1 : 0;
            if (weapon is HellmawCometWeapon) return manager.HasArcana(ArcanaData.ArcanaType.WaltzOfPearls) ? 1 : 0;
            if (weapon is CrossWeapon) return manager.HasArcana(ArcanaData.ArcanaType.WaltzOfPearls) ? 2 : 0; // Cross and Heaven Sword
            return 0;
        }

        // ------------------------------------------------------------------ Gemini (I)

        /// <summary>True while the player holds Gemini (I).</summary>
        public static bool GeminiActive => Instance != null && Instance.HasArcana(ArcanaData.ArcanaType.Gemini);

        /// <summary>
        /// vampire.survivors.wiki/w/Gemini_(I) (raw, fetched 2026-10-09): of its list Soul Hunter has Peachone (its
        /// counterpart "attacks slightly further outward") and Phiera Der Tuphello, the Gun (its counterpart "fires at a
        /// slightly different angle"). Nduja Fritta Tanto (Hellfire Breath) gets its back stream in FireBreath. Ebony
        /// Wings, Eight The Sparrow, Sorbetto and the rest are not in Soul Hunter.
        /// </summary>
        public static bool HasGeminiCounterpart(AutoAttackWeapon weapon) => weapon is PeachoneWeapon || weapon is GunWeapon;

        private readonly Dictionary<Type, AutoAttackWeapon> _geminiCounterparts = new Dictionary<Type, AutoAttackWeapon>();
        private WeaponManager _geminiWeapons;

        /// <summary>The counterpart made for this weapon's kind, or null (tests and tools).</summary>
        public AutoAttackWeapon GeminiCounterpartOf(AutoAttackWeapon original)
        {
            if (original == null || !_geminiCounterparts.TryGetValue(original.GetType(), out var twin) || twin == null) return null;
            return twin;
        }

        private void HookGemini()
        {
            var weapons = WeaponManager.Instance;
            if (weapons == null || _geminiWeapons == weapons) return;
            if (_geminiWeapons != null) _geminiWeapons.OnInventoryChanged -= RefreshGemini;
            _geminiWeapons = weapons;
            weapons.OnInventoryChanged += RefreshGemini;
        }

        private void Update()
        {
            // Gemini granted before the WeaponManager was ready: hook it once it is.
            if (_geminiWeapons == null && WeaponManager.Instance != null && HasArcana(ArcanaData.ArcanaType.Gemini))
            {
                HookGemini();
                RefreshGemini();
            }
        }

        /// <summary>
        /// Gives every listed weapon the player uses one counterpart: a copy of the same weapon at the same level, on the
        /// player, kept out of WeaponManager (no slot, no HUD, no evolution, not mirrored by Shadow Kael). "Having
        /// multiple copies of certain weapon and Gemini (I) will only provide 1 counterpart", so one per weapon kind.
        /// </summary>
        public void RefreshGemini()
        {
            if (!HasArcana(ArcanaData.ArcanaType.Gemini)) return;
            var weapons = WeaponManager.Instance;
            if (weapons == null) return;
            foreach (var pair in weapons.GetEquippedWeapons())
            {
                var original = pair.Value;
                if (pair.Key == null || original == null || original.IsHeldByEnemy || !HasGeminiCounterpart(original)) continue;
                if (_geminiCounterparts.TryGetValue(original.GetType(), out var existing) && existing != null) continue;

                var copy = Instantiate(pair.Key, original.transform.parent);
                copy.name = pair.Key.name + " (Gemini)";
                copy.SetActive(true);
                var twin = copy.GetComponent<AutoAttackWeapon>();
                if (twin == null) { Destroy(copy); continue; }
                copy.AddComponent<GeminiCounterpart>().Bind(original, twin);
                if (twin is PeachoneWeapon bird && original is PeachoneWeapon originalBird)
                {
                    bird.SetOrbit(originalBird.CircleRadius + PeachoneWeapon.GeminiExtraRadius, 180f);
                    // Gemini (I): "Only Peachone, Ebony Wings, Vandalier, Phieraggi, Gatti Amari, and Vicious Hunger can
                    // transfer their Limit Broken bonuses to their counterparts." The Gun's counterpart keeps none.
                    twin.LimitBreakSource = original;
                }
                if (twin is GunWeapon gun) gun.ArmOffsetDegrees = GunWeapon.GeminiArmOffsetDegrees;
                WeaponManager.ApplyWeaponLevel(copy, original.CurrentLevel);
                _geminiCounterparts[original.GetType()] = twin;

                // Results: in VS the counterpart is a weapon of its own (own name and damage). Soul Hunter names it after
                // its original ("<weapon> Twin") instead of using a VS counterpart name.
                var type = original is PeachoneWeapon ? UpgradeData.UpgradeType.Peachone : UpgradeData.UpgradeType.Gun;
                var definition = WeaponDefinitionLookup.Find(type);
                var tracker = RunStatsTracker.Instance;
                if (tracker != null) tracker.TrackWeapon(twin, UpgradeText.NameOf(type) + " Twin", definition != null ? definition.Icon : null, twin.MaxLevel, false);
            }
        }

        public bool HasArcana(ArcanaData.ArcanaType type)
        {
            foreach (var a in ActiveArcanas)
            {
                if (a != null && a.Type == type) return true;
            }
            return false;
        }
    }
}
