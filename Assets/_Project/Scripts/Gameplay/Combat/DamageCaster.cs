using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// Decoupled Hitbox component. Weapon par lagakar OverlapBoxNonAlloc se damage apply karta hai.
    /// 2036 Test pass: Player aur Enemy dono ke weapons ke liye same script use ho sakti hai.
    /// </summary>
    public class DamageCaster : MonoBehaviour
    {
        [SerializeField] private Vector3 _hitboxSize = new Vector3(1, 1, 1);
        [SerializeField] private LayerMask _targetLayer;
        [SerializeField] private int _damageAmount = 10;
        
        private readonly Collider[] _hitColliders = new Collider[5];

        public void CastDamage()
        {
            // Zero-allocation physics check
            int hits = UnityEngine.Physics.OverlapBoxNonAlloc(
                transform.position, 
                _hitboxSize / 2f, 
                _hitColliders, 
                transform.rotation, 
                _targetLayer
            );

            for (int i = 0; i < hits; i++)
            {
                var target = _hitColliders[i].GetComponent<IDamageable>();
                if (target != null)
                {
                    Vector3 knockback = (_hitColliders[i].transform.position - transform.position).normalized;
                    DamagePacket packet = new DamagePacket(_damageAmount, transform.position, knockback);
                    target.TakeDamage(packet);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(Vector3.zero, _hitboxSize);
        }
    }
}
