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

        public const float WorldFontSize = 7f;
        public const float MaxLifetime = 0.65f;

        /// <summary>
        /// Bug MIS B07 (2026-09-30): numbers showed cut ("3Ↄ") where enemy models stood in front of them. The camera is
        /// orthographic, so moving the text toward it changes neither its place nor its size on screen, only its depth:
        /// it is drawn this far in front of its point, ahead of the models.
        /// </summary>
        public const float TowardCamera = 4f;
        private Vector3 _anchor;

        private float _fadeSpeed;
        private Color _textColor;
        private float _timer;

        /// <summary>
        /// 60 FPS pass (2026-10-14): a number wrote its fading colour to the text every frame, and TextMeshPro rebuilt
        /// the whole text each time (25 numbers, about 0.6 ms a frame). The fade is now shown in this many steps.
        /// </summary>
        public const int FadeSteps = 10;
        private float _shownAlpha;

        // The camera's pose, read once a frame for every number.
        private static int _cameraFrame = -1;
        private static bool _hasCamera, _orthographic;
        private static Quaternion _cameraRotation;
        private static Vector3 _cameraForward;

        private static void ReadCamera()
        {
            if (_cameraFrame == Time.frameCount) return;
            _cameraFrame = Time.frameCount;
            var cam = Camera.main;
            _hasCamera = cam != null;
            if (!_hasCamera) return;
            var t = cam.transform;
            _cameraRotation = t.rotation;
            _cameraForward = t.forward;
            _orthographic = cam.orthographic;
        }

        public void Setup(int damageAmount)
        {
            if (_textMesh == null) _textMesh = GetComponent<TextMeshPro>();

            _textMesh.text = damageAmount.ToString();
            // The prefab's 36 pt (~3.6 units) was sized for the old straight-down camera, which saw the text edge-on.
            // Facing the tilted camera it must be small, VS-style: under half the hero's height.
            _textMesh.fontSize = WorldFontSize;
            _textColor = _textMesh.color;
            _textColor.a = 1f; // Full visible
            _textMesh.color = _textColor;
            _shownAlpha = 1f;

            // ...and lives a shorter time (0.65 s), so fewer stay on screen at once.
            _timer = Mathf.Min(_lifeTime, MaxLifetime);
            _fadeSpeed = 1f / _timer;
            _anchor = transform.position; // the hit point (the manager places the popup there before Setup)
            Place();
        }

        /// <summary>At the anchor on screen, facing the camera and pulled toward it (see TowardCamera).</summary>
        private void Place()
        {
            ReadCamera();
            if (!_hasCamera) { transform.position = _anchor; return; }
            transform.SetPositionAndRotation(_anchor - _cameraForward * (_orthographic ? TowardCamera : 0f), _cameraRotation);
        }

        private void Update()
        {
            // Upar ki taraf udna, camera ki taraf mooh karke (tilted camera: flat text would look skewed)
            _anchor += Vector3.up * _moveSpeed * Time.deltaTime;
            Place();

            // Gayab (Fade out) hona
            _timer -= Time.deltaTime;
            _textColor.a -= _fadeSpeed * Time.deltaTime;
            
            float alpha = Mathf.Ceil(Mathf.Clamp01(_textColor.a) * FadeSteps) / FadeSteps;
            if (_textMesh != null && alpha != _shownAlpha)
            {
                _shownAlpha = alpha;
                var shown = _textColor;
                shown.a = alpha;
                _textMesh.color = shown;
            }

            // Jab time khatam ho jaye toh pool mein wapas bhej do (Ya abhi ke liye Destroy)
            if (_timer <= 0)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
