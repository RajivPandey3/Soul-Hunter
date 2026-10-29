using UnityEngine;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// A map-event enemy that keeps its own movement (a homing VS "Swarm": Milk Elemental, Skeleton) but goes when its
    /// event's "duration" is up, back to the pool without drops (it was not defeated). Game time, so a pause holds it.
    /// Removed when the enemy goes back to the pool, so the next one from the pool is ordinary.
    /// </summary>
    [DisallowMultipleComponent]
    public class EventLifetime : MonoBehaviour, ILeavesWithItsLife
    {
        public float RemainingSeconds { get; set; }

        public static EventLifetime Begin(GameObject enemyObject, float seconds)
        {
            if (enemyObject == null || seconds <= 0f) return null;
            var life = LifeComponent.Fresh<EventLifetime>(enemyObject);
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

        public bool Doomed { get; private set; }

        private void OnDisable()
        {
            if (Doomed) return;
            Doomed = true;
            Destroy(this);
        }
    }
}
