using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Stormbound Halo: Soul Hunter's evolution of the Lightning Ring (owner's name, 2026-10-01), built on VS Thunder Loop
    /// (vampire.survivors.wiki/w/Thunder_Loop, fetched 2026-10-01): Lightning Ring at max level + Duplicator. 65 damage,
    /// 6 strikes, Area 4 (the Lightning Ring's maxed Area), 4.5 s cooldown, no interval between strikes; "the lightning
    /// strikes again in the same location after a small delay". The wiki gives no number for the delay: 0.5 s is Soul
    /// Hunter's. Strikes go to the nearest enemies (owner's aiming decision). A boss-held copy strikes the player.
    /// </summary>
    public class StormboundHaloWeapon : AutoAttackWeapon
    {
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Magic;

        public GameObject LightningStrikePrefab;
        public float StrikeRadius = 15f;

        public const float VsDamage = 65f;
        public const int VsAmount = 6;
        public const float VsArea = 4f;
        public const float VsCooldown = 4.5f;
        public const float RepeatDelay = 0.5f;

        public override int MaxLevel => 1;
        public override void LevelUp() { }

        /// <summary>Strikes landed so far, the repeats included (tests).</summary>
        public int Strikes { get; private set; }

        private readonly List<EnemyController> _targets = new List<EnemyController>(64);

        protected override void Awake()
        {
            base.Awake();
            DamageAmount = VsDamage;
            AttackCooldown = VsCooldown;
        }

        protected override void Attack()
        {
            if (LightningStrikePrefab == null) return;
            if (HeldByEnemy)
            {
                var player = FindNearestEnemy(StrikeRadius);
                if (player != null) StartCoroutine(StrikeTwice(player.position));
                return;
            }
            _targets.Clear();
            foreach (var enemy in EnemyController.ActiveEnemies)
                if (enemy != null && enemy.gameObject.activeInHierarchy) _targets.Add(enemy);
            Vector3 me = transform.position;
            _targets.Sort((a, b) => (a.transform.position - me).sqrMagnitude.CompareTo((b.transform.position - me).sqrMagnitude));
            int count = Mathf.Min(VsAmount + ExtraAmount, _targets.Count);
            for (int i = 0; i < count; i++)
            {
                Vector3 at = _targets[i].transform.position;
                if ((at - me).sqrMagnitude <= StrikeRadius * StrikeRadius) StartCoroutine(StrikeTwice(at));
            }
        }

        private IEnumerator StrikeTwice(Vector3 position)
        {
            Strike(position);
            yield return new WaitForSeconds(RepeatDelay);
            if (isActiveAndEnabled) Strike(position); // the same spot again
        }

        private void Strike(Vector3 position)
        {
            GameObject strike = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(LightningStrikePrefab, position, Quaternion.identity)
                : Instantiate(LightningStrikePrefab, position, Quaternion.identity);
            if (strike == null) return;
            Vector3 scale = LightningStrikePrefab.transform.localScale * AreaMultiplier;
            strike.transform.localScale = new Vector3(scale.x * VsArea, scale.y, scale.z * VsArea);
            var damage = strike.GetComponent<ProjectileDamage>();
            if (damage == null) damage = strike.AddComponent<ProjectileDamage>();
            damage.DamageAmount = ScaledDamage(VsDamage);
            damage.DamageType = DamageType.Magic;
            damage.SourceWeaponName = "Stormbound Halo";
            AimAtHostiles(strike);
            var lifetime = strike.GetComponent<PooledLifetime>();
            if (lifetime != null) lifetime.Arm(0.5f);
            else Destroy(strike, 0.5f);
            Strikes++;
        }
    }
}
