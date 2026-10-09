using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Weapons;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// The 18 Arcana cards added by release item P2-01 (2026-10-09). Every number is from the card's own wiki page,
    /// re-fetched raw on 2026-10-09 (vampire.survivors.wiki/w/&lt;Card&gt;?action=raw) and quoted at its constant. Where the
    /// page is silent the value is chosen by the balance method (owner rule 2026-10-09) and the reason is written next to
    /// it. Every effect is the player's only: a weapon held by Shadow Kael (AutoAttackWeapon.IsHeldByEnemy) gets nothing,
    /// and the stat changes live in PlayerStats' Arcana fields, which his copy (PlayerStats.CopyFrom) never takes.
    /// Special damage (blasts, zones, pulses) is credited by the card's Soul Hunter name on the Results screen (wiki
    /// Arcanas, Patch 0.9.0: "Results screen now shows damage from special Arcanas"). It is Magic damage, so it reaches the
    /// Level 7 ghosts, except Mending Wrath's healing pulse, which is Holy (owner's "type by theme" rule, Level 7).
    /// </summary>
    public partial class ArcanaManager
    {
        // ================================================================== shared numbers (balance method)

        /// <summary>
        /// Blast radius of the special explosions (Final Echo, Shattering Frost, Burning Core, Soul Barrage), times Area. No
        /// page gives a size in world units; 1.5 units is the Soul Hunter Lightning Ring strike and Peachone blast size
        /// (0.9) scaled to a blast meant to catch a small group, the same size as the Cherry Bomb's explosion read.
        /// </summary>
        public const float BlastRadius = 1.5f;

        /// <summary>Soul Hunter's base Move Speed (PlayerController: 5 units/s at multiplier 1).</summary>
        public const float BaseMoveSpeed = 5f;

        // ================================================================== 0 Game Killer -> "Soul Barrage"
        // wiki Game_Killer_(0): "Halts XP gain. Experience Gems turn into exploding projectiles. All Treasure Chests contain
        // at least 3 items." Infobox weapon: "damage = 10", "duration = 1.0 second". Overview: gems "hurl themselves towards
        // the closest enemy when collected"; "Growth multiplies the damage dealt by the gems". Treasure_Chest: "Will provide
        // two additional items if the player has Game Killer (0)."
        public const float GameKillerDamage = 10f;
        public const float GameKillerFlightSeconds = 1f;
        public const int GameKillerMinimumChestItems = 3;
        /// <summary>The gem's flight speed: the gem's own magnet speed (XPGem, 15 units/s); the wiki gives none.</summary>
        public const float GameKillerShotSpeed = 15f;

        public static bool GameKillerActive => Has(ArcanaData.ArcanaType.GameKiller);

        /// <summary>A regular chest's item count: at least 3 with Game Killer.</summary>
        public static int ChestItemCount(int rolled) => GameKillerActive ? Mathf.Max(GameKillerMinimumChestItems, rolled) : rolled;

        /// <summary>Items an Arcana chest adds after its Arcana: 2 with Game Killer, else 0.</summary>
        public static int ArcanaChestExtraItems => GameKillerActive ? GameKillerMinimumChestItems - 1 : 0;

        /// <summary>
        /// Moves a Soul Barrage gem toward the nearest enemy; true when it explodes (it reached one, or its 1 s is up). Damage
        /// 10 x Growth x Might (a weapon, so Might applies; the page adds Growth), radius 1.5 x Area.
        /// </summary>
        public static bool TickGemShot(Transform gem, float secondsLeft)
        {
            if (gem == null) return true;
            var target = NearestEnemy(gem.position);
            if (target != null)
            {
                Vector3 to = target.transform.position - gem.position;
                to.y = 0f;
                gem.position += Vector3.ClampMagnitude(to, GameKillerShotSpeed * Time.deltaTime);
                if (to.sqrMagnitude > 0.36f && secondsLeft > 0f) return false;
            }
            else if (secondsLeft > 0f) return false;
            var stats = HeroStats();
            float growth = stats != null ? stats.ExpBonus : 1f;
            float might = stats != null ? stats.Might : 1f;
            Blast(gem.position, BlastRadius * Area(stats), GameKillerDamage * growth * might, Of(ArcanaData.ArcanaType.GameKiller), DamageType.Magic, false);
            return true;
        }

        // ================================================================== II Twilight Requiem -> "Final Echo"
        // wiki Twilight_Requiem_(II): "Listed weapon projectiles generate explosions when they expire. Explosions damage is
        // affected by Curse." Overview: "Damage increases linearly where every +100% Curse provides around 13.5 damage
        // starting at 10 damage for +0% Curse." The page's own examples (+200% -> 40, +900% -> 140) do not fit one line
        // exactly; the stated rule is used. No radius is given (BlastRadius); the page names no Might scaling, so none.
        public const float TwilightBaseDamage = 10f;
        public const float TwilightDamagePerCurse = 13.5f;

        public static float TwilightDamage(float curse) => TwilightBaseDamage + TwilightDamagePerCurse * Mathf.Max(0f, curse - 1f);

        /// <summary>Arms (or disarms) a spawned shot for Twilight Requiem's expiry blast.</summary>
        public static void ArmShot(AutoAttackWeapon weapon, GameObject shot)
        {
            if (shot == null || !Has(ArcanaData.ArcanaType.TwilightRequiem)) return;
            bool listed = weapon != null && !weapon.IsHeldByEnemy && ArcanaCatalog.Lists(ArcanaData.ArcanaType.TwilightRequiem, Identify(weapon));
            var hook = shot.GetComponent<ArcanaShotHook>();
            if (!listed) { if (hook != null) hook.Weapon = null; return; }
            if (hook == null) hook = shot.AddComponent<ArcanaShotHook>();
            hook.Weapon = weapon;
        }

        public static void OnShotExpired(AutoAttackWeapon weapon, Vector3 at)
        {
            if (!Has(ArcanaData.ArcanaType.TwilightRequiem) || weapon == null || weapon.IsHeldByEnemy) return;
            var stats = HeroStats();
            Blast(at, BlastRadius * Area(stats), TwilightDamage(stats != null ? stats.Curse : 1f), Of(ArcanaData.ArcanaType.TwilightRequiem), DamageType.Magic, false);
        }

        // ================================================================== III Tragic Princess -> "Restless Pilgrim"
        // wiki Tragic_Princess_(III): "The cooldown of the listed weapons reduces when moving." "The faster the player moves,
        // the greater the Cooldown reduction." "acts independently of the Cooldown stat". The page gives no rate. Balance
        // method: while moving, the cooldown timer runs 50% faster at base Move Speed, in proportion to speed (at +30% Move
        // Speed, 65% faster). It shortens a held cycle to two thirds, about what the Cooldown stat's -40% cap would.
        public const float TragicPrincessBoost = 0.5f;

        public static float CooldownTickRate(AutoAttackWeapon weapon)
        {
            var manager = Instance;
            if (manager == null || weapon == null || weapon.IsHeldByEnemy || !manager.HasArcana(ArcanaData.ArcanaType.TragicPrincess)) return 1f;
            if (!ArcanaCatalog.Lists(ArcanaData.ArcanaType.TragicPrincess, Identify(weapon))) return 1f;
            var player = PlayerController.Instance;
            if (player == null || player.CurrentMoveInput.sqrMagnitude < 0.01f) return 1f;
            return TragicPrincessRate(player.MoveSpeed);
        }

        public static float TragicPrincessRate(float moveSpeed) => 1f + TragicPrincessBoost * Mathf.Max(0f, moveSpeed) / BaseMoveSpeed;

        // ================================================================== V / XVII / XVIII: the swaying stats
        // wiki Chaos_in_the_Dark_Night_(V): "Overall projectile Speed continuously changes between -50% and +50% over 10
        // seconds. The character starts gaining +1% projectile Speed every level." "a multiplier on top of everything else
        // ranging from 0.5 to 1.5". Lost_&_Found_Painting_(XVII): the same for Duration (-50%..+50%, +1% Duration per level).
        // Boogaloo_of_Illusions_(XVIII): Area -25%..+25%, +1% Area per level. "over 10 seconds": from one end to the other
        // takes 10 s, so one full swing is 20 s (Chaos's Peachone note: "every 10 seconds the circular zone slows down,
        // stops, changes direction"). A smooth (sine) swing; the wiki does not say the curve.
        public const float SwayHalfPeriod = 10f;
        public const float ChaosSway = 0.5f, PaintingSway = 0.5f, BoogalooSway = 0.25f;
        public const float SwayPerLevel = 0.01f;

        public static float Sway(float amplitude, float seconds) =>
            1f + amplitude * Mathf.Sin(Mathf.PI * seconds / SwayHalfPeriod);

        // ================================================================== VI Sarabande of Healing -> "Mending Wrath"
        // wiki Sarabande_of_Healing_(VI): "Healing is doubled. Recovering HP damages nearby enemies for the same amount."
        // "The damage from the healing pulse does not benefit from Might, and can only deal exactly the amount healed. It does
        // not require missing health to deal damage either." "Healing from Reviving is also affected." Tips: "The healing
        // pulse is affected by Area." Infobox weapon: "area = 4", "cooldown = 0.5 seconds". Heals in one 0.5 s cooldown are
        // pulsed together (so Recovery's ticks are not lost). The page's "area 4" has no world unit; balance method: 3 units
        // x Area, a little more than Garlic's 2.5, so the pulse reaches the enemies around the hero.
        public const float SarabandeHealFactor = 2f;
        public const float SarabandeCooldown = 0.5f;
        public const float SarabandeRadius = 3f;

        // ================================================================== VIII Mad Groove -> "Gathering Call"
        // wiki Mad_Groove_(VIII): "upon acquiring the Arcana and at every even minute on the in-game timer afterwards, all
        // stage items, Treasure Chests, pickups (including Experience Gems), and a maximum count of light sources are
        // immediately moved into a circle around the player." "Mad Groove does not attract relics." The timer is the stage
        // clock (one VS run is one stage). The page gives neither the circle's size nor the light-source maximum; balance
        // method: a 3-unit ring (inside the 4-unit base Magnet, so gems and pickups come in at once) and every lantern on the
        // stage (the stage itself caps lanterns at 10-20).
        public const float MadGrooveIntervalSeconds = 120f;
        public const float MadGrooveRing = 3f;

        // ================================================================== IX Divine Bloodline -> "Ironblood Oath"
        // wiki Divine_Bloodline_(IX): "The following weapons now deal +5 flat damage per Armor, up to +250 flat damage" (Cross,
        // King Bible, Garlic, Santa Water, Lightning Ring, Song of Mana ...); "All weapons gain +0.1 flat damage per missing HP
        // below the character's Max Health, uncapped"; "Enemies that touch the character take 10 flat damage per Armor";
        // "Killing any enemy with retaliatory damage gives the character +0.5 Max Health" (this Arcana's touch, NO FUTURE,
        // Crimson Shroud ...). Infobox: "cooldown = 0.5 seconds". "Touch" has no distance in world units; balance method:
        // 1.2 units from the hero's centre, the hero's body plus an enemy's.
        public const float DivineDamagePerArmor = 5f, DivineMaxArmorDamage = 250f, DivineDamagePerMissingHealth = 0.1f;
        public const float DivineTouchDamagePerArmor = 10f, DivineTouchCooldown = 0.5f, DivineTouchRadius = 1.2f;
        public const float DivineMaxHealthPerKill = 0.5f;

        /// <summary>Flat damage the player's Arcanas add to one hit of <paramref name="weapon"/> (before Might).</summary>
        public static float FlatDamageFor(AutoAttackWeapon weapon)
        {
            var manager = Instance;
            if (manager == null || weapon == null || weapon.IsHeldByEnemy || !manager.HasArcana(ArcanaData.ArcanaType.DivineBloodline)) return 0f;
            var player = PlayerController.Instance;
            var health = player != null ? player.GetComponent<HealthController>() : null;
            int armor = player != null && player.Stats != null ? player.Stats.Armor : 0;
            int missing = health != null ? Mathf.Max(0, health.MaxHealth - health.CurrentHealth) : 0;
            return DivineFlatDamage(ArcanaCatalog.Lists(ArcanaData.ArcanaType.DivineBloodline, Identify(weapon)), armor, missing);
        }

        public static float DivineFlatDamage(bool listedWeapon, int armor, int missingHealth) =>
            (listedWeapon ? Mathf.Min(DivineMaxArmorDamage, DivineDamagePerArmor * Mathf.Max(0, armor)) : 0f)
            + DivineDamagePerMissingHealth * Mathf.Max(0, missingHealth);

        // ================================================================== X Beginning -> "First Blade"
        // wiki Beginning_(X): "the character's starting weapon gains +3 Amount. Additionally, the following weapons, if they
        // are not the starting weapon, gain +1 Amount: Bone, Carrello, Celestial Dusting, Cherry Bomb, La Robba". Infobox:
        // "its evolutions or unions" count as the starting weapon.
        public const int BeginningMainAmount = 3, BeginningListedAmount = 1;

        public static int AmountBonusFor(AutoAttackWeapon weapon)
        {
            var manager = Instance;
            if (manager == null || weapon == null || weapon.IsHeldByEnemy || !manager.HasArcana(ArcanaData.ArcanaType.Beginning)) return 0;
            var id = Identify(weapon);
            var weapons = WeaponManager.Instance;
            if (weapons != null && BaseTypeOf(id) == weapons.StartingWeapon) return BeginningMainAmount;
            return ArcanaCatalog.Lists(ArcanaData.ArcanaType.Beginning, id) ? BeginningListedAmount : 0;
        }

        // ================================================================== XII Out of Bounds -> "Shattering Frost"
        // wiki Out_of_Bounds_(XII): "Freezing enemies generates explosions." "The explosions have a Base Damage of 10. They
        // deal an increasing amount of damage based on time survived in a run with the formula (SecondsSurvived / 30)".
        // "Out of Bounds (XII) doubles the spawn weight of Orologions". Infobox weapon: "damage = 10", "Ignores Duration and
        // Speed" (so Might and Area apply). Time survived is the stage clock: a VS run is one 30-minute stage, and a whole
        // 10-stage Soul Hunter run would push the blast far past anything VS reaches (balance method). Sorbetto: Soul
        // Hunter has no Sorbetto pickup, not built.
        public const float OutOfBoundsBaseDamage = 10f, OutOfBoundsSecondsPerDamage = 30f, OutOfBoundsOrologionWeight = 2f;

        public static float OutOfBoundsDamage(float stageSeconds) => OutOfBoundsBaseDamage + Mathf.Max(0f, stageSeconds) / OutOfBoundsSecondsPerDamage;

        public static void OnEnemyFrozen(EnemyController enemy)
        {
            if (enemy == null || !Has(ArcanaData.ArcanaType.OutOfBounds)) return;
            var stats = HeroStats();
            float seconds = LevelProgressionManager.Instance != null ? LevelProgressionManager.Instance.StageElapsedTime : 0f;
            Blast(enemy.transform.position, BlastRadius * Area(stats), OutOfBoundsDamage(seconds) * Might(stats),
                Of(ArcanaData.ArcanaType.OutOfBounds), DamageType.Magic, false);
        }

        // ================================================================== XIII Wicked Season -> "Changing Fortunes"
        // wiki Wicked_Season_(XIII): "Overall Growth, Luck, Greed, and Curse are doubled at fixed intervals. The character
        // starts gaining +1% Growth, Luck, Greed, and Curse every 2 levels." Overview: "temporarily doubled, changing every 10
        // seconds in that order". "Stacks multiplicatively" (a factor of 2 on the whole stat).
        public const float WickedSeasonSeconds = 10f, WickedSeasonFactor = 2f, WickedSeasonPerTwoLevels = 0.01f;

        /// <summary>Which stat Wicked Season doubles at <paramref name="seconds"/>: 0 Growth, 1 Luck, 2 Greed, 3 Curse.</summary>
        public static int WickedSeasonPhase(float seconds) => Mathf.FloorToInt(Mathf.Max(0f, seconds) / WickedSeasonSeconds) % 4;

        // ================================================================== XIV Jail of Crystal -> "Icebound Snare"
        // wiki Jail_of_Crystal_(XIV): "All these weapons have a 25% chance to freeze enemies, multiplied by Luck. The freeze
        // duration is based on the weapon's individual Duration." "The freeze from the listed weapons cannot freeze The
        // Reaper." Magic Wand and Holy Wand have no Duration of their own; balance method: Clock Lancet's level-1 freeze
        // (2 s, VS Clock Lancet) times the Duration stat, with Clock Lancet's freeze chance against freeze resistance (so
        // stage bosses, resistance 1.1, are not frozen, as The Reaper is not).
        public const float JailFreezeChance = 0.25f, JailFreezeSeconds = 2f;

        public static bool JailFreezes(float luck, float roll01) => roll01 < Mathf.Clamp01(JailFreezeChance * Mathf.Max(0f, luck));

        // ================================================================== XV Disco of Gold -> "Midas Pulse"
        // wiki Disco_of_Gold_(XV): "collecting any amount of gold will heal the player by the same amount, and scales up
        // with Greed. Additionally, collecting Coin Bags and Rich Coin Bags will trigger Gold Fever." Affected sources include
        // Treasure Chests and Big Coin Bags. A bag picked up while a fever runs only extends it (GoldFever's own rule).
        public static void OnGoldObtained(int amount)
        {
            if (amount <= 0 || !Has(ArcanaData.ArcanaType.DiscoOfGold)) return;
            var player = PlayerController.Instance;
            var health = player != null ? player.GetComponent<HealthController>() : null;
            if (health != null) health.Heal(amount);
        }

        public static void OnCoinPickedUp(int baseValue, GameObject player)
        {
            if (player == null || baseValue < LightSourceDrops.CoinBagValue || !Has(ArcanaData.ArcanaType.DiscoOfGold) || GoldFever.Running) return;
            GoldFever.Begin(player);
        }

        // ================================================================== XIX Heart of Fire -> "Burning Core"
        // wiki Heart_of_Fire_(XIX): "the projectiles from the following weapons create an explosion on impact, dealing 50% of
        // base damage" (Fire Wand, Hellfire, Phiera Der Tuphello ...). "The damage caused by these explosions is counted as
        // retaliatory damage, allowing Armor to increase the damage of the explosions by +10% per point". "causes light
        // sources to explode when destroyed and summons a retaliatory explosion when the player is damaged ... can retaliate
        // every 0.5 seconds." "If none of the listed weapons are present, the explosion from destroying light sources and
        // the retaliatory explosion deals the default base damage of 10. These explosions are affected by Might and Area".
        // "The base damage ... will increase by 5 for every listed weapon held, starting from the second weapon." "the drop
        // weight of Nduja Fritta Tantos will be multiplied by 4".
        public const float HeartImpactShare = 0.5f, HeartBaseDamage = 10f, HeartDamagePerExtraWeapon = 5f;
        public const float HeartRetaliationCooldown = 0.5f, HeartArmorBonus = 0.1f, HeartNdujaWeight = 4f;

        public static float HeartBlastBase(int listedWeaponsHeld) => HeartBaseDamage + HeartDamagePerExtraWeapon * Mathf.Max(0, listedWeaponsHeld - 1);
        public static float RetaliationFactor(int armor) => 1f + HeartArmorBonus * Mathf.Clamp(armor, 0, global::SoulHunter.Gameplay.Player.PlayerStats.MaxArmor);

        public static void OnLightSourceBroken(Vector3 at)
        {
            var manager = Instance;
            if (manager == null || !manager.HasArcana(ArcanaData.ArcanaType.HeartOfFire)) return;
            manager.HeartBlast(at);
        }

        private void HeartBlast(Vector3 at)
        {
            var stats = HeroStats();
            float damage = HeartBlastBase(HeartListedWeaponsHeld()) * Might(stats) * RetaliationFactor(stats != null ? stats.Armor : 0);
            Blast(at, BlastRadius * Area(stats), damage, Of(ArcanaData.ArcanaType.HeartOfFire), DamageType.Magic, true);
        }

        private static int HeartListedWeaponsHeld()
        {
            var weapons = WeaponManager.Instance;
            if (weapons == null) return 0;
            int count = 0;
            foreach (var pair in weapons.GetEquippedWeapons())
                if (pair.Value != null && ArcanaCatalog.Lists(ArcanaData.ArcanaType.HeartOfFire, Identify(pair.Value))) count++;
            return count;
        }

        public static float DropWeightFactor(LightSourceDrops.Drop drop)
        {
            var manager = Instance;
            if (manager == null) return 1f;
            if (drop == LightSourceDrops.Drop.TimeFreeze && manager.HasArcana(ArcanaData.ArcanaType.OutOfBounds)) return OutOfBoundsOrologionWeight;
            if (drop == LightSourceDrops.Drop.FireBreath && manager.HasArcana(ArcanaData.ArcanaType.HeartOfFire)) return HeartNdujaWeight;
            return 1f;
        }

        // ================================================================== XX Silent Old Sanctuary -> "Empty Hands"
        // wiki Silent_Old_Sanctuary_(XX): "Gives +3 Reroll, Skip, and Banish. Gives +20% Might and -8% Cooldown for each
        // active weapon slot left empty." Overview: "for a maximum of +100% Might and -40% Cooldown."
        public const int SanctuaryCharges = 3;
        public const float SanctuaryMightPerSlot = 0.2f, SanctuaryCooldownPerSlot = 0.08f;
        public const int SanctuaryMaxSlots = 5;

        public static int SanctuaryEmptySlots(int maxSlots, int weaponsHeld) => Mathf.Clamp(maxSlots - weaponsHeld, 0, SanctuaryMaxSlots);

        // ================================================================== XXI Blood Astronomia -> "Crimson Orbit"
        // wiki Blood_Astronomia_(XXI): "turns the player's Magnet area into a zone that damages enemies. It has a base damage
        // of 5, multiplied by Amount. It has a Cooldown of 2 seconds and no Knockback." Infobox: "Ignores Area, Duration, and
        // Speed. Scales with Magnet." The listed weapons "receive an additional Blood Weapon effect upon being activated":
        // - Garlic and Soul Eater: "zones on the ground under the character that each damages enemies individually ... The
        //   area of the zones is based on Magnet's radius divided by 32 or the calculated Area of the weapon, whichever is
        //   bigger. The amount of zone placed is based on calculated Amount. It lasts for calculated Duration."
        // - Pentagram and Gorgeous Moon: zones "around the player which can erase enemies"; "base Amount * 3 + player Amount"
        //   zones; each "can be triggered calculated Amount of times"; they "expire after calculated Cooldown plus calculated
        //   Duration".
        // - Song of Mana and Mannajja: planets "orbit in an ellipse around the character and the center point stays in
        //   place. They orbit further away with higher Magnet"; "base Amount * 3 + player Amount" planets, each hitting
        //   "calculated Amount of times", expiring after Cooldown plus Duration.
        // - Laurel: "When it regenerates a shield, it places a zone on the ground under the character. The area of the zone is
        //   based on Magnet's radius. Its damage is based on: base damage * player Amount. The zone can be triggered
        //   calculated Amount^2 of times".
        // - Clock Lancet: the page's section is empty, so it gets nothing.
        // "Amount" in "5 multiplied by Amount" is the calculated Amount (1 + the player's Amount): with the bare player
        // Amount (0 at base) the card would do nothing until Duplicator, which no page says (balance method). Laurel deals
        // no damage of its own, so its zone uses the card's base 5 (balance method). Soul Hunter's balance numbers for the
        // shapes the page leaves open: VS Magnet 30 / 32 is about 0.94 of the weapon's own size, so a Garlic zone is the
        // aura's radius times max(Magnet x 30/32, the weapon's Area); erase zones are 1.5 units x Area on a ring at 3/4 of the
        // Magnet radius; planets are 0.8 units x Area, orbiting at the Magnet radius (x0.6 across) at 90 degrees a second.
        public const float AstronomiaBaseDamage = 5f, AstronomiaCooldown = 2f;
        public const float AstronomiaMagnetToArea = 30f / 32f;
        public const float AstronomiaEraseRadius = 1.5f, AstronomiaPlanetRadius = 0.8f, AstronomiaOrbitSpeed = 90f, AstronomiaOrbitSquash = 0.6f;

        public static int AstronomiaZoneCount(int playerAmount) => 1 * 3 + Mathf.Max(0, playerAmount); // "base Amount * 3 + player Amount"
        public static float AstronomiaBaseZoneDamage(int playerAmount) => AstronomiaBaseDamage * (1 + Mathf.Max(0, playerAmount));

        /// <summary>A listed weapon fired (or Laurel grew a shield): it adds its blood zones.</summary>
        public static void OnWeaponFired(AutoAttackWeapon weapon)
        {
            var manager = Instance;
            if (manager == null || weapon == null || weapon.IsHeldByEnemy || !manager.HasArcana(ArcanaData.ArcanaType.BloodAstronomia)) return;
            var id = Identify(weapon);
            if (!ArcanaCatalog.Lists(ArcanaData.ArcanaType.BloodAstronomia, id)) return;
            var player = PlayerController.Instance;
            if (player == null) return;
            var stats = player.Stats;
            Vector3 at = player.transform.position;
            float magnet = stats != null ? stats.Magnet : 1f;
            float magnetRadius = stats != null ? stats.MagnetRadius : global::SoulHunter.Gameplay.Player.PlayerStats.BaseMagnetRadius;
            int playerAmount = stats != null ? stats.Amount : 0;
            int amount = weapon.ZoneAmount;
            float lifetime = weapon.ZoneCooldown + weapon.LimitBreakBaseDuration * weapon.ZoneDuration;
            string name = Of(ArcanaData.ArcanaType.BloodAstronomia);
            switch (id)
            {
                case ArcanaWeapon.Garlic:
                case ArcanaWeapon.SoulEater:
                {
                    var garlic = weapon as GarlicWeapon;
                    float natural = garlic != null ? garlic.HitRadius / Mathf.Max(0.1f, weapon.ZoneArea) : 2.5f;
                    float radius = natural * Mathf.Max(magnet * AstronomiaMagnetToArea, weapon.ZoneArea);
                    for (int i = 0; i < amount; i++)
                        Named(ArcanaZone.Spawn(at, radius, Mathf.RoundToInt(weapon.ZoneDamage), int.MaxValue, Mathf.Max(weapon.ZoneCooldown, lifetime), false), name);
                    break;
                }
                case ArcanaWeapon.Pentagram:
                case ArcanaWeapon.EclipseSigil:
                {
                    int zones = AstronomiaZoneCount(playerAmount);
                    for (int i = 0; i < zones; i++)
                    {
                        float angle = i * Mathf.PI * 2f / zones;
                        Vector3 spot = at + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * magnetRadius * 0.75f;
                        Named(ArcanaZone.Spawn(spot, AstronomiaEraseRadius * weapon.ZoneArea, 1, amount, lifetime, true), name);
                    }
                    break;
                }
                case ArcanaWeapon.SongOfMana:
                case ArcanaWeapon.RequiemOfSouls:
                {
                    int planets = AstronomiaZoneCount(playerAmount);
                    for (int i = 0; i < planets; i++)
                    {
                        var zone = Named(ArcanaZone.Spawn(at, AstronomiaPlanetRadius * weapon.ZoneArea, Mathf.RoundToInt(weapon.ZoneDamage), amount, lifetime, false), name);
                        zone.Orbits = true;
                        zone.Centre = at;
                        zone.OrbitX = magnetRadius;
                        zone.OrbitZ = magnetRadius * AstronomiaOrbitSquash;
                        zone.Angle = i * 360f / planets;
                        zone.AngleSpeed = AstronomiaOrbitSpeed;
                    }
                    break;
                }
                case ArcanaWeapon.Laurel:
                {
                    var laurel = weapon as LaurelWeapon;
                    float damage = AstronomiaBaseDamage * Mathf.Max(1, playerAmount) * Might(stats);
                    Named(ArcanaZone.Spawn(at, magnetRadius, Mathf.RoundToInt(damage), amount * amount,
                        laurel != null ? laurel.EffectiveRechargeCooldown : weapon.ZoneCooldown, false), name);
                    break;
                }
            }
        }

        private static ArcanaZone Named(ArcanaZone zone, string name) { zone.SourceName = name; return zone; }

        // ================================================================== identity

        /// <summary>Which Arcana-listed weapon this is (evolutions are their own entry; Gemini twins count as theirs).</summary>
        public static ArcanaWeapon Identify(AutoAttackWeapon weapon)
        {
            if (weapon == null) return ArcanaWeapon.None;
            string name = weapon.gameObject.name;
            switch (weapon)
            {
                case BloodyTearWeapon _: return ArcanaWeapon.BloodyTear;
                case WhipWeapon _: return ArcanaWeapon.Whip;
                case MagicWandWeapon _: return ArcanaWeapon.MagicWand;
                case HolyWandWeapon _: return ArcanaWeapon.HolyWand;
                case ThousandEdgeWeapon _: return ArcanaWeapon.ThousandEdge;
                case KnifeWeapon _: return ArcanaWeapon.Knife;
                case DeathSpiralWeapon _: return ArcanaWeapon.DeathSpiral;
                case AxeWeapon _: return ArcanaWeapon.Axe;
                // Heaven Sword, Unholy Vespers and Soul Eater share their base weapon's script (their own prefabs).
                case CrossWeapon _: return name.Contains("Heaven") ? ArcanaWeapon.HeavenSword : ArcanaWeapon.Cross;
                case BibleWeapon _: return name.Contains("Vespers") ? ArcanaWeapon.UnholyVespers : ArcanaWeapon.Bible;
                case GarlicWeapon _: return name.Contains("SoulEater") || name.Contains("Soul Eater") ? ArcanaWeapon.SoulEater : ArcanaWeapon.Garlic;
                case HellmawCometWeapon _: return ArcanaWeapon.HellmawComet;
                case FireWandWeapon _: return ArcanaWeapon.FireWand;
                case DrowningChaliceWeapon _: return ArcanaWeapon.DrowningChalice;
                case SantaWaterWeapon _: return ArcanaWeapon.SantaWater;
                case SeveredFateWeapon _: return ArcanaWeapon.SeveredFate;
                case RunetracerWeapon _: return ArcanaWeapon.Runetracer;
                case StormboundHaloWeapon _: return ArcanaWeapon.StormboundHalo;
                case LightningRingWeapon _: return ArcanaWeapon.LightningRing;
                case EclipseSigilWeapon _: return ArcanaWeapon.EclipseSigil;
                case PentagramWeapon _: return ArcanaWeapon.Pentagram;
                case PeachoneWeapon _: return ArcanaWeapon.Peachone;
                case BoneWeapon _: return ArcanaWeapon.Bone;
                case CherryBombWeapon _: return ArcanaWeapon.CherryBomb;
                case GunWeapon _: return ArcanaWeapon.Gun;
                case RequiemOfSoulsWeapon _: return ArcanaWeapon.RequiemOfSouls;
                case SongOfManaWeapon _: return ArcanaWeapon.SongOfMana;
                case ClockLancetWeapon _: return ArcanaWeapon.ClockLancet;
                case CrimsonShroudWeapon _: return ArcanaWeapon.CrimsonShroud;
                case LaurelWeapon _: return ArcanaWeapon.Laurel;
                case FireBreath _: return ArcanaWeapon.HellfireBreath;
            }
            return ArcanaWeapon.None;
        }

        /// <summary>The base weapon (UpgradeData type) of a weapon or its evolution; the weapon itself for base weapons.</summary>
        public static UpgradeData.UpgradeType? BaseTypeOf(ArcanaWeapon weapon) => weapon switch
        {
            ArcanaWeapon.Whip or ArcanaWeapon.BloodyTear => UpgradeData.UpgradeType.Whip,
            ArcanaWeapon.MagicWand or ArcanaWeapon.HolyWand => UpgradeData.UpgradeType.MagicWand,
            ArcanaWeapon.Knife or ArcanaWeapon.ThousandEdge => UpgradeData.UpgradeType.Knife,
            ArcanaWeapon.Axe or ArcanaWeapon.DeathSpiral => UpgradeData.UpgradeType.Axe,
            ArcanaWeapon.Cross or ArcanaWeapon.HeavenSword => UpgradeData.UpgradeType.Cross,
            ArcanaWeapon.Bible or ArcanaWeapon.UnholyVespers => UpgradeData.UpgradeType.Bible,
            ArcanaWeapon.FireWand or ArcanaWeapon.HellmawComet => UpgradeData.UpgradeType.FireWand,
            ArcanaWeapon.Garlic or ArcanaWeapon.SoulEater => UpgradeData.UpgradeType.Garlic,
            ArcanaWeapon.SantaWater or ArcanaWeapon.DrowningChalice => UpgradeData.UpgradeType.SantaWater,
            ArcanaWeapon.Runetracer or ArcanaWeapon.SeveredFate => UpgradeData.UpgradeType.Runetracer,
            ArcanaWeapon.LightningRing or ArcanaWeapon.StormboundHalo => UpgradeData.UpgradeType.LightningRing,
            ArcanaWeapon.Pentagram or ArcanaWeapon.EclipseSigil => UpgradeData.UpgradeType.Pentagram,
            ArcanaWeapon.SongOfMana or ArcanaWeapon.RequiemOfSouls => UpgradeData.UpgradeType.SongOfMana,
            ArcanaWeapon.Laurel or ArcanaWeapon.CrimsonShroud => UpgradeData.UpgradeType.Laurel,
            ArcanaWeapon.Peachone => UpgradeData.UpgradeType.Peachone,
            ArcanaWeapon.Bone => UpgradeData.UpgradeType.Bone,
            ArcanaWeapon.CherryBomb => UpgradeData.UpgradeType.CherryBomb,
            ArcanaWeapon.Gun => UpgradeData.UpgradeType.Gun,
            ArcanaWeapon.ClockLancet => UpgradeData.UpgradeType.ClockLancet,
            _ => null
        };

        // ================================================================== applying a card

        private PlayerController _hookedPlayer;
        private HealthController _hookedHealth;
        private PlayerExperience _hookedExperience;
        private bool _hitObserverSet, _retaliationObserverSet;
        private float _runSeconds, _sarabandePending, _sarabandeNextPulse, _divineNextTouch, _heartNextRetaliation, _astronomiaNext;
        private int _madGrooveLastSlot = -1;
        private readonly Dictionary<ArcanaData.ArcanaType, int> _levelsSinceTaken = new Dictionary<ArcanaData.ArcanaType, int>();
        /// <summary>Times Gathering Call (Mad Groove) has pulled the items in (tests).</summary>
        public int MadGrooveTriggers { get; private set; }
        /// <summary>Healing pulses Mending Wrath (Sarabande) has sent (tests).</summary>
        public int SarabandePulses { get; private set; }

        private static bool Has(ArcanaData.ArcanaType type) => Instance != null && Instance.HasArcana(type);

        private static string Of(ArcanaData.ArcanaType type) => ArcanaCatalog.Of(type).Name;

        private static PlayerStats HeroStats() => PlayerController.Instance != null ? PlayerController.Instance.Stats : null;
        private static float Area(PlayerStats stats) => stats != null ? Mathf.Max(0.1f, stats.Area) : 1f;
        private static float Might(PlayerStats stats) => stats != null ? stats.Might : 1f;

        /// <summary>The effects of the 18 cards added in P2-01 (the first four are in ExecuteArcanaEffect).</summary>
        private void ApplyCardEffect(ArcanaData.ArcanaType type)
        {
            _levelsSinceTaken[type] = 0;
            HookPlayer();
            switch (type)
            {
                case ArcanaData.ArcanaType.Slash:
                    // wiki Slash_(XVI): "certain weapons can deal critical damage. More importantly, critical hits from all
                    // sources deal double damage." The per-weapon chances are in each weapon (AutoAttackWeapon.CritFor).
                    AutoAttackWeapon.SlashArcanaActive = true;
                    break;
                case ArcanaData.ArcanaType.SilentOldSanctuary:
                {
                    var levelUp = FindFirstObjectByType<LevelUpManager>();
                    if (levelUp != null) levelUp.AddCharges(SanctuaryCharges, SanctuaryCharges, SanctuaryCharges);
                    break;
                }
                case ArcanaData.ArcanaType.MadGroove:
                    // "once it is selected, it will be immediately triggered, then triggered after every 2-minute interval,
                    // based on the timer"
                    PullEverythingIn();
                    _madGrooveLastSlot = StageSlot();
                    break;
            }
            UpdateObservers();
            ApplyStatFactors();
        }

        private void HookPlayer()
        {
            var player = PlayerController.Instance;
            if (player == null || player == _hookedPlayer) return;
            UnhookPlayer();
            _hookedPlayer = player;
            _hookedHealth = player.GetComponent<HealthController>();
            _hookedExperience = player.GetComponent<PlayerExperience>();
            if (_hookedHealth != null)
            {
                _hookedHealth.HealModifier = ModifyHeal;
                _hookedHealth.OnHealAttempted += OnPlayerHealAttempted;
                _hookedHealth.OnDamaged += OnPlayerDamaged;
            }
            if (_hookedExperience != null) _hookedExperience.OnLevelUp += OnPlayerLevelUp;
        }

        private void UnhookPlayer()
        {
            if (_hookedHealth != null)
            {
                if (_hookedHealth.HealModifier == (System.Func<int, int>)ModifyHeal) _hookedHealth.HealModifier = null;
                _hookedHealth.OnHealAttempted -= OnPlayerHealAttempted;
                _hookedHealth.OnDamaged -= OnPlayerDamaged;
            }
            if (_hookedExperience != null) _hookedExperience.OnLevelUp -= OnPlayerLevelUp;
            _hookedPlayer = null; _hookedHealth = null; _hookedExperience = null;
        }

        private void UpdateObservers()
        {
            bool wantHits = HasArcana(ArcanaData.ArcanaType.HeartOfFire) || HasArcana(ArcanaData.ArcanaType.JailOfCrystal);
            if (wantHits && !_hitObserverSet) { HealthController.WeaponHitObserver += OnWeaponHit; _hitObserverSet = true; }
            bool wantKills = HasArcana(ArcanaData.ArcanaType.DivineBloodline);
            if (wantKills && !_retaliationObserverSet) { HealthController.RetaliationKillObserver += OnRetaliationKill; _retaliationObserverSet = true; }
        }

        private void UnhookEffects()
        {
            UnhookPlayer();
            if (_hitObserverSet) HealthController.WeaponHitObserver -= OnWeaponHit;
            if (_retaliationObserverSet) HealthController.RetaliationKillObserver -= OnRetaliationKill;
            _hitObserverSet = _retaliationObserverSet = false;
        }

        // ------------------------------------------------------------------ every frame

        private void TickEffects(float dt)
        {
            if (ActiveArcanas.Count == 0 || dt <= 0f) return; // nothing held, or paused
            if (_hookedPlayer == null && PlayerController.Instance != null) HookPlayer();
            _runSeconds += dt;
            ApplyStatFactors();

            float now = Time.time;
            if (HasArcana(ArcanaData.ArcanaType.SarabandeOfHealing) && _sarabandePending > 0f && now >= _sarabandeNextPulse)
            {
                _sarabandeNextPulse = now + SarabandeCooldown;
                int damage = Mathf.RoundToInt(_sarabandePending);
                _sarabandePending = 0f;
                SarabandePulses++;
                if (_hookedPlayer != null && damage > 0)
                    Blast(_hookedPlayer.transform.position, SarabandeRadius * Area(HeroStats()), damage, Of(ArcanaData.ArcanaType.SarabandeOfHealing), DamageType.Holy, false, showVfx: false);
                if (_hookedPlayer != null) SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.Heal, _hookedPlayer.transform.position);
            }

            if (HasArcana(ArcanaData.ArcanaType.MadGroove))
            {
                int slot = StageSlot();
                if (slot != _madGrooveLastSlot) { _madGrooveLastSlot = slot; PullEverythingIn(); }
            }

            if (HasArcana(ArcanaData.ArcanaType.DivineBloodline) && now >= _divineNextTouch && _hookedPlayer != null)
            {
                _divineNextTouch = now + DivineTouchCooldown;
                int armor = _hookedPlayer.Stats != null ? _hookedPlayer.Stats.Armor : 0;
                if (armor > 0)
                    Blast(_hookedPlayer.transform.position, DivineTouchRadius, DivineTouchDamagePerArmor * armor, Of(ArcanaData.ArcanaType.DivineBloodline), DamageType.Magic, true, showVfx: false);
            }

            if (HasArcana(ArcanaData.ArcanaType.BloodAstronomia) && now >= _astronomiaNext && _hookedPlayer != null)
            {
                var stats = _hookedPlayer.Stats;
                _astronomiaNext = now + AstronomiaCooldown * (stats != null ? stats.Cooldown : 1f);
                float radius = stats != null ? stats.MagnetRadius : global::SoulHunter.Gameplay.Player.PlayerStats.BaseMagnetRadius;
                Blast(_hookedPlayer.transform.position, radius, AstronomiaBaseZoneDamage(stats != null ? stats.Amount : 0) * Might(stats),
                    Of(ArcanaData.ArcanaType.BloodAstronomia), DamageType.Magic, false, showVfx: false, knockback: 0f);
            }
        }

        /// <summary>Sets the player's Arcana factors and slot bonuses from the cards held and the run's time.</summary>
        private void ApplyStatFactors()
        {
            var stats = _hookedPlayer != null ? _hookedPlayer.Stats : null;
            if (stats == null) return;
            stats.ArcanaSpeedFactor = HasArcana(ArcanaData.ArcanaType.ChaosInTheDarkNight) ? Sway(ChaosSway, _runSeconds) : 1f;
            stats.ArcanaDurationFactor = HasArcana(ArcanaData.ArcanaType.LostAndFoundPainting) ? Sway(PaintingSway, _runSeconds) : 1f;
            stats.ArcanaAreaFactor = HasArcana(ArcanaData.ArcanaType.BoogalooOfIllusions) ? Sway(BoogalooSway, _runSeconds) : 1f;

            bool wicked = HasArcana(ArcanaData.ArcanaType.WickedSeason);
            int phase = WickedSeasonPhase(_runSeconds);
            stats.ArcanaGrowthFactor = wicked && phase == 0 ? WickedSeasonFactor : 1f;
            stats.ArcanaLuckFactor = wicked && phase == 1 ? WickedSeasonFactor : 1f;
            stats.ArcanaGreedFactor = wicked && phase == 2 ? WickedSeasonFactor : 1f;
            stats.ArcanaCurseFactor = wicked && phase == 3 ? WickedSeasonFactor : 1f;

            if (HasArcana(ArcanaData.ArcanaType.SilentOldSanctuary))
            {
                var weapons = WeaponManager.Instance;
                int empty = weapons != null ? SanctuaryEmptySlots(weapons.MaxWeaponSlots, weapons.GetActiveWeaponsCount()) : 0;
                stats.ArcanaMightBonus = SanctuaryMightPerSlot * empty;
                stats.ArcanaCooldownBonus = SanctuaryCooldownPerSlot * empty;
            }
        }

        // "The Arcana only starts giving stat boosts when the player selects it and does not give extra stats retroactively."
        private void OnPlayerLevelUp(int level)
        {
            var stats = _hookedPlayer != null ? _hookedPlayer.Stats : null;
            if (stats == null) return;
            if (HasArcana(ArcanaData.ArcanaType.ChaosInTheDarkNight)) stats.ArcanaSpeedBonus += SwayPerLevel;
            if (HasArcana(ArcanaData.ArcanaType.LostAndFoundPainting)) stats.ArcanaDurationBonus += SwayPerLevel;
            if (HasArcana(ArcanaData.ArcanaType.BoogalooOfIllusions)) stats.ArcanaAreaBonus += SwayPerLevel;
            if (HasArcana(ArcanaData.ArcanaType.WickedSeason))
            {
                int levels = (_levelsSinceTaken.TryGetValue(ArcanaData.ArcanaType.WickedSeason, out int n) ? n : 0) + 1;
                _levelsSinceTaken[ArcanaData.ArcanaType.WickedSeason] = levels;
                if (levels % 2 == 0)
                {
                    stats.ArcanaGrowthBonus += WickedSeasonPerTwoLevels;
                    stats.ArcanaLuckBonus += WickedSeasonPerTwoLevels;
                    stats.ArcanaGreedBonus += WickedSeasonPerTwoLevels;
                    stats.ArcanaCurseBonus += WickedSeasonPerTwoLevels;
                }
            }
        }

        private int ModifyHeal(int amount) =>
            HasArcana(ArcanaData.ArcanaType.SarabandeOfHealing) ? Mathf.RoundToInt(amount * SarabandeHealFactor) : amount;

        private void OnPlayerHealAttempted(int amount)
        {
            if (HasArcana(ArcanaData.ArcanaType.SarabandeOfHealing)) _sarabandePending += amount;
        }

        private void OnPlayerDamaged()
        {
            if (!HasArcana(ArcanaData.ArcanaType.HeartOfFire) || _hookedPlayer == null || Time.time < _heartNextRetaliation) return;
            _heartNextRetaliation = Time.time + HeartRetaliationCooldown;
            HeartBlast(_hookedPlayer.transform.position);
        }

        private void OnWeaponHit(AutoAttackWeapon weapon, EnemyController enemy, Vector3 point)
        {
            if (weapon == null || weapon.IsHeldByEnemy || enemy == null) return;
            var id = Identify(weapon);
            var stats = HeroStats();
            if (HasArcana(ArcanaData.ArcanaType.HeartOfFire) && ArcanaCatalog.Lists(ArcanaData.ArcanaType.HeartOfFire, id))
            {
                // 50% of the weapon's base damage (its hit before crits: base x Might), +10% per Armor.
                float damage = HeartImpactShare * weapon.ZoneDamage * RetaliationFactor(stats != null ? stats.Armor : 0);
                Blast(enemy.transform.position, BlastRadius * Area(stats), damage, Of(ArcanaData.ArcanaType.HeartOfFire), DamageType.Magic, true);
            }
            if (HasArcana(ArcanaData.ArcanaType.JailOfCrystal) && ArcanaCatalog.Lists(ArcanaData.ArcanaType.JailOfCrystal, id)
                && enemy.isActiveAndEnabled && JailFreezes(stats != null ? stats.Luck : 1f, Random.value))
                enemy.TryWeaponFreeze(1f, JailFreezeSeconds * (stats != null ? Mathf.Max(0.1f, stats.Duration) : 1f));
        }

        private void OnRetaliationKill(EnemyController enemy)
        {
            if (!HasArcana(ArcanaData.ArcanaType.DivineBloodline)) return;
            var player = PlayerController.Instance;
            var health = player != null ? player.GetComponent<HealthController>() : null;
            if (health != null) health.AddMaxHealthWithoutHealing(DivineMaxHealthPerKill);
        }

        // ------------------------------------------------------------------ Mad Groove

        private static int StageSlot()
        {
            float seconds = LevelProgressionManager.Instance != null ? LevelProgressionManager.Instance.StageElapsedTime : 0f;
            int stage = LevelProgressionManager.Instance != null ? LevelProgressionManager.Instance.CurrentStage : 1;
            return stage * 1000 + Mathf.FloorToInt(seconds / MadGrooveIntervalSeconds);
        }

        private static readonly List<ITickedPickup> TickedBuffer = new List<ITickedPickup>();
        private static readonly List<Transform> Pulled = new List<Transform>();

        /// <summary>Moves every gem, pickup, chest, stage item and Soul Lantern into a ring around the player.</summary>
        public void PullEverythingIn()
        {
            var player = PlayerController.Instance;
            if (player == null) return;
            Pulled.Clear();
            foreach (var gem in XPGem.ActiveGems) if (gem != null && !gem.IsShot) Pulled.Add(gem.transform);
            PickupTicker.CopyTo(TickedBuffer);
            foreach (var pickup in TickedBuffer) if (pickup is Component c && c != null) Pulled.Add(c.transform);
            foreach (var chest in FindObjectsByType<ChestPickup>(FindObjectsSortMode.None)) Pulled.Add(chest.transform);
            foreach (var item in FindObjectsByType<StageItemPickup>(FindObjectsSortMode.None)) Pulled.Add(item.transform);
            foreach (var lantern in FindObjectsByType<LightSource>(FindObjectsSortMode.None)) Pulled.Add(lantern.transform);
            Vector3 centre = player.transform.position;
            int count = 0;
            foreach (var t in Pulled)
            {
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                count++;
            }
            int i = 0;
            foreach (var t in Pulled)
            {
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                float angle = i++ * Mathf.PI * 2f / Mathf.Max(1, count);
                Vector3 spot = centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * MadGrooveRing;
                spot.y = t.position.y;
                t.position = spot;
            }
            Pulled.Clear();
            MadGrooveTriggers++;
        }

        // ------------------------------------------------------------------ helpers

        private static readonly List<HealthController> BlastTargets = new List<HealthController>();

        /// <summary>
        /// Deals <paramref name="damage"/> to every live enemy within <paramref name="radius"/> of <paramref name="at"/> on
        /// the ground plane, credited to the card <paramref name="source"/> (Results). Returns how many were hit.
        /// </summary>
        public static int Blast(Vector3 at, float radius, float damage, string source, DamageType type, bool retaliatory,
                                bool showVfx = true, float knockback = -1f)
        {
            int amount = Mathf.Max(1, Mathf.RoundToInt(damage));
            float sqr = radius * radius;
            BlastTargets.Clear();
            var enemies = EnemyController.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                Vector3 offset = enemy.transform.position - at;
                offset.y = 0f;
                if (offset.sqrMagnitude > sqr) continue;
                var health = enemy.GetComponent<HealthController>();
                if (health != null) BlastTargets.Add(health);
            }
            if (showVfx) SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.Explosion, at);
            int hit = 0;
            foreach (var health in BlastTargets)
            {
                if (health == null || health.CurrentHealth <= 0) continue;
                Vector3 push = health.transform.position - at;
                push.y = 0f;
                var packet = new DamagePacket(amount, health.transform.position, push.sqrMagnitude > 0.0001f ? push.normalized : Vector3.forward, type).WithKnockback(knockback);
                using (DamageSource.From(null, source))
                using (new HealthController.RetaliationScope(retaliatory))
                    health.TakeDamage(packet);
                hit++;
            }
            BlastTargets.Clear();
            return hit;
        }

        private static EnemyController NearestEnemy(Vector3 from)
        {
            EnemyController nearest = null;
            float best = float.MaxValue;
            var enemies = EnemyController.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                Vector3 offset = enemy.transform.position - from;
                offset.y = 0f;
                float sqr = offset.sqrMagnitude;
                if (sqr < best) { best = sqr; nearest = enemy; }
            }
            return nearest;
        }
    }
}
