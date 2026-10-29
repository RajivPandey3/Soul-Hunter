using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// One member of a Powder Ring (Omens; VS Random Event "Roulette", vampire.survivors.wiki/w/Random_Events, raw,
    /// 2026-10-13: "Spawns a ring of poltergeists that self-destruct when the player approaches them"). The pooled stage
    /// enemy stands still; when the hero comes within <see cref="StageHazardTable.MineArmDistance"/> it arms, shows its
    /// blast circle and explodes after the fuse, hurting the hero inside the circle. The page gives no numbers, so they
    /// are the ones Soul Hunter already uses for a self-destructing enemy (EnemyAbilityDefaults, from VS Sig.ra Rossi:
    /// "exploding 2 seconds later for 50 damage"). A frozen member holds its fuse. An exploded member leaves without
    /// drops and is not a kill; it can be killed first like any enemy. The ring's survivors leave when its time is up.
    /// </summary>
    [DisallowMultipleComponent]
    public class MineSentry : MonoBehaviour, IHoldsPlace, ILeavesWithItsLife
    {
        private static readonly Color FuseTint = new Color(1f, 0.45f, 0.2f, 0.75f);

        public float RemainingSeconds { get; set; }
        public bool IsArmed { get; private set; }
        public bool Exploded { get; private set; }
        public bool IsLeaving { get; private set; }
        public float FuseLeft { get; private set; }

        private EnemyController _enemy;
        private EnemyAbilityZone _marker;
        private bool _wasFrozen;

        public static MineSentry Begin(GameObject enemyObject, float durationSeconds)
        {
            if (enemyObject == null) return null;
            var mine = LifeComponent.Fresh<MineSentry>(enemyObject);
            mine._enemy = enemyObject.GetComponent<EnemyController>();
            mine.RemainingSeconds = Mathf.Max(0.1f, durationSeconds);
            mine.IsArmed = false;
            mine.Exploded = false;
            mine.IsLeaving = false;
            mine.Hold();
            // Bug MIS AI-06 (wiki Orologion: "new enemies will also become frozen"): a member spawned during an Orologion
            // was frozen by SpawnEnemy, and the hold replaced that; it stays frozen for the rest of it, then holds again.
            EnemySpawner.KeepWorldFreeze(mine._enemy);
            return mine;
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
            if (_enemy.IsFrozen) { _wasFrozen = true; return; } // a frozen member holds its fuse and its time
            if (_wasFrozen) { _wasFrozen = false; Hold(); }

            if (IsArmed)
            {
                FuseLeft -= dt;
                if (FuseLeft <= 0f) Explode();
                return;
            }
            RemainingSeconds -= dt;
            if (RemainingSeconds <= 0f) { Leave(); return; }
            var player = PlayerController.Instance;
            if (player == null) return;
            Vector3 d = player.transform.position - transform.position;
            d.y = 0f;
            if (d.magnitude <= StageHazardTable.MineArmDistance) Arm();
        }

        /// <summary>Starts the fuse (the hero came close; also used by tests).</summary>
        public void Arm()
        {
            if (IsArmed || IsLeaving) return;
            IsArmed = true;
            FuseLeft = StageHazardTable.MineFuseSeconds;
            _marker = EnemyAbilityZone.CreateMarker(transform.position, StageHazardTable.MineBlastRadius, FuseTint, transform);
        }

        private void Explode()
        {
            Exploded = true;
            EnemyAbilityZone.DamagePlayerInside(transform.position, StageHazardTable.MineBlastRadius, StageHazardTable.MineDamage, false);
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.Explosion, transform.position + Vector3.up * 0.3f);
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFXThrottled(audio.ExplosionSound, 0.08f, 0.6f);
            IsLeaving = true;
            gameObject.SetActive(false); // back to the pool: no drops, no kill
        }

        /// <summary>The ring's time is up: back to the pool without drops.</summary>
        public void Leave()
        {
            if (IsLeaving) return;
            IsLeaving = true;
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.EnemyDeath, transform.position + Vector3.up * 0.8f);
            gameObject.SetActive(false);
        }

        public bool Doomed { get; private set; }

        private void OnDisable()
        {
            if (Doomed) return;
            Doomed = true;
            if (_marker != null) Destroy(_marker.gameObject);
            _marker = null;
            _enemy = null;
            Destroy(this);
        }
    }
}
