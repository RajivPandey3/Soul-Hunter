using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// A pooled enemy that stands where it was placed for one life (Omens' Rootbound, Powder Ring and Thorn Circle; VS
    /// Summon II Molise: "These don't move"). A Time Freeze or Clock Lancet would send it chasing afterwards, so it is put
    /// back on its spot. With a lifetime it leaves without drops when the time is up (it was not defeated). Everything
    /// is undone when the enemy goes back to the pool, so the next one from the pool is ordinary.
    /// </summary>
    [DisallowMultipleComponent]
    public class StillEnemy : MonoBehaviour
    {
        /// <summary>Game seconds left before it leaves; 0 or less = it stays until defeated.</summary>
        public float RemainingSeconds { get; set; }
        public bool IsLeaving { get; private set; }

        private EnemyController _enemy;
        private bool _timed;
        private bool _wasFrozen;

        public static StillEnemy Begin(GameObject enemyObject, float lifetimeSeconds = 0f)
        {
            if (enemyObject == null) return null;
            var still = enemyObject.GetComponent<StillEnemy>();
            if (still == null) still = enemyObject.AddComponent<StillEnemy>();
            still._enemy = enemyObject.GetComponent<EnemyController>();
            still._timed = lifetimeSeconds > 0f;
            still.RemainingSeconds = lifetimeSeconds;
            still.IsLeaving = false;
            still.Hold();
            // Bug MIS AI-06 (wiki Orologion: "new enemies will also become frozen"): a member spawned during an Orologion
            // was frozen by SpawnEnemy, and the hold replaced that; it stays frozen for the rest of it, then holds again.
            EnemySpawner.KeepWorldFreeze(still._enemy);
            return still;
        }

        private void Hold()
        {
            if (_enemy != null) _enemy.ChangeState(new EnemyHoldState(_enemy));
        }

        private void Update()
        {
            if (IsLeaving || _enemy == null) return;
            float dt = Time.deltaTime; // game time: nothing happens while paused
            if (dt <= 0f) return;
            if (_enemy.IsFrozen) _wasFrozen = true;
            else if (_wasFrozen) { _wasFrozen = false; Hold(); }
            if (!_timed) return;
            RemainingSeconds -= dt;
            if (RemainingSeconds <= 0f) Leave();
        }

        /// <summary>Its time is up: back to the pool without drops.</summary>
        public void Leave()
        {
            if (IsLeaving) return;
            IsLeaving = true;
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.EnemyDeath, transform.position + Vector3.up * 0.8f);
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _enemy = null;
            Destroy(this);
        }
    }
}
