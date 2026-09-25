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
        protected string HostileTag =>
            GetComponentInParent<SoulHunter.Gameplay.AI.EnemyController>() != null ? "Player" : "Enemy";

        protected float MightMultiplier => OwnerStats != null ? OwnerStats.Might : 1f;
        protected float CooldownMultiplier => OwnerStats != null ? OwnerStats.Cooldown : 1f;
        protected float AreaMultiplier => OwnerStats != null ? Mathf.Max(0.1f, OwnerStats.Area) : 1f;
        protected float DurationMultiplier => OwnerStats != null ? Mathf.Max(0.1f, OwnerStats.Duration) : 1f;
        protected float SpeedMultiplier => OwnerStats != null ? Mathf.Max(0.1f, OwnerStats.ProjectileSpeed) : 1f;
        protected int ExtraAmount => OwnerStats != null ? Mathf.Max(0, OwnerStats.Amount) : 0;
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

            if (GetComponentInParent<SoulHunter.Gameplay.AI.EnemyController>() != null)
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
        protected float ScaledDamage(float baseDamage) => baseDamage * MightMultiplier;

        /// <summary>
        /// VS rule: some weapons can land critical hits; Luck multiplies the chance. 0 = cannot crit.
        /// Provisional values; crit weapons override this.
        /// </summary>
        protected virtual float BaseCritChance => 0f;
        public const float CritDamageMultiplier = 2f;

        /// <summary>Critical hit multiplier for a holder: its character's (e.g. Kaelen 3x), else the VS default 2x.</summary>
        public static float CritMultiplierFor(PlayerStats stats) => stats != null ? stats.CritDamageMultiplier : CritDamageMultiplier;

        public static bool IsCrit(float baseChance, float luck, float roll01) =>
            baseChance > 0f && roll01 < Mathf.Clamp01(baseChance * Mathf.Max(0f, luck));

        /// <summary>Damage for one hit or projectile: Might-scaled, doubled on a critical hit.</summary>
        protected float RollDamage(float baseDamage)
        {
            float damage = ScaledDamage(baseDamage);
            float luck = OwnerStats != null ? OwnerStats.Luck : 1f;
            return IsCrit(BaseCritChance, luck, Random.value) ? damage * CritMultiplierFor(OwnerStats) : damage;
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
                ShowAttackOnHolder(); // before Attack(): the shot may kill the enemy it aimed at
                Attack();
                var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
                if (audio != null && FireSound != SoulHunter.Gameplay.Audio.WeaponSound.None) audio.PlayWeaponSound(FireSound);
                _timer = Mathf.Max(0.05f, AttackCooldown * CooldownMultiplier);
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
                _damageCaster.CastDamage();
            }
        }
    }
}
