using UnityEngine;
using System.Collections;

namespace SoulHunter.Gameplay.Combat
{
    public class PeachoneWeapon : AutoAttackWeapon
    {
        protected override bool ShowsAttackPose => false; // an orbit/aura/area weapon: no swing toward enemies

        public GameObject BirdVisual; // Bird circling the player
        public GameObject BombPrefab; // The projectile dropped
        public float CircleRadius = 3f;
        
        private int _bombsPerAttack = 1;
        private float _angle = 0f;

        public const float BirdHeight = 1.6f;

        protected override void Awake()
        {
            base.Awake();
            // BirdVisual was never assigned, so Attack() returned at once and Peachone never dropped a bomb.
            // The prefab now names a bird prefab; the weapon flies its own instance of it.
            if (BirdVisual != null && !BirdVisual.scene.IsValid())
                BirdVisual = Instantiate(BirdVisual, transform.position, Quaternion.identity, transform);
        }

        protected override void Update()
        {
            base.Update();
            if (BirdVisual != null)
            {
                // Circle around the player on the ground plane (it used to circle vertically, in X-Y).
                _angle += Time.deltaTime * 50f;
                float rad = _angle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * CircleRadius;
                BirdVisual.transform.position = transform.position + offset + Vector3.up * BirdHeight;
                BirdVisual.transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad)));
            }
        }

        public override void LevelUp()
        {
            CurrentLevel++;
            DamageAmount += 5f; 
            if (CurrentLevel % 2 == 0) _bombsPerAttack++; 
        }

        protected override void Attack()
        {
            if (BombPrefab == null || BirdVisual == null) return;
            StartCoroutine(BombingRun());
        }

        private IEnumerator BombingRun()
        {
            for (int i = 0; i < _bombsPerAttack + ExtraAmount; i++)
            {
                Vector3 dropPos = BirdVisual.transform.position - Vector3.up * BirdHeight; // blast on the ground under the bird
                GameObject bomb = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(BombPrefab, dropPos, Quaternion.identity)
                    : Instantiate(BombPrefab, dropPos, Quaternion.identity);
                if (bomb == null) continue;
                ApplyArea(bomb, BombPrefab);
                
                var damageDealer = bomb.GetComponent<ProjectileDamage>();
                if (damageDealer == null) damageDealer = bomb.AddComponent<ProjectileDamage>();
                damageDealer.DamageAmount = ScaledDamage(DamageAmount);
                damageDealer.DamageType = DamageType.Magic; // Owner (2026-09-26, Level 7): weapons are typed by theme so ghosts can be fought with more than four weapons.
                damageDealer.SourceWeaponName = "Peachone";
                AimAtHostiles(bomb);
                
                var lifetime = bomb.GetComponent<PooledLifetime>();
                if (lifetime != null) lifetime.Arm(1.5f);
                else Destroy(bomb, 1.5f);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
