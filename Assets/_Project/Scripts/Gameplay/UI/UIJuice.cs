using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Ye script kisi bhi UI Panel ya Button ko "Juicy" (bouncy) bana deti hai.
    /// Isey seedha apne UI element (jaise Button ya Panel) par drag karke daalein.
    /// </summary>
    public class UIJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        [Header("Animation Settings")]
        public bool AnimateOnEnable = true;
        public float PopInDuration = 0.3f;
        public Vector3 HoverScale = new Vector3(1.1f, 1.1f, 1.1f);
        public Vector3 ClickScale = new Vector3(0.9f, 0.9f, 0.9f);
        
        private Vector3 _originalScale;
        private Coroutine _currentCoroutine;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        private void OnEnable()
        {
            if (AnimateOnEnable)
            {
                transform.localScale = Vector3.zero;
                if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);
                _currentCoroutine = StartCoroutine(PopInAnimation());
            }
            else
            {
                transform.localScale = _originalScale;
            }
        }

        private void OnDisable()
        {
            transform.localScale = _originalScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);
            _currentCoroutine = StartCoroutine(ScaleTo(HoverScale, 0.1f));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);
            _currentCoroutine = StartCoroutine(ScaleTo(_originalScale, 0.1f));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);
            _currentCoroutine = StartCoroutine(ScaleTo(ClickScale, 0.05f));
        }

        private IEnumerator PopInAnimation()
        {
            float elapsed = 0f;
            while (elapsed < PopInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / PopInDuration;
                // Simple ease out back math
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                if (t > 0.7f) ease = 1f + Mathf.Sin((t - 0.7f) * Mathf.PI * 3f) * 0.1f;
                
                transform.localScale = _originalScale * ease;
                yield return null;
            }
            transform.localScale = _originalScale;
        }

        private IEnumerator ScaleTo(Vector3 targetScale, float duration)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
                yield return null;
            }
            transform.localScale = targetScale;
        }
    }
}
