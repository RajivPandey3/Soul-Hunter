using UnityEngine;
using System.Collections.Generic;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// Expansion 1: Lightning Ring (VS Style).
    /// Har kuch seconds baad random dushmanon par asman se bijli (lightning) girti hai.
    /// Ise aim karne ki zaroorat nahi hoti.
    /// </summary>
    public class LightningRingWeapon : AutoAttackWeapon
    {
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Magic;
        [Header("Lightning Ring Settings")]
        public GameObject LightningStrikePrefab; // Bijli ka visual aur damage hitbox
        public float StrikeRadius = 15f; // Kitni door tak bijli gir sakti hai
        
        // VS wiki (Lightning Ring): 15 damage, 4.5 s cooldown, 2 strikes. Levels: L2 +1 strike; L3 Area +100%, +10;
        // L4 +1 strike; L5 Area +100%, +20; L6 +1 strike; L7 Area +100%, +20; L8 +1 strike.
        // Area bonuses scale our own strike size.
        public const float VsBaseDamage = 15f;
        public const float VsCooldown = 4.5f;
        public const int VsAmount = 2;

        public override int MaxLevel => 8;
        private int Level => Mathf.Clamp(CurrentLevel, 1, 8);
        public float BaseDamage => DamageAmount + (Level >= 3 ? 10f : 0f) + (Level >= 5 ? 20f : 0f) + (Level >= 7 ? 20f : 0f);
        public int StrikeCount => VsAmount + (Level >= 2 ? 1 : 0) + (Level >= 4 ? 1 : 0) + (Level >= 6 ? 1 : 0) + (Level >= 8 ? 1 : 0);
        public float LevelArea => 1f + (Level >= 3 ? 1f : 0f) + (Level >= 5 ? 1f : 0f) + (Level >= 7 ? 1f : 0f);
        private readonly List<EnemyController> _targetBuffer = new List<EnemyController>(64);

        protected override void Awake()
        {
            base.Awake();
            DamageAmount = VsBaseDamage;
            AttackCooldown = VsCooldown;
        }

        public override void LevelUp() { CurrentLevel = Mathf.Min(MaxLevel, CurrentLevel + 1); }

        public const float VsInterval = 0.05f;

        protected override void Attack()
        {
            if (LightningStrikePrefab == null) return;

            // Shadow Kael's copy strikes the player (one strike: there is one target), never its own horde.
            if (HeldByEnemy)
            {
                var player = FindNearestEnemy(StrikeRadius);
                if (player != null) SpawnLightning(player.position);
                return;
            }

            // Screen par mojood dushman dhoondo
            var allEnemies = EnemyController.ActiveEnemies;
            if (allEnemies.Count == 0) return;

            // Random dushmanon ko target karo
            _targetBuffer.Clear();
            _targetBuffer.AddRange(allEnemies);
            List<EnemyController> validTargets = _targetBuffer;
            
            int strikesThisTurn = Mathf.Min(StrikeCount + ExtraAmount, validTargets.Count);

            // VS interval (wiki Lightning_Ring): the Amount of shots leave 0.05s apart.
            FireInSuccession(strikesThisTurn, VsInterval, _strikeShot ??= StrikeShot);
        }

        private SuccessionShot _strikeShot;

        private void StrikeShot(int i, int count, Vector3 arg)
        {
            // Owner decision: aim at the nearest enemy; strike the nearest remaining enemies first.
            // Strikes come 0.05 s apart now: an enemy killed or pooled in between is passed over for the next one,
            // so every strike still lands while enemies remain.
            EnemyController target = null;
            while (target == null)
            {
                int nearestIndex = NearestIndex(_targetBuffer);
                if (nearestIndex < 0) return;
                target = _targetBuffer[nearestIndex];
                _targetBuffer.RemoveAt(nearestIndex); // Ek dushman pe 2 bijliyan na giren
                if (target != null && !target.gameObject.activeInHierarchy) target = null;
            }

            if (Vector3.Distance(transform.position, target.transform.position) <= StrikeRadius)
            {
                SpawnLightning(target.transform.position);
            }
        }

        private int NearestIndex(List<EnemyController> candidates)
        {
            int best = -1;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] == null) continue;
                Vector3 offset = candidates[i].transform.position - transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude < bestSqr) { bestSqr = offset.sqrMagnitude; best = i; }
            }
            return best;
        }

        private void SpawnLightning(Vector3 position)
        {
            GameObject strike = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(LightningStrikePrefab, position, Quaternion.identity)
                : Instantiate(LightningStrikePrefab, position, Quaternion.identity);
            if (strike == null) return;
            // VS Area level-ups widen the strike on the ground; the bolt keeps its height.
            Vector3 scale = LightningStrikePrefab.transform.localScale * AreaMultiplier;
            strike.transform.localScale = new Vector3(scale.x * LevelArea, scale.y, scale.z * LevelArea);
            
            var damageDealer = strike.GetComponent<ProjectileDamage>();
            if (damageDealer == null) damageDealer = strike.AddComponent<ProjectileDamage>();
            
            damageDealer.DamageAmount = ScaledDamage(BaseDamage);
            damageDealer.DamageType = DamageType.Magic; // Owner (2026-09-26, Level 7): weapons are typed by theme so ghosts can be fought with more than four weapons.
            damageDealer.SourceWeaponName = "Lightning Ring";
            AimAtHostiles(strike);
            
            // Bijli 0.5 sec baad khud gayab ho jayegi
            var lifetime = strike.GetComponent<PooledLifetime>();
            if (lifetime != null) lifetime.Arm(0.5f);
            else Destroy(strike, 0.5f);
        }
    }
}
