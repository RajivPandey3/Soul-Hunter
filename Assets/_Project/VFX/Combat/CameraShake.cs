using UnityEngine;

namespace SoulHunter.Gameplay.VFX
{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        private Vector3 _baseLocalPosition;
        private float _shakeTime;
        private float _shakeDuration;
        private float _shakeMagnitude;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _baseLocalPosition = transform.localPosition;
        }

        private void LateUpdate()
        {
            if (_shakeTime > 0f)
            {
                _shakeTime -= Time.deltaTime;

                float normalizedTime = Mathf.Clamp01(
                    _shakeTime / _shakeDuration
                );

                Vector3 offset =
                    Random.insideUnitSphere *
                    (_shakeMagnitude * normalizedTime);

                transform.localPosition = _baseLocalPosition + offset;
            }
            else
            {
                transform.localPosition = _baseLocalPosition;
            }
        }

        public void TriggerShake(
            float duration = 0.2f,
            float magnitude = 0.5f)
        {
            _shakeDuration = duration;
            _shakeMagnitude = magnitude;
            _shakeTime = duration;
        }
    }
}