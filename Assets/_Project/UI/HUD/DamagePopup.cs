using UnityEngine;
using TMPro;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Learning Comment:
    /// VS = SH Rule: Damage lagne par number udta hua dikhna chahiye.
    /// Ye script damage number ko upar le jati hai (float) aur dheere-dheere gayab (fade) karti hai.
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _textMesh;
        [SerializeField] private float _moveSpeed = 2f;
        [SerializeField] private float _lifeTime = 1f;

        private float _fadeSpeed;
        private Color _textColor;
        private float _timer;

        public void Setup(int damageAmount)
        {
            if (_textMesh == null) _textMesh = GetComponent<TextMeshPro>();

            _textMesh.text = damageAmount.ToString();
            _textColor = _textMesh.color;
            _textColor.a = 1f; // Full visible
            _textMesh.color = _textColor;

            _timer = _lifeTime;
            _fadeSpeed = 1f / _lifeTime;
        }

        private void Update()
        {
            // Upar ki taraf udna
            transform.position += Vector3.up * _moveSpeed * Time.deltaTime;

            // Gayab (Fade out) hona
            _timer -= Time.deltaTime;
            _textColor.a -= _fadeSpeed * Time.deltaTime;
            
            if (_textMesh != null)
            {
                _textMesh.color = _textColor;
            }

            // Jab time khatam ho jaye toh pool mein wapas bhej do (Ya abhi ke liye Destroy)
            if (_timer <= 0)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
