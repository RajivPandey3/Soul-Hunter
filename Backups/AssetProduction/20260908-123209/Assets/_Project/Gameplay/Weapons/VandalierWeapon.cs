using UnityEngine;
using System.Collections;

namespace SoulHunter.Gameplay.Combat
{
    public class VandalierWeapon : AutoAttackWeapon
    {
        public GameObject BirdVisual; // Combine both birds into one
        public GameObject BombPrefab; 
        public float CircleRadius = 4f;
        
        private int _bombsPerAttack = 8; // Double the normal bombs
        private float _angle = 0f;

        protected override void Update()
        {
            base.Update();
            if (BirdVisual != null)
            {
                // Rotates the two zones
                _angle += Time.deltaTime * 80f;
                Vector3 offset = new Vector3(Mathf.Cos(_angle * Mathf.Deg2Rad), Mathf.Sin(_angle * Mathf.Deg2Rad), 0) * CircleRadius;
                BirdVisual.transform.position = transform.position + offset;
            }
        }

        public override void LevelUp() {}

        protected override void Attack()
        {
            if (BombPrefab == null || BirdVisual == null) return;
            StartCoroutine(CarpetBombing());
        }

        private IEnumerator CarpetBombing()
        {
            for (int i = 0; i < _bombsPerAttack; i++)
            {
                // Opposite sides bombing at the same time
                Vector3 dropPos1 = BirdVisual.transform.position;
                Vector3 dropPos2 = transform.position - (BirdVisual.transform.position - transform.position);

                DropBomb(dropPos1);
                DropBomb(dropPos2);
                
                yield return new WaitForSeconds(0.05f);
            }
        }

        private void DropBomb(Vector3 pos)
        {
            GameObject bomb = WeaponPoolManager.Instance != null
                ? WeaponPoolManager.Instance.GetFromPool(BombPrefab, pos, Quaternion.identity)
                : Instantiate(BombPrefab, pos, Quaternion.identity);
            if (bomb == null) return;
            
            var damageDealer = bomb.GetComponent<ProjectileDamage>();
            if (damageDealer == null) damageDealer = bomb.AddComponent<ProjectileDamage>();
            damageDealer.DamageAmount = 45f;
            damageDealer.SourceWeaponName = "Vandalier";
            
            var lifetime = bomb.GetComponent<PooledLifetime>();
            if (lifetime != null) lifetime.Arm(1.5f);
            else Destroy(bomb, 1.5f);
        }
    }
}
