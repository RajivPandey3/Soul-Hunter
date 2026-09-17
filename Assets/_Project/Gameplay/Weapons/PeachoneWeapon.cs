using UnityEngine;
using System.Collections;

namespace SoulHunter.Gameplay.Combat
{
    public class PeachoneWeapon : AutoAttackWeapon
    {
        public GameObject BirdVisual; // Bird circling the player
        public GameObject BombPrefab; // The projectile dropped
        public float CircleRadius = 3f;
        
        private int _bombsPerAttack = 1;
        private float _angle = 0f;

        protected override void Update()
        {
            base.Update();
            if (BirdVisual != null)
            {
                // Circle around player
                _angle += Time.deltaTime * 50f;
                Vector3 offset = new Vector3(Mathf.Cos(_angle * Mathf.Deg2Rad), Mathf.Sin(_angle * Mathf.Deg2Rad), 0) * CircleRadius;
                BirdVisual.transform.position = transform.position + offset;
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
            for (int i = 0; i < _bombsPerAttack; i++)
            {
                Vector3 dropPos = BirdVisual.transform.position;
                GameObject bomb = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(BombPrefab, dropPos, Quaternion.identity)
                    : Instantiate(BombPrefab, dropPos, Quaternion.identity);
                if (bomb == null) continue;
                
                var damageDealer = bomb.GetComponent<ProjectileDamage>();
                if (damageDealer == null) damageDealer = bomb.AddComponent<ProjectileDamage>();
                damageDealer.DamageAmount = DamageAmount;
                damageDealer.SourceWeaponName = "Peachone";
                
                var lifetime = bomb.GetComponent<PooledLifetime>();
                if (lifetime != null) lifetime.Arm(1.5f);
                else Destroy(bomb, 1.5f);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
