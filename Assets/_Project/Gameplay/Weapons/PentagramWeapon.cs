using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Combat
{
    public class PentagramWeapon : AutoAttackWeapon
    {
        protected override bool ShowsAttackPose => false; // an orbit/aura/area weapon: no swing toward enemies

        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Explosion;
        public GameObject PentagramVFXPrefab; 
        
        // VS wiki (Pentagram): 90 s cooldown, 10% chance not to erase an enemy's drops. Levels: L2 CD -10; L3 25%;
        // L4 CD -10; L5 45%; L6 CD -5; L7 65%; L8 CD -5.
        public const float VsCooldown = 90f;
        public const float VsKeepChance = 0.1f;

        // Base chance at level 1; level-ups add on top (tests set it to 0 to erase every drop).
        private float _chanceToKeepGems = VsKeepChance;

        public override int MaxLevel => 8;
        private int Level => Mathf.Clamp(CurrentLevel, 1, 8);
        public float CooldownForLevel => VsCooldown - (Level >= 2 ? 10f : 0f) - (Level >= 4 ? 10f : 0f) - (Level >= 6 ? 5f : 0f) - (Level >= 8 ? 5f : 0f);
        public float KeepChance => _chanceToKeepGems + (Level >= 7 ? 0.55f : Level >= 5 ? 0.35f : Level >= 3 ? 0.15f : 0f);

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
                    if (lifetime != null) lifetime.Arm(2f);
                    else Destroy(vfx, 2f);
                }
            }

            // Shadow Kael's copy would wipe its own horde and itself. Wiping the player is not a VS mechanic, so a
            // boss-held Pentagram only flashes (owner decision pending).
            if (HeldByEnemy) return;

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
                // Check chance to leave gem
                bool keepDrop = Random.value < KeepChance;
                if (!keepDrop)
                {
                    // Erase the drops of this life only. Disabling the component (as before) also stopped the kill
                    // being counted, lost chest elites' chests, and stayed off when the pool reused the enemy, so it
                    // never dropped a gem again.
                    var drop = enemy.GetComponent<EnemyDrop>();
                    if (drop != null) drop.EraseDropsThisLife();
                }
                
                var health = enemy.GetComponent<HealthController>();
                if (health != null) health.KillSilently();
            }
        }
    }
}
