using UnityEngine;

namespace SoulHunter.Gameplay.Physics
{
    /// <summary>
    /// Learning Comment:
    /// Decoupled Physics Scanner. Ye script kis entity par lagi hai, isay farq nahi padta (Single Responsibility).
    /// Isme Zero-Allocation physics calls use ki jayengi.
    /// </summary>
    public class EnvironmentScanner : MonoBehaviour
    {
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private Transform _feetPivot;
        [SerializeField] private float _groundCheckRadius = 0.3f;

        private EnvironmentData _currentData;
        private readonly Collider[] _hitColliders = new Collider[1]; // Zero-allocation array

        public EnvironmentData ScanEnvironment()
        {
            _currentData.IsGrounded = CheckGrounded();
            
            if (_currentData.IsGrounded)
            {
                CalculateGroundSlope();
            }
            else
            {
                _currentData.GroundNormal = Vector3.up;
                _currentData.GroundAngle = 0f;
            }

            return _currentData;
        }

        private bool CheckGrounded()
        {
            if (_feetPivot == null) return false;
            
            // NonAlloc Physics call to avoid garbage collection
            int hits = UnityEngine.Physics.OverlapSphereNonAlloc(_feetPivot.position, _groundCheckRadius, _hitColliders, _groundLayer);
            return hits > 0;
        }

        private void CalculateGroundSlope()
        {
            if (UnityEngine.Physics.Raycast(_feetPivot.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 1f, _groundLayer))
            {
                _currentData.GroundNormal = hit.normal;
                _currentData.GroundAngle = Vector3.Angle(Vector3.up, hit.normal);
            }
        }
    }
}
