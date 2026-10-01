using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>A Drowning Chalice pool: creeps toward its owner and grows (with Speed bonuses) up to a limit.</summary>
    public sealed class PoolCreep : MonoBehaviour
    {
        private Transform _target;
        private float _speed, _growthPerSecond, _maxGrowth;
        private Vector3 _baseScale;
        private float _growth = 1f;

        public float Growth => _growth;

        public void Begin(Transform target, float speed, float growthPerSecond, Vector3 baseScale, float maxGrowth)
        {
            _target = target;
            _speed = speed;
            _growthPerSecond = growthPerSecond;
            _baseScale = baseScale;
            _maxGrowth = maxGrowth;
            _growth = 1f;
            enabled = true;
        }

        // A pooled pool reused by Santa Water must not creep: off until Begin is called again.
        private void OnDisable() => enabled = false;

        private void Update()
        {
            if (_target == null) return;
            Vector3 to = _target.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.04f) transform.position += to.normalized * Mathf.Min(_speed * Time.deltaTime, to.magnitude);
            if (_growthPerSecond > 0f && _growth < _maxGrowth)
            {
                _growth = Mathf.Min(_maxGrowth, _growth + _growthPerSecond * Time.deltaTime);
                transform.localScale = _baseScale * _growth;
            }
        }
    }
}
