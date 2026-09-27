using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// One Hellfire Breath flame: grows from its start size to its end size over its life (VS: "the flame will grow in
    /// size as they travel"). Projectile moves it, hits with it and returns it to the pool.
    /// </summary>
    public sealed class FireBreathFlame : MonoBehaviour
    {
        private Vector3 _from = Vector3.one, _to = Vector3.one;
        private float _life = 1f, _age;

        public void Begin(Vector3 fromScale, Vector3 toScale, float lifeSeconds)
        {
            _from = fromScale;
            _to = toScale;
            _life = Mathf.Max(0.01f, lifeSeconds);
            _age = 0f;
            transform.localScale = _from;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            transform.localScale = Vector3.Lerp(_from, _to, Mathf.Clamp01(_age / _life));
        }
    }
}
