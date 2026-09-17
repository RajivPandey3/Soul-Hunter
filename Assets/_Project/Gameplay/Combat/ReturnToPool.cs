using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// Object Pooling Helper: Ye script kisi object ko ek tay waqt (Lifetime) ke baad
    /// auto-disable kar deti hai, taake wo pool me wapas chala jaye aur dobara use ho sake.
    /// </summary>
    public class ReturnToPool : MonoBehaviour
    {
        public float Lifetime = 5f;
        private float _timer;

        private void OnEnable()
        {
            _timer = Lifetime;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
