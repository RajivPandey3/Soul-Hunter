using UnityEngine;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.CameraSystem
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors mein camera hamesha player ke upar lock rehta hai.
    /// Ye script smoothly camera ko player ke pichhe/upar follow karwayegi.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new Vector3(0, 15f, -10f);
        [SerializeField] private float _smoothSpeed = 5f;


        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 desiredPosition = _target.position + _offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed * Time.deltaTime);
            // Follow the actual target in LateUpdate so the camera stays aligned
            // with the interpolated Rigidbody and does not fight the camera rig's
            // parent transform.
            transform.LookAt(_target);
        }
    }
}
