using UnityEngine;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors jaisa auto-attack system.
    /// Ye script ek timer maintain karti hai aur har X seconds ke baad DamageCaster ko trigger karti hai.
    /// Player par attach karne se player ke aas-paas automatically attack hoga.
    /// VS rule: player ke passive stats (Might, Cooldown, Area, Amount, Duration, Speed) har weapon par lagte hain;
    /// subclasses neeche wale helpers se apne base values scale karti hain.
    /// </summary>
    public class AutoAttackWeapon : MonoBehaviour
    {
        public int CurrentLevel = 1;
        public virtual int MaxLevel => int.MaxValue;
        public float DamageAmount = 10f;
        public float AttackCooldown = 1.5f;

        [System.NonSerialized] private float _timer;
        private DamageCaster _damageCaster;
        private PlayerStats _ownerStats;
        private bool _ownerStatsResolved;

        /// <summary>The owning player's stats, or null for weapons not held by the player (e.g. Shadow Kael's mirrored copies).</summary>
        protected PlayerStats OwnerStats
        {
            get
            {
                if (!_ownerStatsResolved)
                {
                    _ownerStats = GetComponentInParent<PlayerStats>();
                    _ownerStatsResolved = true;
                }
                return _ownerStats;
            }
        }

        /// <summary>
        /// Tag this weapon's damage zones must hit: enemies, unless a boss holds it (Shadow Kael's mirrored
        /// copies), which must hit the player. TouchDamage defaults to "Player" (it was written for enemy
        /// contact damage), so every weapon that adds one must set this or it hurts its own holder.
        /// </summary>
        protected string HostileTag => HeldByEnemy ? "Player" : "Enemy";

        private bool _heldByEnemy;
        private bool _heldByEnemyResolved;

        /// <summary>True when a boss holds this weapon (Shadow Kael's mirrored copy): it attacks the player, never enemies.</summary>
        protected bool HeldByEnemy
        {
            get
            {
                if (!_heldByEnemyResolved)
                {
                    _heldByEnemy = GetComponentInParent<SoulHunter.Gameplay.AI.EnemyController>() != null;
                    _heldByEnemyResolved = true;
                }
                return _heldByEnemy;
            }
        }

        private static readonly System.Collections.Generic.List<Projectile> ProjectileBuffer = new();
        private static readonly System.Collections.Generic.List<ProjectileDamage> ProjectileDamageBuffer = new();
        private static readonly System.Collections.Generic.List<TouchDamage> TouchDamageBuffer = new();

        /// <summary>
        /// Points an object this weapon spawned (projectile, thrown weapon, zone) at the holder's enemies. Pooled objects
        /// are shared by the player's weapons and Shadow Kael's mirrored copies and keep the last setting, so every spawn
        /// site calls this after setting the object up. Before this the boss's shots only ever hit enemies.
        /// </summary>
        protected void AimAtHostiles(GameObject spawned)
        {
            if (spawned == null) return;
            bool targetsPlayer = HeldByEnemy;
            string tag = HostileTag;
            spawned.GetComponentsInChildren(true, ProjectileBuffer);
            foreach (var projectile in ProjectileBuffer) projectile.TargetsPlayer = targetsPlayer;
            spawned.GetComponentsInChildren(true, ProjectileDamageBuffer);
            foreach (var damage in ProjectileDamageBuffer) damage.TargetTag = tag;
            spawned.GetComponentsInChildren(true, TouchDamageBuffer);
            foreach (var touch in TouchDamageBuffer) touch.TargetTag = tag;
            // VS: every hit carries its weapon's knockback (each weapon's wiki infobox).
            float knockback = Knockback;
            foreach (var projectile in ProjectileBuffer) projectile.Knockback = knockback;
            foreach (var damage in ProjectileDamageBuffer) damage.Knockback = knockback;
            foreach (var touch in TouchDamageBuffer) touch.Knockback = knockback;
        }

        /// <summary>
        /// VS knockback of this weapon's hits (each weapon's wiki infobox, fetched 2026-10-01): 1 for most; weapons with
        /// another value override it. Weapons whose page gives none keep 1.
        /// </summary>
        public virtual float Knockback => 1f;

        protected float MightMultiplier => OwnerStats != null ? (IgnoresMightCap ? OwnerStats.RawMight : OwnerStats.Might) : 1f;
        /// <summary>VS (wiki Player_stats): the 1000% Might cap "does not affect Soul Eater".</summary>
        protected virtual bool IgnoresMightCap => false;
        protected float CooldownMultiplier => OwnerStats != null ? OwnerStats.Cooldown : 1f;
        // VS Limit Break (LimitBreak): this weapon's own bonuses on top of the player's stats.
        protected float AreaMultiplier => (OwnerStats != null ? Mathf.Max(0.1f, OwnerStats.Area) : 1f) * (1f + LimitBreak.AreaBonus(this));
        protected float DurationMultiplier => (OwnerStats != null ? Mathf.Max(0.1f, OwnerStats.Duration) : 1f) * LimitBreakDurationFactor;
        protected float SpeedMultiplier => (OwnerStats != null ? Mathf.Max(0.1f, OwnerStats.ProjectileSpeed) : 1f)
                                           * (OwnerStats != null ? SoulHunter.Gameplay.Data.RunModes.ProjectileSpeedMultiplier : 1f)
                                           * (1f + LimitBreak.SpeedBonus(this));
        protected int ExtraAmount => (OwnerStats != null ? Mathf.Max(0, OwnerStats.Amount) : 0) + LimitBreak.AmountBonus(this);
        /// <summary>Limit Break pierce added to the weapon's own pierce (weapons with <see cref="UsesPierce"/>).</summary>
        protected int ExtraPierce => LimitBreak.PierceBonus(this);

        /// <summary>The weapon's base duration in seconds, for Limit Break's +0.1 s (0 = the weapon has no duration).</summary>
        public virtual float LimitBreakBaseDuration => 0f;
        /// <summary>The weapon has a pierce count that Limit Break can raise.</summary>
        public virtual bool UsesPierce => false;
        /// <summary>The weapon has a chance-based effect that Limit Break can raise (Cherry Bomb, Pentagram).</summary>
        public virtual bool HasChance => false;
        /// <summary>The weapon can land critical hits now (its own chance, or Slash's).</summary>
        public bool CanCrit => BaseCritChance > 0f || (SlashArcanaActive && SlashCritChance > 0f);

        private float LimitBreakDurationFactor
        {
            get
            {
                float extra = LimitBreak.DurationSeconds(this);
                float baseDuration = LimitBreakBaseDuration;
                return extra > 0f && baseDuration > 0f ? (baseDuration + extra) / baseDuration : 1f;
            }
        }

        /// <summary>
        /// VS rule (each weapon's wiki infobox "interval"): a weapon fires its Amount of projectiles one after another,
        /// <paramref name="interval"/> seconds apart, so every extra shot from Amount (Duplicator, Soul Eggs, PowerUps) is
        /// seen on its own. Shot 0 fires now; the rest follow on game time (a pause holds them), and stop if the weapon
        /// is switched off.
        /// </summary>
        protected void FireInSuccession(int count, float interval, System.Action<int> fireOne)
        {
            if (count <= 0) return;
            if (!Application.isPlaying)
            {
                // Edit Mode (tests) has no game time to wait on: every shot leaves at once.
                for (int i = 0; i < count; i++) fireOne(i);
                return;
            }
            fireOne(0);
            if (count > 1 && isActiveAndEnabled) StartCoroutine(FireTheRest(count, interval, fireOne));
        }

        private System.Collections.IEnumerator FireTheRest(int count, float interval, System.Action<int> fireOne)
        {
            for (int i = 1; i < count; i++)
            {
                yield return new WaitForSeconds(interval);
                if (!isActiveAndEnabled) yield break;
                fireOne(i);
            }
        }
        /// <summary>Extra projectile bounces from the holder's character (e.g. Seraphina).</summary>
        protected int ExtraBounces => OwnerStats != null ? Mathf.Max(0, OwnerStats.ExtraProjectileBounces) : 0;

        /// <summary>
        /// Nearest live enemy on the ground plane within maxRange, or null. For a boss's mirrored copy
        /// (Shadow Kael) the "enemy" is the player, so its weapons aim at the player, not its own minions.
        /// </summary>
        protected Transform FindNearestEnemy(float maxRange = float.MaxValue)
        {
            Transform nearest = null;
            float bestSqr = maxRange * maxRange;
            Vector3 origin = transform.position;

            if (HeldByEnemy)
            {
                var player = PlayerController.Instance;
                if (player == null) return null;
                Vector3 toPlayer = player.transform.position - origin;
                toPlayer.y = 0f;
                return toPlayer.sqrMagnitude <= bestSqr ? player.transform : null;
            }

            var enemies = SoulHunter.Gameplay.AI.EnemyController.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.isActiveAndEnabled) continue;
                Vector3 offset = enemy.transform.position - origin;
                offset.y = 0f;
                float sqr = offset.sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; nearest = enemy.transform; }
            }
            return nearest;
        }

        /// <summary>Flat (ground-plane) unit direction from this weapon to target; forward if on top of it.</summary>
        protected Vector3 FlatDirectionTo(Transform target)
        {
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        /// <summary>
        /// Owner decision: attacks fire toward the nearest enemy. Returns the flat direction to it, or
        /// <paramref name="fallback"/> (flattened) when there is no enemy, so weapons keep their old behaviour then.
        /// </summary>
        protected Vector3 AimDirection(Vector3 fallback, float maxRange = float.MaxValue)
        {
            Transform target = FindNearestEnemy(maxRange);
            if (target != null) return FlatDirectionTo(target);
            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
        }

        /// <summary>
        /// Where the holder faces, for "facing side when no enemy" fallbacks. The player used to mirror itself with
        /// localScale.x = +-1 (read by several weapons); it now turns its model, so this reads its facing direction.
        /// </summary>
        protected Vector3 HolderFacing
        {
            get
            {
                var player = GetComponentInParent<PlayerController>();
                return player != null ? player.FacingDirection : Vector3.right;
            }
        }

        /// <summary>A random flat unit direction, for weapons whose no-enemy fallback is random.</summary>
        protected static Vector3 RandomFlatDirection()
        {
            Vector2 random = Random.insideUnitCircle.normalized;
            return random.sqrMagnitude > 0f ? new Vector3(random.x, 0f, random.y) : Vector3.forward;
        }

        /// <summary>Base damage scaled by Might.</summary>
        protected float ScaledDamage(float baseDamage) => (baseDamage + LimitBreak.BaseDamageBonus(this)) * MightMultiplier;

        /// <summary>
        /// VS (vampire.survivors.wiki/w/Critical_Hit, fetched 2026-10-01): "Not all weapons are capable of Critical Hits."
        /// Each weapon has its own chance and multiplier; Luck multiplies the chance. Without the Slash Arcana only Bloody
        /// Tear (10%, 2x) and Heaven Sword (10%, 2.5x) can crit here; Whip, Knife and Axe crit only with Slash.
        /// </summary>
        protected virtual float BaseCritChance => 0f;
        /// <summary>The weapon's own critical hit multiplier (VS: 2x for most).</summary>
        protected virtual float WeaponCritMultiplier => CritDamageMultiplier;
        /// <summary>VS Slash (XVI): the chance it gives a weapon that cannot crit by itself (0 = none).</summary>
        protected virtual float SlashCritChance => 0f;
        /// <summary>The multiplier of a Slash-given crit, before Slash doubles it.</summary>
        protected virtual float SlashCritMultiplier => CritDamageMultiplier;
        public const float CritDamageMultiplier = 2f;

        /// <summary>
        /// VS Slash (XVI) Arcana: "doubles Critical Hit Multipliers and provides Critical Hit Chance to select weapons".
        /// Soul Hunter has no Slash Arcana yet (Phase 2 Arcanas); this switch is for it.
        /// </summary>
        public static bool SlashArcanaActive { get; set; }

        /// <summary>
        /// The holder's character crit bonus as a factor over the VS default 2x (Kaelen's 3x gives 1.5). Soul Hunter's own
        /// character ability; VS characters have none.
        /// </summary>
        public static float CharacterCritFactor(PlayerStats stats) => stats != null ? stats.CritDamageMultiplier / CritDamageMultiplier : 1f;

        /// <summary>Critical hit multiplier for a holder with the VS default weapon multiplier (2x × character factor).</summary>
        public static float CritMultiplierFor(PlayerStats stats) => CritDamageMultiplier * CharacterCritFactor(stats);

        /// <summary>The crit chance and multiplier this weapon uses now (Slash adds a chance to weapons with none and doubles).</summary>
        public static void CritFor(float baseChance, float baseMultiplier, float slashChance, float slashMultiplier, bool slash, out float chance, out float multiplier)
        {
            // wiki: "Crit Chance provided by Slash is overrode when a weapon is evolved into one that gives itself Crit Chance".
            if (baseChance > 0f) { chance = baseChance; multiplier = baseMultiplier; }
            else if (slash && slashChance > 0f) { chance = slashChance; multiplier = slashMultiplier; }
            else { chance = 0f; multiplier = 1f; return; }
            if (slash) multiplier *= 2f;
        }

        public static bool IsCrit(float baseChance, float luck, float roll01) =>
            baseChance > 0f && roll01 < Mathf.Clamp01(baseChance * Mathf.Max(0f, luck));

        /// <summary>Damage for one hit or projectile: Might-scaled, doubled on a critical hit.</summary>
        protected float RollDamage(float baseDamage)
        {
            float damage = ScaledDamage(baseDamage);
            float luck = OwnerStats != null ? OwnerStats.Luck : 1f;
            CritFor(BaseCritChance, WeaponCritMultiplier, SlashCritChance, SlashCritMultiplier, SlashArcanaActive, out float chance, out float multiplier);
            if (chance > 0f) chance += LimitBreak.CritChanceBonus(this); // VS Limit Break: Crit Chance +2.5% per card
            return IsCrit(chance, luck, Random.value) ? damage * multiplier * CharacterCritFactor(OwnerStats) : damage;
        }

        /// <summary>Scales a spawned effect by Area, relative to its prefab's own scale (safe for pooled objects).</summary>
        protected void ApplyArea(GameObject instance, GameObject prefab)
        {
            if (instance == null || prefab == null) return;
            instance.transform.localScale = prefab.transform.localScale * AreaMultiplier;
        }

        protected virtual void Awake()
        {
            _damageCaster = GetComponent<DamageCaster>();
        }

        protected virtual void Update()
        {
            _timer -= Time.deltaTime;

            if (_timer <= 0f)
            {
                // The cooldown is set first: a weapon whose attack throws must not retry every frame (Death Spiral
                // threw 30,000 times in one 30-minute run and dragged the frame rate from 80 to 20).
                _timer = Mathf.Max(MinimumCooldown, AttackCooldown * CooldownMultiplier);
                ShowAttackOnHolder(); // before Attack(): the shot may kill the enemy it aimed at
                Attack();
                var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
                if (audio != null && FireSound != SoulHunter.Gameplay.Audio.WeaponSound.None) audio.PlayWeaponSound(FireSound);
            }
        }

        /// <summary>
        /// Weapons the player visibly swings, throws or casts toward enemies. Constant auras and orbits
        /// (Garlic, Bible, Laurel, Pentagram, Peachone) do not play the attack pose or turn the player.
        /// </summary>
        protected virtual bool ShowsAttackPose => FireSound != SoulHunter.Gameplay.Audio.WeaponSound.None;

        private SoulHunter.Gameplay.Player.PlayerController _holder;
        private bool _holderResolved;

        /// <summary>The player turns to the nearest enemy and plays the attack pose when this weapon fires.</summary>
        private void ShowAttackOnHolder()
        {
            if (!ShowsAttackPose) return;
            if (!_holderResolved)
            {
                _holder = GetComponentInParent<SoulHunter.Gameplay.Player.PlayerController>();
                _holderResolved = true;
            }
            if (_holder == null) return;
            var target = FindNearestEnemy();
            _holder.ShowWeaponAttack(target != null ? FlatDirectionTo(target) : Vector3.zero);
        }

        /// <summary>
        /// VS (wiki Weapons): "Upgrading a weapon will also reset its Cooldown and activate it instantly." Called by
        /// WeaponManager when an owned weapon gains a level (level-up, chest or stage item). Weapons with their own timer
        /// override it.
        /// </summary>
        public virtual void ResetCooldownAndFire() { _timer = 0f; }

        /// <summary>Sets when the next attack comes, for weapons whose cycle is longer than their cooldown (VS duration).</summary>
        protected void SetNextAttackIn(float seconds) { _timer = Mathf.Max(MinimumCooldown, seconds); }

        /// <summary>The shortest cooldown this weapon can reach whatever the Cooldown stat (VS Pentagram: 15 s).</summary>
        protected virtual float MinimumCooldown => 0.05f;

        public virtual void LevelUp()
        {
            CurrentLevel++;
            DamageAmount += 2f;
        }

        /// <summary>Sound played each time this weapon attacks (VS: every weapon is heard). None for constant auras.</summary>
        protected virtual SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Shoot;

        protected virtual void Attack()
        {
            if (_damageCaster != null)
            {
                _damageCaster.Knockback = Knockback;
                _damageCaster.CastDamage();
            }
        }
    }
}
