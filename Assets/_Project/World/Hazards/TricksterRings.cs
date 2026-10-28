using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// The Trickster's attack (vampire.survivors.wiki/w/The_Trickster, raw 2026-10-28): "periodically summons ellipsoid
    /// rings of 48 Poltergeist Gems around the player that self-destruct when approached. The gems lasts for 30 seconds";
    /// "two rings upon spawning and one every 30 seconds thereafter"; "randomly ... either a large or a small ring ... The
    /// gems in the larger ring remain stationary, however, the gems in the smaller ring will home at the player"; "When The
    /// Trickster dies, all remaining Poltergeist Gems explode".
    /// The gems are Poltergeist Gem (homing, speed 150) and Poltergeist Gem (static) statbox types: 5 x Level, damage 3,
    /// self-destruct 2 s for 50. Ring sizes are Soul Hunter's (StageHazardTable).
    /// </summary>
    public class TricksterRings : MonoBehaviour
    {
        private DeathsShadow _trickster;
        private GameObject _homingGem, _stillGem;
        private readonly List<GameObject> _gems = new List<GameObject>();
        private float _nextRing;
        // Gems still to make: a ring's 48 are made 16 a frame (two rings in one frame took 13 + 11 ms, 2026-10-28).
        private readonly Queue<(GameObject prefab, Vector3 at, bool still)> _pending = new Queue<(GameObject, Vector3, bool)>();
        public const int GemsPerFrame = 16;
        public int GemsPending => _pending.Count;

        public int RingsSummoned { get; private set; }
        public int GemsAlive { get { int n = 0; foreach (var g in _gems) if (g != null && g.activeInHierarchy) n++; return n; } }
        public int GemsExploded { get; private set; }

        public static TricksterRings Begin(DeathsShadow trickster, GameObject homingGem, GameObject stillGem)
        {
            if (trickster == null || (homingGem == null && stillGem == null)) return null;
            var rings = new GameObject("TricksterRings").AddComponent<TricksterRings>();
            rings._trickster = trickster;
            rings._homingGem = homingGem;
            rings._stillGem = stillGem;
            rings.SummonRing();
            rings.SummonRing();
            rings._nextRing = StageHazardTable.TricksterRingEverySeconds;
            return rings;
        }

        private bool TricksterHere => _trickster != null && _trickster.isActiveAndEnabled && !_trickster.IsLeaving;

        private void Update()
        {
            _gems.RemoveAll(g => g == null || !g.activeInHierarchy);
            if (!TricksterHere)
            {
                // Its death sets the gems off; leaving at the end of its time does not (they keep their own 30 s).
                bool died = _trickster != null && (_trickster.WornDown || _trickster.DefeatedBy != ShadowKiller.None);
                if (died) ExplodeAll();
                Destroy(gameObject);
                return;
            }
            float dt = Time.deltaTime; // game time
            if (dt <= 0f) return; // nothing is made under a pause
            using (SoulHunter.Core.Services.SlowSpan.Measure("Trickster.Gems")) MakePending();
            _nextRing -= dt;
            if (_nextRing <= 0f)
            {
                _nextRing = StageHazardTable.TricksterRingEverySeconds;
                SummonRing();
            }
        }

        /// <summary>One ring of 48 gems round the player: a large still ring or a small homing one, at random.</summary>
        public void SummonRing(bool? large = null)
        {
            var player = PlayerController.Instance;
            if (player == null) return;
            bool big = large ?? Random.value < 0.5f;
            var prefab = big ? (_stillGem != null ? _stillGem : _homingGem) : (_homingGem != null ? _homingGem : _stillGem);
            float scale = big ? 1f : StageHazardTable.TricksterSmallRingScale;
            Vector3 centre = player.transform.position;
            for (int i = 0; i < StageHazardTable.TricksterGems; i++)
            {
                float angle = Mathf.PI * 2f * i / StageHazardTable.TricksterGems;
                Vector3 at = centre + new Vector3(Mathf.Cos(angle) * StageHazardTable.JellyRadiusX, 0f, Mathf.Sin(angle) * StageHazardTable.JellyRadiusZ) * scale;
                _pending.Enqueue((prefab, at, big));
            }
            RingsSummoned++;
        }

        /// <summary>Makes up to <see cref="GemsPerFrame"/> of the queued gems, at the places their ring gave them.</summary>
        public void MakePending()
        {
            if (_pending.Count == 0) return;
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner == null) { _pending.Clear(); return; }
            for (int n = 0; n < GemsPerFrame && _pending.Count > 0; n++)
            {
                var (prefab, at, still) = _pending.Dequeue();
                var gem = spawner.SpawnHazardEnemy(prefab, at);
                if (gem == null) { _pending.Clear(); return; } // the 500 hard cap
                if (still) StillEnemy.Begin(gem, StageHazardTable.TricksterGemSeconds);
                else EventLifetime.Begin(gem, StageHazardTable.TricksterGemSeconds);
                _gems.Add(gem);
            }
        }

        /// <summary>Every gem left explodes at once: 50 to the hero within the blast (Armor applies), then it is gone.</summary>
        public void ExplodeAll()
        {
            var player = PlayerController.Instance;
            var health = player != null ? player.GetComponent<HealthController>() : null;
            _pending.Clear(); // gems not made yet never appear
            bool hit = false;
            foreach (var gem in _gems)
            {
                if (gem == null || !gem.activeInHierarchy) continue;
                Vector3 at = gem.transform.position;
                SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.Explosion, at + Vector3.up * 0.5f);
                if (!hit && health != null && player != null)
                {
                    Vector3 d = player.transform.position - at;
                    d.y = 0f;
                    if (d.magnitude <= StageHazardTable.GemBlastRadius)
                    {
                        hit = true; // one blast's damage, as overlapping blasts of one moment hurt once
                        health.TakeDamage(new DamagePacket(StageHazardTable.GemBlastDamage, at, d.normalized));
                    }
                }
                gem.SetActive(false); // gone without drops
                GemsExploded++;
            }
            _gems.Clear();
        }
    }
}
