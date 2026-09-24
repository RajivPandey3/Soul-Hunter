using UnityEngine;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.CameraSystem
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        
        // Learning Comment: Adjusted offset for a direct top-down view
        [SerializeField] private Vector3 _offset = new Vector3(0, 20f, 0f);
        [SerializeField] private float _smoothSpeed = 15f; // Increased for tighter following
        
        // Learning Comment: Added a toggle to enforce strict top-down rotation vs dynamic look-at
        [SerializeField] private bool _strictTopDown = true;

        private void Start()
        {
            // Learning Comment: Auto-assign player if it was not set in the inspector
            if (_target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _target = player.transform;
                }
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 desiredPosition = _target.position + _offset;

            // Learning Comment: Tighten the follow so procedural world edges aren't exposed by camera lag
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                _smoothSpeed * Time.deltaTime
            );

            // Learning Comment: Lock the rotation to look straight down (90 degrees on X axis) for top-down games
            if (_strictTopDown)
            {
                transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                transform.LookAt(_target);
            }
        }
    }
}