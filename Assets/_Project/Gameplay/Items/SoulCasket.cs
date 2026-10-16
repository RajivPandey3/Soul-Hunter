using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// A Soul Casket, Soul Hunter's coffin (CasketBook; vampire.survivors.wiki/w/Coffin, raw, fetched 2026-10-12): "Upon
    /// arriving at the coffin, an expanding and contracting ring of enemies appears around it. Once all of these enemies
    /// are defeated, the player can open the coffin by moving over it."
    /// - Guards: when the hero comes within <see cref="GuardWakeDistance"/>, a ring of <see cref="GuardCount"/> of the
    ///   level's elites spawns around the casket. The wiki gives neither the count nor the ring's size; balance method: 8,
    ///   one every 45 degrees, at <see cref="GuardRingRadius"/> units, so the ring is closed but a weapon can reach it.
    ///   The VS ring's expanding and contracting motion is not built: the guards chase like any elite.
    /// - A guard counts as defeated when it dies or leaves the field in any other way (erased by a Pentagram, pooled).
    /// - With every guard down the casket glows (the approved level-up magic circle, repeated, and its soul seam lit) and
    ///   walking over it opens it.
    /// The look is the approved Soul Reliquary chest, darkened by property blocks (as ChestLook recolours it): a
    /// placeholder, not owner-approved art.
    /// </summary>
    public sealed class SoulCasket : MonoBehaviour, SoulHunter.Gameplay.IMapMarker
    {
        public const float GuardWakeDistance = 12f, GuardRingRadius = 6f, OpenDistance = 1.6f, GlowEverySeconds = 1.5f;
        public const int GuardCount = 8;

        /// <summary>Darkens every chest colour; the soul seam is dimmed while guarded and lit when the casket can be opened.</summary>
        public static readonly Color DarkTint = new Color(0.32f, 0.28f, 0.42f, 1f);
        public static readonly Color GuardedSeam = new Color(0.25f, 0.05f, 0.08f, 1f);
        public static readonly Color ReadySeam = new Color(1f, 0.82f, 0.35f, 1f);

        /// <summary>
        /// Raised once when a casket is opened in a run, with the CharacterName of the character it holds (also when that
        /// character was already unlocked by its own condition).
        /// </summary>
        public static event System.Action<string> CasketOpened;

        public CasketInfo Info { get; private set; }
        public bool GuardsSpawned { get; private set; }
        /// <summary>Every guard is down: the casket glows and can be opened.</summary>
        public bool IsReady { get; private set; }
        public bool IsOpened { get; private set; }
        /// <summary>True when opening it unlocked the character (false: the character was unlocked already).</summary>
        public bool FreedCharacter { get; private set; }
        /// <summary>Guards still on the field.</summary>
        public int GuardsLeft
        {
            get
            {
                int n = 0;
                foreach (var guard in _guards) if (!guard.Down) n++;
                return n;
            }
        }
        /// <summary>The guards this casket spawned (tests).</summary>
        public IReadOnlyList<GameObject> Guards
        {
            get
            {
                var list = new List<GameObject>();
                foreach (var guard in _guards) list.Add(guard.Body);
                return list;
            }
        }
        /// <summary>The soul seam's glow now (tests): the guarded or the ready colour.</summary>
        public Color SeamColour { get; private set; } = GuardedSeam;

        // IMapMarker: the pause map and the guide arrows find it through this.
        public string MapKind => "Casket";
        public Vector3 MapPosition => transform.position;
        public bool MapVisible => !IsOpened;

        private sealed class Guard
        {
            public GameObject Body;
            public HealthController Health;
            public System.Action Died;
            public bool Down;
        }

        private readonly List<Guard> _guards = new List<Guard>();
        private Transform _visual;
        private float _nextGlow;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static MaterialPropertyBlock _block;

        public static SoulCasket Create(CasketInfo info, Vector3 position)
        {
            var go = new GameObject("SoulCasket_L" + info.Level);
            go.transform.position = position;
            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = OpenDistance;
            trigger.center = new Vector3(0f, 0.8f, 0f);
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var casket = go.AddComponent<SoulCasket>();
            casket.Info = info;
            casket.BuildVisual();
            return casket;
        }

        private void BuildVisual()
        {
            var pool = PickupPoolManager.Instance;
            var chest = pool != null ? pool.ChestPrefab : null;
            var source = chest != null ? chest.transform.Find("Visual") : null;
            if (source == null) return; // no chest model (a bare test scene): the casket still works, unseen
            _visual = Instantiate(source.gameObject, transform).transform;
            _visual.name = "Visual";
            _visual.localPosition = Vector3.zero;
            _visual.localScale = source.localScale * 1.5f;
            // Only the model is wanted: no pickup scripts, no colliders of the chest.
            // PropMergedLook stays: ApplyLook needs it to find a merged mesh's soul seam (it runs again when the casket is ready).
            foreach (var behaviour in _visual.GetComponentsInChildren<MonoBehaviour>(true))
                if (!(behaviour is SoulHunter.Gameplay.VFX.PropMergedLook)) Destroy(behaviour);
            foreach (var collider in _visual.GetComponentsInChildren<Collider>(true)) Destroy(collider);
            ApplyLook();
        }

        /// <summary>The dark casket look; the soul seam shows whether it is guarded or ready.</summary>
        private void ApplyLook()
        {
            SeamColour = IsReady ? ReadySeam : GuardedSeam;
            if (_visual == null) return;
            if (_block == null) _block = new MaterialPropertyBlock();
            foreach (var renderer in _visual.GetComponentsInChildren<Renderer>(true))
            {
                // A merged chest mesh (PropMeshMerger: one PropMerged material, every part's look baked in) is darkened
                // through the shader's colour multiplier, which acts like the old per-material tint on every baked part
                // (iron bands included); its soul seam (mask B) then takes the seam colour and glow.
                if (renderer.TryGetComponent(out SoulHunter.Gameplay.VFX.PropMergedLook _))
                {
                    _block.Clear();
                    _block.SetColor(BaseColor, DarkTint);
                    renderer.SetPropertyBlock(_block);
                    if (SoulHunter.Gameplay.VFX.PropMergedLook.TryFind(renderer, "Chest_Soul", out var seam))
                        SoulHunter.Gameplay.VFX.PropMergedLook.Set(seam, _block, SeamColour, SeamColour * (IsReady ? 3f : 1f));
                    continue;
                }
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null) continue;
                    _block.Clear();
                    if (material.name.Contains("Chest_Soul"))
                    {
                        _block.SetColor(BaseColor, SeamColour);
                        _block.SetColor(EmissionColor, SeamColour * (IsReady ? 3f : 1f));
                    }
                    else if (material.HasProperty(BaseColor))
                    {
                        _block.SetColor(BaseColor, material.GetColor(BaseColor) * DarkTint);
                    }
                    renderer.SetPropertyBlock(_block, i);
                }
            }
        }

        private void Update()
        {
            if (IsOpened) return;
            var player = PlayerController.Instance;
            if (player == null) return;
            Vector3 to = player.transform.position - transform.position;
            to.y = 0f;
            if (!GuardsSpawned)
            {
                if (to.sqrMagnitude <= GuardWakeDistance * GuardWakeDistance) SpawnGuards();
                return;
            }
            if (!IsReady)
            {
                bool allDown = true;
                foreach (var guard in _guards)
                {
                    if (!guard.Down && (guard.Body == null || !guard.Body.activeInHierarchy)) MarkDown(guard);
                    if (!guard.Down) allDown = false;
                }
                if (allDown) BecomeReady();
                return;
            }
            // Ready: the glow repeats so the player sees it can be opened (real time: it also shows on a paused screen's edge).
            if (Time.unscaledTime >= _nextGlow)
            {
                _nextGlow = Time.unscaledTime + GlowEverySeconds;
                SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.LevelUp, transform.position + Vector3.up * 0.1f);
            }
            // The hero may already stand on it when the last guard falls (no new trigger enter then).
            if (to.sqrMagnitude <= OpenDistance * OpenDistance) Open();
        }

        /// <summary>Spawns the guard ring now (also called by tests).</summary>
        public void SpawnGuards()
        {
            if (GuardsSpawned) return;
            GuardsSpawned = true;
            var spawner = FindFirstObjectByType<SoulHunter.Gameplay.AI.EnemySpawner>();
            var prefab = spawner != null ? spawner.StageElitePrefab : null;
            if (prefab != null)
                for (int i = 0; i < GuardCount; i++)
                {
                    Vector3 at = transform.position + Quaternion.Euler(0f, i * (360f / GuardCount), 0f) * Vector3.forward * GuardRingRadius;
                    var body = spawner.SpawnHazardEnemy(prefab, at);
                    if (body == null) continue; // the enemy cap is full: fewer guards
                    var guard = new Guard { Body = body, Health = body.GetComponent<HealthController>() };
                    if (guard.Health != null)
                    {
                        guard.Died = () => MarkDown(guard);
                        guard.Health.OnDied += guard.Died;
                    }
                    _guards.Add(guard);
                }
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null && _guards.Count > 0) audio.PlaySFX(audio.BossWarningSound, 0.7f);
            // No guard could be spawned (no spawner in a test scene): nothing is left to defeat.
            if (_guards.Count == 0) BecomeReady();
        }

        private void MarkDown(Guard guard)
        {
            if (guard.Down) return;
            guard.Down = true;
            if (guard.Health != null && guard.Died != null) guard.Health.OnDied -= guard.Died;
            guard.Died = null;
        }

        private void BecomeReady()
        {
            if (IsReady) return;
            IsReady = true;
            _nextGlow = 0f;
            ApplyLook();
        }

        private void OnDestroy()
        {
            foreach (var guard in _guards)
                if (guard.Health != null && guard.Died != null) guard.Health.OnDied -= guard.Died;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsReady || IsOpened) return;
            if (!other.CompareTag("Player") && other.GetComponentInParent<PlayerController>() == null) return;
            Open();
        }

        /// <summary>
        /// Opens a ready casket: its character is unlocked on the save (or, if it already was, the casket pays
        /// CasketBook.AlreadyFreeGold), the run's Results list it, and the unlock notice says what happened. Returns false
        /// while guards remain or when it is already open.
        /// </summary>
        public bool Open()
        {
            if (!IsReady || IsOpened) return false;
            IsOpened = true;
            SaveService save = null;
            GameServices.Instance?.TryGet(out save);
            var data = save != null ? save.CurrentData : null;
            var player = PlayerController.Instance;
            string line = null;
            if (data != null && CasketBook.Open(data, Info, out bool freed))
            {
                FreedCharacter = freed;
                if (freed) line = $"Soul Casket opened. New character unlocked: {Info.Character}!";
                else
                {
                    int gold = GoldRewards.Grant(CasketBook.AlreadyFreeGold, player != null ? player.Stats : null);
                    line = $"Soul Casket opened. {Info.Character} was already unlocked: +{gold} gold.";
                }
            }
            // Results ("pickups and the amount of them") and, through CollectionTracker, the casket's Collection entry.
            var stats = SoulHunter.Gameplay.Core.RunStatsTracker.Instance;
            if (stats != null) stats.RecordPickup(CasketBook.PickupName);
            var session = SoulHunter.Gameplay.Core.GameSessionManager.Instance;
            var tracker = session != null ? session.GetComponent<SoulHunter.Gameplay.Core.CharacterUnlockTracker>() : null;
            if (line != null)
            {
                if (tracker != null) tracker.AnnounceUnlock(line);
                else Debug.Log($"[SoulCasket] {line}");
            }
            if (save != null && data != null) save.SaveInBackground();
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.ChestOpen, transform.position + Vector3.up * 0.6f);
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.ChestOpenSound, 0.9f);
            CasketOpened?.Invoke(Info.Character);
            Destroy(gameObject, 0.6f);
            return true;
        }
    }
}
