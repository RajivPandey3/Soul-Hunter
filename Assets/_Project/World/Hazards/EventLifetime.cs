using UnityEngine;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// A map-event enemy that keeps its own movement (a homing VS "Swarm": Milk Elemental, Skeleton) but goes when its
    /// event's "duration" is up, back to the pool without drops (it was not defeated). Game time, so a pause holds it.
    /// Removed when the enemy goes back to the pool, so the next one from the pool is ordinary.
    /// </summary>
    [DisallowMultipleComponent]
    public class EventLifetime : MonoBehaviour
    {
        public float RemainingSeconds { get; set; }

        public static EventLifetime Begin(GameObject enemyObject, float seconds)
        {
            if (enemyObject == null || seconds <= 0f) return null;
            var life = enemyObject.GetComponent<EventLifetime>();
            if (life == null) life = enemyObject.AddComponent<EventLifetime>();
            life.RemainingSeconds = seconds;
            return life;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            RemainingSeconds -= dt;
            if (RemainingSeconds <= 0f) gameObject.SetActive(false);
        }

        private void OnDisable() => Destroy(this);
    }
}
