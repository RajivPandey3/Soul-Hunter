using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Combat
{
    public class PentagramWeapon : AutoAttackWeapon
    {
        public override bool HasChance => true;
        protected override bool ShowsAttackPose => false; // an orbit/aura/area weapon: no swing toward enemies

        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Explosion;
        public GameObject PentagramVFXPrefab; 
        
        // VS wiki (Pentagram): 90 s cooldown, 10% chance not to erase an enemy's drops. Levels: L2 CD -10; L3 25%;
        // L4 CD -10; L5 45%; L6 CD -5; L7 65%; L8 CD -5.
        public const float VsCooldown = 90f;
        public const float VsKeepChance = 0.1f;
        /// <summary>VS wiki Pentagram: "a hard-coded limit of 15 seconds on how often Pentagram" can activate.</summary>
        public const float VsMinimumCooldown = 15f;
        /// <summary>VS wiki Pentagram infobox: duration 1 s (the glyph).</summary>
        public const float VsDuration = 1f;
        protected override float MinimumCooldown => VsMinimumCooldown;
        /// <summary>Whether the last activation erased items (tests).</summary>
        public bool LastActivationErasedItems { get; private set; }

        // Base chance at level 1; level-ups add on top (tests set it to 0 to erase every drop).
        private float _chanceToKeepGems = VsKeepChance;

        public override int MaxLevel => 8;
        private int Level => Mathf.Clamp(CurrentLevel, 1, 8);
        public float CooldownForLevel => VsCooldown - (Level >= 2 ? 10f : 0f) - (Level >= 4 ? 10f : 0f) - (Level >= 6 ? 5f : 0f) - (Level >= 8 ? 5f : 0f);
        /// <summary>Items the last boss-held activation erased (tests).</summary>
        public int LastItemsErased { get; private set; } = -1;

        public float KeepChance => _chanceToKeepGems + (Level >= 7 ? 0.55f : Level >= 5 ? 0.35f : Level >= 3 ? 0.15f : 0f) + LimitBreak.ChanceBonus(this); // + Limit Break Chance

        protected override void Awake()
        {
            base.Awake();
            AttackCooldown = CooldownForLevel;
        }

        public override void LevelUp()
        {
            CurrentLevel = Mathf.Min(MaxLevel, CurrentLevel + 1);
            AttackCooldown = CooldownForLevel;
        }

        protected override void Attack()
        {
            if (PentagramVFXPrefab != null)
            {
                GameObject vfx = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(PentagramVFXPrefab, transform.position, Quaternion.identity)
                    : Instantiate(PentagramVFXPrefab, transform.position, Quaternion.identity);
                if (vfx != null)
                {
                    var lifetime = vfx.GetComponent<PooledLifetime>();
                    if (lifetime != null) lifetime.Arm(VsDuration);
                    else Destroy(vfx, VsDuration);
                }
            }

            // Shadow Kael's copy never wipes its own horde or itself. Owner decision (2026-09-30): it uses VS Pentagram's
            // own item erase against the player: every item on screen except stage items goes, unless the VS keep roll
            // (with the boss's mirrored Luck) saves them.
            if (HeldByEnemy)
            {
                float luck = OwnerStats != null ? OwnerStats.Luck : 1f;
                if (SoulHunter.Gameplay.Pickups.GroundItemEraser.ErasesItems(KeepChance, luck, Random.value))
                    LastItemsErased = SoulHunter.Gameplay.Pickups.GroundItemEraser.EraseOnScreen(Camera.main);
                return;
            }

            // VS (wiki Pentagram): one roll per activation. Usually the glyph erases "every enemy, their drops, and all
            // items on the screen, except for stage items"; with the keep chance (x Luck) a blue symbol erases only the
            // enemies. Before 2026-10-01 each enemy rolled on its own and items on the ground were never erased.
            float playerLuck = OwnerStats != null ? OwnerStats.Luck : 1f;
            bool eraseItems = SoulHunter.Gameplay.Pickups.GroundItemEraser.ErasesItems(KeepChance, playerLuck, Random.value);
            LastActivationErasedItems = eraseItems;

            // Wipe all enemies on screen
            // KillSilently disables enemies immediately, and OnDisable removes
            // them from ActiveEnemies. Iterate backwards so that removal during
            // this wipe cannot invalidate an enumerator or skip the remaining
            // lower indices.
            var allEnemies = EnemyController.ActiveEnemies;
            for (int i = allEnemies.Count - 1; i >= 0; i--)
            {
                if (i >= allEnemies.Count) continue;
                var enemy = allEnemies[i];
                if (enemy == null) continue;
                // VS: the 25-minute stage bosses are immune to Pentagram's instant kill.
                if (enemy.IsStageBoss) continue;
                // VS instant kill resistance (vampire.survivors.wiki/w/Enemies, "Resistances": immunity to Pentagram).
                if (enemy.ResistsInstantKill) continue;
                if (eraseItems)
                {
                    // Erase the drops of this life only. Disabling the component (as before) also stopped the kill
                    // being counted, lost chest elites' chests, and stayed off when the pool reused the enemy, so it
                    // never dropped a gem again.
                    var drop = enemy.GetComponent<EnemyDrop>();
                    if (drop != null) drop.EraseDropsThisLife();
                }
                
                var health = enemy.GetComponent<HealthController>();
                if (health != null) using (DamageSource.From(this)) health.EraseByInstantKill(); // VS: max-health damage
            }
            if (eraseItems) LastItemsErased = SoulHunter.Gameplay.Pickups.GroundItemEraser.EraseOnScreen(Camera.main);
        }
    }
}
