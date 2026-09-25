using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Player
{
    /// <summary>
    /// Shows a character's starting shield around the player while HealthController.IsShielded,
    /// blinks during the last seconds so the player sees it ending, then removes itself.
    /// </summary>
    public sealed class StartingShieldVisual : MonoBehaviour
    {
        public const float BlinkSeconds = 2f;
        private const float BlinkRate = 8f;

        private HealthController _health;
        private GameObject _visual;

        public GameObject Visual => _visual;

        /// <summary>Spawns the visual as a child of the shielded object; returns null if nothing to show.</summary>
        public static StartingShieldVisual Attach(HealthController health, GameObject visualPrefab)
        {
            if (health == null || visualPrefab == null || !health.IsShielded) return null;
            var holder = new GameObject("StartingShield");
            holder.transform.SetParent(health.transform, false);
            var shield = holder.AddComponent<StartingShieldVisual>();
            shield._health = health;
            shield._visual = Instantiate(visualPrefab, holder.transform);
            return shield;
        }

        private void Update()
        {
            if (_health == null || !_health.IsShielded)
            {
                Destroy(gameObject);
                return;
            }
            float remaining = _health.ShieldedUntil - Time.time;
            bool visible = remaining > BlinkSeconds || Mathf.Repeat(Time.time * BlinkRate, 2f) < 1f;
            if (_visual != null && _visual.activeSelf != visible) _visual.SetActive(visible);
        }
    }
}
