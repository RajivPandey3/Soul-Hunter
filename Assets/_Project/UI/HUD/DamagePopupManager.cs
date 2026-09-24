using UnityEngine;
using System.Collections.Generic;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Learning Comment:
    /// VS mein lagatar damage lagta hai, isliye agar hum har bar Instantiate karenge toh game hang (lag) hoga.
    /// Ye Manager ek "Object Pool" use karta hai (Jaise humne dushmano ke liye kiya tha). 
    /// Ye purane damage numbers ko recycle karta hai.
    /// </summary>
    public class DamagePopupManager : MonoBehaviour
    {
        public static DamagePopupManager Instance { get; private set; }

        [SerializeField] private GameObject _damagePopupPrefab;
        
        private List<GameObject> _popupPool = new List<GameObject>();

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

        private const int MAX_POPUPS = 25;
        private int _recycleIndex = 0;

        public void ShowDamage(int amount, Vector3 position)
        {
            if (_damagePopupPrefab == null) return;

            GameObject popupToSpawn = null;

            // Pool mein se koi chhipa hua popup dhoondho
            for (int i = 0; i < _popupPool.Count; i++)
            {
                if (_popupPool[i] != null && !_popupPool[i].activeInHierarchy)
                {
                    popupToSpawn = _popupPool[i];
                    break;
                }
            }

            // Learning Comment:
            // Capped UI Pool: Agar 50-100 dushman ek sath marein toh 100 naye UI Canvas/TMP
            // allocate karne ke bajaye hum max 25 popups recycle karte hain taake frame drops na hon.
            if (popupToSpawn == null)
            {
                if (_popupPool.Count < MAX_POPUPS)
                {
                    popupToSpawn = Instantiate(_damagePopupPrefab);
                    _popupPool.Add(popupToSpawn);
                }
                else
                {
                    _recycleIndex = (_recycleIndex + 1) % _popupPool.Count;
                    popupToSpawn = _popupPool[_recycleIndex];
                }
            }

            // Popup ko chalu karo
            popupToSpawn.transform.position = position;
            popupToSpawn.SetActive(true);

            // Script ko bolo apna animation shuru kare
            var popupScript = popupToSpawn.GetComponent<DamagePopup>();
            if (popupScript != null)
            {
                popupScript.Setup(amount);
            }
        }
    }
}
