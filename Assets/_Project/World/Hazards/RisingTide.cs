using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>
    /// The Rising Tide (Omens; VS Random Event "The Drowner", vampire.survivors.wiki/w/Random_Events and /w/The_Drowner,
    /// raw, 2026-10-13: "Summons the Drowner, who fills the stage with damaging water from the bottom up, for ~1 minute";
    /// "appears on the bottom-left corner and starts flooding the lower area of the map, slowly engulfing it as the water
    /// rises over time. Entering the water will cause the player to rapidly take 20 damage per game-tick. Freezing the
    /// Drowner also causes the flood to stop rising. The flood will disappear if Drowner is defeated or if its set
    /// duration expires").
    ///
    /// The Tide Caller (Soul Hunter's Drowner) is the level's boss model, tinted blue, standing still at the bottom-left
    /// of the screen where it was summoned. Its statbox: "health = 800 - 1250", "HP x Level", "resistances = Freeze,
    /// Debuff, Knockback", so 800 x the hero's level and no knockback. The water is everything below a line across the
    /// world that starts at the screen's bottom edge and rises.
    ///
    /// Soul Hunter's numbers, the wiki gives none (balance method):
    /// - the line rises one screen height over the event (<see cref="StageHazardTable.TideSeconds"/>), so a hero who stands
    ///   still is reached at about half time and one who walks up the screen stays dry;
    /// - a "game-tick" is not defined: the damage lands every <see cref="StageHazardTable.TideTickSeconds"/>, the game's own
    ///   contact-damage interval;
    /// - the flood does not hurt enemies (the page speaks of the player only).
    /// Its killers (owner, 2026-10-19: "SH = VS"; /w/The_Drowner, raw): ordinary damage defeats it (HP x Level) with no
    /// chest; it is "unaffected by Pentagram"; Eclipse Sigil, Sundered Hour, Crimson Shroud and Soul Toll kill it at once
    /// ("instantly killed upon taking damage from Gorgeous Moon, Infinite Corridor, Crimson Shroud, ... Rosary") and then it
    /// drops a Treasure Chest ("Defeating it with instant-kill effects will make it drop a Treasure Chest",
    /// {{treasure|evo=1|tier3=6|tier2=66|tier1=100|reward4=evo|reward5=evo}}, the Stalker's high-quality chest). Its
    /// resistances are the statbox's: freeze, debuff and knockback, not instant kill. A frozen Tide Caller holds the water.
    /// </summary>
    public class RisingTide : MonoBehaviour
    {
        private static readonly Color WaterColour = new Color(0.16f, 0.42f, 0.85f, 0.42f);
        private static readonly Color CallerTint = new Color(0.35f, 0.6f, 1f, 1f);
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private EnemyController _caller;
        private HealthController _callerHealth;
        private float _callerKnockbackBefore;
        private System.Func<DamagePacket, bool> _callerInterceptorBefore;
        private bool _chestGiven;

        /// <summary>The chest the last Tide Caller left (null when it was not defeated by one of its killers; tests).</summary>
        public static SoulHunter.Gameplay.Core.ChestTiers? LastChest { get; private set; }
        private Transform _water;
        private float _riseSpeed;
        private float _tick;
        private float _left;

        /// <summary>The world Z of the water's edge: everything below (smaller Z) is flooded.</summary>
        public float WaterLine { get; private set; }
        public float RemainingSeconds => _left;
        public EnemyController Caller => _caller;
        public int Ticks { get; private set; }
        public bool IsOver { get; private set; }

        /// <param name="callerObject">The Tide Caller's body (a pooled enemy, already placed), or null for water alone.</param>
        public static RisingTide Begin(GameObject callerObject, float startLine, float riseSpeed, float seconds, int playerLevel) =>
            Begin(callerObject, startLine, riseSpeed, seconds, playerLevel, StageHazardTable.TideCallerHealthPerLevel, 0f);

        /// <param name="healthPerLevel">The caller's HP x Level (the Omen's 800; Cappella Magna's Drowner 2550).</param>
        /// <param name="vsXp">The caller's xp (Cappella's Drowner: 50), left as a gem when it is defeated.</param>
        public static RisingTide Begin(GameObject callerObject, float startLine, float riseSpeed, float seconds, int playerLevel,
            int healthPerLevel, float vsXp)
        {
            var go = new GameObject("RisingTide");
            var tide = go.AddComponent<RisingTide>();
            tide.WaterLine = startLine;
            tide._riseSpeed = Mathf.Max(0f, riseSpeed);
            tide._left = Mathf.Max(1f, seconds);
            tide._tick = StageHazardTable.TideTickSeconds;
            tide._vsXp = vsXp;
            tide.BuildWater();
            if (callerObject != null)
            {
                tide._caller = callerObject.GetComponent<EnemyController>();
                tide._callerHealth = callerObject.GetComponent<HealthController>();
                StillEnemy.Begin(callerObject);
                LastChest = null;
                if (tide._caller != null)
                {
                    // The statbox's resistances (freeze, debuff, knockback), not the boss model's instant-kill resistance,
                    // so its instant killers reach it; and Pentagram passes it by.
                    tide._caller.OverrideResistances(tide._caller.FreezeResistance, false, true);
                    tide._caller.IgnoresPentagram = true;
                }
                if (tide._callerHealth != null)
                {
                    tide._callerHealth.Initialize(Mathf.Max(1, healthPerLevel) * Mathf.Max(1, playerLevel)); // the spawner sets it again on reuse
                    tide._callerKnockbackBefore = tide._callerHealth.KnockbackResistance;
                    tide._callerHealth.KnockbackResistance = 1f;
                    tide._callerInterceptorBefore = tide._callerHealth.DamageInterceptor;
                    tide._callerHealth.DamageInterceptor = tide.InterceptCaller; // Crimson Shroud and Sundered Hour hits
                    tide._callerHealth.OnDied += tide.HandleCallerDied;          // Eclipse Sigil and Soul Toll erase it
                }
                var drop = callerObject.GetComponent<EnemyDrop>();
                if (drop != null)
                {
                    drop.EraseDropsThisLife(); // the statbox lists no XP
                    // No chest either (Bug MIS AI-03): the body is the level's boss model, whose data drops a chest, and an
                    // erased carrier passes its chest to the next enemy, so killing the caller gave a free chest.
                    // vampire.survivors.wiki/w/Random_Events (raw, 2026-10-19) says nothing about a chest from the event's
                    // Drowner. /w/The_Drowner: the Gallo Tower Drowner, which only instant kills defeat, drops one ("Defeating
                    // it with instant-kill effects will make it drop a Treasure Chest"), while the Cappella Magna / Boss Rash
                    // Drowner "can be defeated with Pentagram and defeating it won't make it drop a Treasure Chest". The Tide
                    // Caller dies to any damage, Pentagram included, like the second one, so it drops none (balance method).
                    drop.NoChestThisLife();
                }
                var flash = callerObject.GetComponentInChildren<SoulHunter.Gameplay.VFX.DamageFlash>();
                if (flash != null) flash.SetTint(CallerTint);
            }
            return tide;
        }

        private void BuildWater()
        {
            var quad = new GameObject("Water");
            quad.transform.SetParent(transform, false);
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var shader = Shader.Find("SoulHunter/VFX/Particle");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var material = _material = new Material(shader); // destroyed with the tide (OnDestroy)
            material.SetColor(ColorId, WaterColour);
            material.SetFloat("_SrcBlend", 5f);  // SrcAlpha
            material.SetFloat("_DstBlend", 10f); // OneMinusSrcAlpha: see-through water, not a glow
            var renderer = quad.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _water = quad.transform;
            PlaceWater();
        }

        /// <summary>The sheet covers the ground below the line, wide and deep enough for any view, and follows the hero sideways.</summary>
        private void PlaceWater()
        {
            if (_water == null) return;
            var player = PlayerController.Instance;
            float x = player != null ? player.transform.position.x : 0f;
            float y = player != null ? player.transform.position.y - 0.9f : 0.1f; // just above the floor, under the bodies
            _water.localScale = new Vector3(StageHazardTable.TideSheetWidth, StageHazardTable.TideSheetDepth, 1f);
            _water.position = new Vector3(x, y, WaterLine - StageHazardTable.TideSheetDepth * 0.5f);
        }

        /// <summary>True when a point on the ground is under water.</summary>
        public bool Floods(Vector3 point) => !IsOver && point.z < WaterLine;

        private void Update()
        {
            if (IsOver) return;
            float dt = Time.deltaTime; // game time: a pause holds the tide
            if (dt <= 0f) return;
            if (_caller != null && (!_caller.isActiveAndEnabled || _callerHealth == null || _callerHealth.CurrentHealth <= 0)) { End(); return; }
            _left -= dt;
            if (_left <= 0f) { End(); return; }
            bool held = _caller != null && _caller.IsFrozen; // "Freezing the Drowner also causes the flood to stop rising"
            if (!held) WaterLine += _riseSpeed * dt;
            PlaceWater();

            var player = PlayerController.Instance;
            if (player == null || !Floods(player.transform.position)) { _tick = 0f; return; }
            _tick -= dt;
            if (_tick > 0f) return;
            _tick = StageHazardTable.TideTickSeconds;
            var health = player.GetComponent<HealthController>();
            if (health == null) return;
            Ticks++;
            health.TakeDamage(new DamagePacket(StageHazardTable.TideDamage, player.transform.position, Vector3.zero));
        }

        /// <summary>The caller's body is pooled: its hooks, resistances and Pentagram immunity go back to ordinary.</summary>
        private void ReleaseCaller()
        {
            if (_callerHealth != null)
            {
                _callerHealth.KnockbackResistance = _callerKnockbackBefore;
                _callerHealth.OnDied -= HandleCallerDied;
                if (_callerHealth.DamageInterceptor == (System.Func<DamagePacket, bool>)InterceptCaller) _callerHealth.DamageInterceptor = _callerInterceptorBefore;
            }
            if (_caller != null)
            {
                _caller.IgnoresPentagram = false;
                _caller.ClearResistanceOverrides();
            }
        }

        // Destroyed without End (a scene change): the pooled body must not keep this tide's hooks.
        private Material _material;

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material); // bug hunt 2026-10-29: every tide leaked its water material
            if (!IsOver) ReleaseCaller();
        }

        /// <summary>A hit from Crimson Shroud or Sundered Hour kills it at once; any other hit is ordinary damage.</summary>
        private bool InterceptCaller(DamagePacket packet)
        {
            if (_callerInterceptorBefore != null && _callerInterceptorBefore(packet)) return true;
            var killer = StageHazardTable.KillerOf(DamageSource.Weapon, DamageSource.Name);
            if (!StageHazardTable.KillsTideCaller(killer) || _callerHealth == null) return false;
            GiveChest();
            _callerHealth.EraseByInstantKill();
            return true;
        }

        /// <summary>Its death: a chest when one of its instant killers did it (the erase arrives inside their DamageSource).</summary>
        private float _vsXp;

        private void HandleCallerDied()
        {
            // Cappella Magna's Drowner has xp 50: it leaves that gem whatever defeated it (its drops are erased above).
            var pickups = SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance;
            if (_vsXp > 0f && pickups != null && _caller != null) pickups.SpawnGem(_caller.transform.position, Mathf.RoundToInt(_vsXp * 10f));
            if (StageHazardTable.KillsTideCaller(StageHazardTable.KillerOf(DamageSource.Weapon, DamageSource.Name))) GiveChest();
        }

        private void GiveChest()
        {
            if (_chestGiven || _caller == null) return;
            _chestGiven = true;
            LastChest = SoulHunter.Gameplay.Core.ChestTiers.StalkerHighQuality;
            var pickups = SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance;
            var chestObject = pickups != null ? pickups.SpawnChest(_caller.transform.position) : null;
            var chest = chestObject != null ? chestObject.GetComponent<SoulHunter.Gameplay.Pickups.ChestPickup>() : null;
            if (chest != null) chest.Tiers = SoulHunter.Gameplay.Core.ChestTiers.StalkerHighQuality;
        }

        /// <summary>The flood is over (time up, the Tide Caller defeated, or the stage changed): the caller leaves without drops.</summary>
        public void End()
        {
            if (IsOver) return;
            IsOver = true;
            ReleaseCaller();
            if (_caller != null)
            {
                var flash = _caller.GetComponentInChildren<SoulHunter.Gameplay.VFX.DamageFlash>(true);
                if (flash != null) flash.SetTint(_caller.TypeTint); // the pooled body is ordinary again
            }
            if (_caller != null && _caller.isActiveAndEnabled)
            {
                var still = _caller.GetComponent<StillEnemy>();
                if (still != null) still.Leave(); else _caller.gameObject.SetActive(false);
            }
            Destroy(gameObject);
        }
    }
}
