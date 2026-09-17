using UnityEngine;

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
            if (_target == null)
            {
                // Agar target nahi hai toh Player ko dhundhne ki koshish karo
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    _target = player.transform;
                }
                return;
            }

            Vector3 desiredPosition = _target.position + _offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed * Time.deltaTime);
            transform.LookAt(_target); // Player ki taraf dekhna
        }
    }
}
