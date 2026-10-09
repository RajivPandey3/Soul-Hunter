using System;
using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// Universal Health component. Implements IDamageable so anything with this script can take damage.
    /// Fires C# events for UI or death logic, keeping dependencies clean.
    /// </summary>
    public class HealthController : MonoBehaviour, IDamageable
    {
        [Tooltip("Asli Vampire Survivors mein early dushmano ki HP bahut kam (10-15) hoti hai taaki wo 1 hit mein marein")]
        [SerializeField] private int _maxHealth = 100;
        private int _currentHealth;
        private float _preciseMaxHealth;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;

        public event Action<int, int> OnHealthChanged;
        public event Action OnDied;
        public event Action OnDamaged; // For Hit Flash VFX
        /// <summary>
        /// Health actually restored (overheal excluded), from <see cref="Heal"/> and from a revival. VS (vampire.survivors.wiki/w/
        /// Revival): "The health gained upon reviving counts as healing"; Soul_Eater: "only healing that actually takes place
        /// is counted, overhealing does not."
        /// </summary>
        public event Action<int> OnHealed;
        public Func<DamagePacket, bool> DamageInterceptor { get; set; }
        public Func<int, int> DamageModifier { get; set; }
        /// <summary>Flat damage reduction per hit. The player's PlayerStats keeps this in sync.</summary>
        public int Armor { get; set; }
        /// <summary>
        /// Called on lethal damage before OnDied. Returning true means the owner revived (it called ReviveFromDeath) or
        /// holds the death for the Revive screen (<see cref="HoldForRevive"/>), so OnDied does not fire: a revival is not
        /// a death (no stage clear, drops or kill count for a boss; no Game Over for the player).
        /// </summary>
        public Func<bool> TryPreventDeath { get; set; }

        /// <summary>
        /// The player died with a Revival left and waits at 0 health for the Revive button (VS Revival: "they will instead
        /// be greeted with a 'Revive' button"). Nothing hurts or heals it until <see cref="ReviveFromDeath"/> or
        /// <see cref="FinishDeath"/>.
        /// </summary>
        public bool IsAwaitingRevive { get; private set; }

        /// <summary>
        /// Seconds of immunity after a revival. VS Revival: "The player is additionally given a large amount of
        /// invulnerability frames"; the wiki gives no length. 1.5 s is Soul Hunter's number (open owner question, P1-16).
        /// The VS 240 ms (<see cref="VsInvulnerabilityTime"/>) is added on top: Invulnerability_Time "adds a flat amount
        /// of time ... to another source of immunity the player receives".
        /// </summary>
        public const float RevivalInvulnerabilitySeconds = 1.5f;
        private float _revivalInvulnerableUntil = -1f;
        /// <summary>The revival's immunity is running (game time: a pause does not use it up, and the dash cannot end it).</summary>
        public bool IsRevivalInvulnerable => Time.time < _revivalInvulnerableUntil;
        /// <summary>Ends the revival's immunity now (tests and debug tools).</summary>
        public void EndRevivalInvulnerability() => _revivalInvulnerableUntil = -1f;

        public bool IsInvincible { get; set; } = false;

        /// <summary>
        /// Seconds of immunity after taking a hit (VS-style). 0 = none; enemies keep 0 so rapid
        /// hits land, and the player sets it. Separate from IsInvincible, which dash/revive toggle.
        /// </summary>
        public float PostHitInvulnerability { get; set; }

        /// <summary>
        /// VS Invulnerability Time (vampire.survivors.wiki/w/Invulnerability_Time): "At base, the player starts with 240
        /// milliseconds", added to every source of immunity: being hit, a Floor Chicken, a treasure chest. Soul Hunter has
        /// no Parm Aegis, so it stays at the base. The chicken and chest have no length of their own on the wiki; the owner
        /// chose the 240 ms alone (2026-10-01).
        /// </summary>
        public const float VsInvulnerabilityTime = 0.24f;
        private float _hitInvulnerableUntil = -1f;
        public bool IsHitInvulnerable => Time.time < _hitInvulnerableUntil;

        /// <summary>Invulnerable for <paramref name="seconds"/> of game time from now (keeps a longer one already running).</summary>
        public void GrantInvulnerability(float seconds)
        {
            if (seconds > 0f) _hitInvulnerableUntil = Mathf.Max(_hitInvulnerableUntil, Time.time + seconds);
        }
        /// <summary>Time (Time.time) until which every hit is blocked: a character's starting shield.</summary>
        public float ShieldedUntil { get; set; } = -1f;
        public bool IsShielded => Time.time < ShieldedUntil;
        private bool _keepAliveAfterDeath;

        private void Awake()
        {
            ResetHealth();
        }

        public void ResetHealth()
        {
            if (_preciseMaxHealth <= 0f) _preciseMaxHealth = _maxHealth;
            _currentHealth = _maxHealth;
            _hitInvulnerableUntil = -1f;
            _revivalInvulnerableUntil = -1f;
            IsAwaitingRevive = false;
        }

        public void Initialize(int maxHealth)
        {
            _maxHealth = maxHealth;
            _preciseMaxHealth = maxHealth;
            ResetHealth();
        }

        [Tooltip("0 = Full Knockback, 1 = Immune (Bosses)")]
        [Range(0f, 1f)]
        public float KnockbackResistance = 0f;

        /// <summary>Called for every weapon hit an enemy takes (ArcanaManager listens while the player holds Arcanas).</summary>
        public static Action<AutoAttackWeapon, SoulHunter.Gameplay.AI.EnemyController, Vector3> WeaponHitObserver;

        /// <summary>Above 0 while retaliatory damage is dealt (VS "Retaliate": NO FUTURE, Crimson Shroud, Arcana blasts).</summary>
        public static int RetaliationDepth;
        /// <summary>An enemy slain by retaliatory damage (VS Divine Bloodline (IX): "+0.5 Max Health").</summary>
        public static Action<SoulHunter.Gameplay.AI.EnemyController> RetaliationKillObserver;

        /// <summary>Marks the damage dealt inside the scope as retaliation; dispose it right after the hit.</summary>
        public static RetaliationScope Retaliation() => new RetaliationScope(true);

        public readonly struct RetaliationScope : IDisposable
        {
            private readonly bool _open;
            public RetaliationScope(bool open) { _open = open; if (open) RetaliationDepth++; }
            public void Dispose() { if (_open) RetaliationDepth = Mathf.Max(0, RetaliationDepth - 1); }
        }

        public void TakeDamage(DamagePacket packet)
        {
            if (_currentHealth <= 0 || IsInvincible) return; // Already dead or invincible
            if (IsRevivalInvulnerable) return; // the revival's immunity blocks every hit
            if (IsShielded) return; // starting shield blocks every hit
            if (IsHitInvulnerable && !packet.IgnoresHitInvulnerability) return; // brief immunity after the last hit
            if (packet.Amount <= 0 || (DamageInterceptor != null && DamageInterceptor(packet))) return;

            int incomingDamage = packet.Amount;
            var enemy = GetComponentInParent<SoulHunter.Gameplay.AI.EnemyController>();
            if (enemy != null)
            {
                // Learning Comment:
                // Authoritative Level 7 Spectral Damage Whitelist (AGENTS.md Section 5 & Docs/Requirements/P1-07-reconciliation.md Section 3.2):
                // Spectral/ghost enemies are intangible to physical and non-holy elemental damage.
                // Reject Normal, Fire, Poison, and Shadow damage, permitting only Holy (2x vulnerability), Magic (1x), and Spectral (1x).
                if (enemy.IsSpectral)
                {
                    bool isWhitelisted = packet.Type == DamageType.Holy ||
                                         packet.Type == DamageType.Magic ||
                                         packet.Type == DamageType.Spectral;
                    if (!isWhitelisted) return;
                }

                incomingDamage = Mathf.Max(1, Mathf.RoundToInt(incomingDamage * enemy.GetDamageMultiplier(packet.Type)));
            }
            // VS rule: each point of Armor removes 1 damage from every hit, but a hit always deals at least 1.
            if (Armor > 0) incomingDamage = Mathf.Max(1, incomingDamage - Armor);
            if (DamageModifier != null) incomingDamage = Mathf.Max(0, DamageModifier(incomingDamage));
            if (incomingDamage == 0) return;
            _currentHealth -= incomingDamage;
            // VS Results: each weapon's damage. The hit's full damage counts, overkill included (the wiki gives no rule).
            if (enemy != null && DamageSource.Any && SoulHunter.Gameplay.Core.RunStatsTracker.Instance != null)
                SoulHunter.Gameplay.Core.RunStatsTracker.Instance.RecordHit(DamageSource.Weapon, DamageSource.Name, incomingDamage);
            // The player's Arcanas that act on a weapon's hit (Heart of Fire's impact explosion, Jail of Crystal's freeze).
            if (enemy != null && DamageSource.Weapon != null) WeaponHitObserver?.Invoke(DamageSource.Weapon, enemy, packet.HitPoint);
            if (PostHitInvulnerability > 0f && !packet.IgnoresHitInvulnerability)
                _hitInvulnerableUntil = Time.time + PostHitInvulnerability;
            
            // Kami #3 Fix: Damage Number dikhana
            // Options: damage numbers can be turned off (VS "Damage Numbers").
            if (SoulHunter.Core.Persistence.GameSettings.DamageNumbers && SoulHunter.Gameplay.UI.DamagePopupManager.Instance != null)
            {
                // Thoda sa upar dikhate hain taaki enemy ke sir par aaye
                Vector3 popupPos = transform.position + Vector3.up * 1.5f; 
                SoulHunter.Gameplay.UI.DamagePopupManager.Instance.ShowDamage(incomingDamage, popupPos);
            }

            _currentHealth = Mathf.Max(0, _currentHealth);

            OnDamaged?.Invoke();

            // VS knockback: an enemy's movement is reversed for 120 ms (EnemyController). The player is never knocked
            // back in VS; it used to slide 2 units whenever an enemy touched it.
            if (enemy != null && KnockbackResistance < 1f)
                enemy.ApplyKnockback(packet.KnockbackOrDefault * (1f - KnockbackResistance));

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (_currentHealth == 0)
            {
                if (enemy != null && RetaliationDepth > 0) RetaliationKillObserver?.Invoke(enemy);
                Die();
            }
        }

        /// <summary>
        /// Changes every heal before it lands (the player's Arcanas: Sarabande of Healing doubles it). Null = unchanged.
        /// </summary>
        public Func<int, int> HealModifier { get; set; }

        /// <summary>
        /// Every heal as attempted (after <see cref="HealModifier"/>), whether or not health was missing: VS Sarabande of
        /// Healing (VI) "does not require missing health to deal damage either". The revival's heal is one too.
        /// </summary>
        public event Action<int> OnHealAttempted;

        /// <summary>
        /// Adds a fraction of Max Health without healing it (VS Divine Bloodline (IX): "+0.5 Max Health", and "current
        /// health is not restored at the same time"). Fractions add up until they reach a whole point.
        /// </summary>
        public void AddMaxHealthWithoutHealing(float amount)
        {
            if (amount <= 0f) return;
            if (_preciseMaxHealth <= 0f) _preciseMaxHealth = _maxHealth;
            _preciseMaxHealth += amount;
            _maxHealth = Mathf.Max(1, Mathf.FloorToInt(_preciseMaxHealth + 0.0001f));
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void Heal(int amount)
        {
            if (_currentHealth <= 0 || amount <= 0) return; // Dead things don't heal
            if (HealModifier != null) amount = Mathf.Max(0, HealModifier(amount));
            if (amount <= 0) return;
            OnHealAttempted?.Invoke(amount);
            int before = _currentHealth;
            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth); // Cap to max
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            int healed = _currentHealth - before; // overheal is not healing (wiki Soul_Eater)
            if (healed > 0) OnHealed?.Invoke(healed);
        }

        /// <summary>
        /// Arena cleanup/boss transition ke liye silent instant death. Isse
        /// gameplay damage popup mein fake 99999 number nahi dikhta.
        /// </summary>
        /// <summary>
        /// VS instant kill (vampire.survivors.wiki/w/Pentagram and Gorgeous_Moon): it "always deals damage equal to the max
        /// health of the enemy. If it is lower than 660, the damage number is overwritten to display it between 655 and
        /// 665." The damage is credited to the current <see cref="DamageSource"/> for the Results screen, then the enemy dies.
        /// </summary>
        public void EraseByInstantKill()
        {
            if (_currentHealth <= 0) return;
            int damage = Mathf.Max(1, _maxHealth);
            var enemy = GetComponentInParent<SoulHunter.Gameplay.AI.EnemyController>();
            if (enemy != null && DamageSource.Any && SoulHunter.Gameplay.Core.RunStatsTracker.Instance != null)
                SoulHunter.Gameplay.Core.RunStatsTracker.Instance.RecordHit(DamageSource.Weapon, DamageSource.Name, damage);
            if (SoulHunter.Core.Persistence.GameSettings.DamageNumbers && SoulHunter.Gameplay.UI.DamagePopupManager.Instance != null)
                SoulHunter.Gameplay.UI.DamagePopupManager.Instance.ShowDamage(InstantKillShownDamage(damage, UnityEngine.Random.value), transform.position + Vector3.up * 1.5f);
            KillSilently();
        }

        /// <summary>The number an instant kill shows: the damage, or 655-665 when it is under 660 (wiki Pentagram).</summary>
        public static int InstantKillShownDamage(int damage, float roll01) =>
            damage >= 660 ? damage : 655 + Mathf.Clamp(Mathf.FloorToInt(roll01 * 11f), 0, 10);

        public void KillSilently()
        {
            if (_currentHealth <= 0) return;
            _currentHealth = 0;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            SilentKillDepth++;
            try { Die(); }
            finally { SilentKillDepth = Mathf.Max(0, SilentKillDepth - 1); }
        }

        private static int SilentKillDepth;

        /// <summary>
        /// True while an instant kill (Pentagram, Eclipse Sigil, Soul Toll, an Arcana zone, the stage-clear wipe) is ending a
        /// life. VS Scarleton (wiki Scarleton): "if killed by Pentagram, Gorgeous Moon, Rosary or Heaven Sword, it will not
        /// resurrect" (EnemyLives).
        /// </summary>
        public static bool IsInstantKill => SilentKillDepth > 0;

        /// <summary>The health a revival restores: VS Revival, "revive once upon death with 50% of their Max Health".</summary>
        public static int RevivalHealth(int maxHealth) => Mathf.Max(1, Mathf.CeilToInt(maxHealth * 0.5f));

        /// <summary>
        /// Holds a lethal hit for the Revive screen: call it from <see cref="TryPreventDeath"/> and return true. The owner
        /// stays alive at 0 health, nothing hurts it, and OnDied does not fire.
        /// </summary>
        public void HoldForRevive()
        {
            if (_currentHealth <= 0) IsAwaitingRevive = true;
        }

        /// <summary>A held death that will not be revived after all: runs the normal death (OnDied).</summary>
        public void FinishDeath()
        {
            if (!IsAwaitingRevive) return;
            IsAwaitingRevive = false;
            DieWithoutPrevention();
        }

        /// <summary>
        /// Revives with 50% of max health (VS Revival), counted as healing (<see cref="OnHealed"/>), and starts the revival's
        /// immunity on game time (<see cref="IsRevivalInvulnerable"/>). Called from <see cref="TryPreventDeath"/> (Shadow
        /// Kael's mirrored revivals), for a held death (the player's Revive button), or from an OnDied listener (the object
        /// is then kept alive).
        /// </summary>
        public void ReviveFromDeath(int restoredHealth = -1)
        {
            if (!IsAwaitingRevive) _keepAliveAfterDeath = true;
            IsAwaitingRevive = false;
            int before = Mathf.Max(0, _currentHealth);
            int restore = restoredHealth > 0 ? restoredHealth : RevivalHealth(_maxHealth);
            // VS Sarabande of Healing (VI): "Healing from Reviving is also affected" ("doubled to become a full heal").
            if (HealModifier != null) restore = Mathf.Max(1, HealModifier(restore));
            OnHealAttempted?.Invoke(restore);
            _currentHealth = Mathf.Min(restore, _maxHealth);
            _revivalInvulnerableUntil = Time.time + RevivalInvulnerabilitySeconds + VsInvulnerabilityTime;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            int healed = _currentHealth - before;
            if (healed > 0) OnHealed?.Invoke(healed);
        }

        public void IncreaseMaxHealth(int amount)
        {
            if (_preciseMaxHealth <= 0f) _preciseMaxHealth = _maxHealth;
            _preciseMaxHealth += amount;
            _maxHealth += amount;
            _currentHealth += amount; // Asli VS ki tarah Max Health barhne par player heal bhi hota hai
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            Debug.Log($"[HealthController] Max Health increased by {amount}! New Max: {_maxHealth}");
        }

        /// <param name="healByIncrease">
        /// True (default) also restores the health gained. VS Hollow Heart passes false: it "does not heal to fill up the
        /// newly increased Max Health".
        /// </param>
        public void MultiplyMaxHealth(float multiplier, bool healByIncrease = true)
        {
            if (multiplier <= 0f) return;
            if (_preciseMaxHealth <= 0f) _preciseMaxHealth = _maxHealth;
            int previous = _maxHealth;
            _preciseMaxHealth *= multiplier;
            _maxHealth = Mathf.Max(1, Mathf.RoundToInt(_preciseMaxHealth));
            if (_currentHealth > 0)
                _currentHealth = Mathf.Clamp(healByIncrease ? _currentHealth + _maxHealth - previous : _currentHealth, 1, _maxHealth);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void Die()
        {
            if (TryPreventDeath != null && TryPreventDeath() && (_currentHealth > 0 || IsAwaitingRevive))
            {
                // Revived at once (Shadow Kael, hazards) or held for the Revive screen: no OnDied either way.
                _keepAliveAfterDeath = false;
                return;
            }
            DieWithoutPrevention();
        }

        private void DieWithoutPrevention()
        {
            OnDied?.Invoke();
            // Learning Comment:
            // Generic cloned enemies (horde) ke marne par Debug.Log karne se Unity console freeze ho jata tha.
            // Ab sirf unique entities (jaise Player ya Boss) ka death log hoga.
            if (!gameObject.name.Contains("(Clone)"))
            {
                Debug.Log($"[HealthController] {gameObject.name} has died.");
            }
            
            // Basic death logic (Disable object); an OnDied listener that revived keeps it alive.
            if (_keepAliveAfterDeath) _keepAliveAfterDeath = false;
            else gameObject.SetActive(false);
        }
    }
}
