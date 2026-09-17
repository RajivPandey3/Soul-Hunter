using UnityEngine;
using System.Collections;

namespace SoulHunter.Gameplay.VFX
{
    /// <summary>
    /// Learning Comment:
    /// VS Quality Screen Shake. Jab Soul Burst use hoga ya bari hit lagegi,
    /// toh screen hil jayegi. Isse hits mein "Wazan" (Weight) feel hota hai.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        private Vector3 _originalPos;
        private float _shakeDuration = 0f;
        private float _shakeMagnitude = 0.7f;
        private float _dampingSpeed = 1.0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnEnable()
        {
            _originalPos = transform.localPosition;
        }

        private void Update()
        {
            if (_shakeDuration > 0)
            {
                transform.localPosition = _originalPos + Random.insideUnitSphere * _shakeMagnitude;
                _shakeDuration -= Time.deltaTime * _dampingSpeed;
            }
            else
            {
                _shakeDuration = 0f;
                transform.localPosition = _originalPos;
            }
        }

        public void TriggerShake(float duration = 0.2f, float magnitude = 0.5f)
        {
            _shakeDuration = duration;
            _shakeMagnitude = magnitude;
        }
    }
}
