using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Hazards;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// The Omens events (Soul Hunter's own names; release item P3-05). Each maps one VS Random Event (vampire.survivors.wiki/w/
    /// Random_Events, raw page re-fetched 2026-10-09) onto a system Soul Hunter already has. VS events with no Soul Hunter
    /// counterpart are left out (see <see cref="OmenTable"/>). New values go at the end.
    /// </summary>
    public enum OmenEvent
    {
        /// <summary>VS Pet: "Creates a big ring of hearts around the player, each healing them for 2 hit points."</summary>
        MendingRing,
        /// <summary>VS Chicken Bonanza: "Creates a ring of Floor Chickens around the player, each one healing for 30 hit points."</summary>
        FeastRing,
        /// <summary>VS Firebreathing: "Gives the player the Nduja Fritta Tanto powerup" (Soul Hunter's Hellfire Breath).</summary>
        HellfireBreath,
        /// <summary>VS Gold Fever: "Starts a Gold Fever."</summary>
        GildedFever,
        /// <summary>VS Pile Assault: "Summons a ring of Twin Snakes around the player that shoot fireballs at them ... Disappears after 1 minute."</summary>
        GallowsRing,
        /// <summary>VS Rosary: "Activates the Rosary effect, killing every enemy on screen, unless they are immune to it." (Soul Toll)</summary>
        TollOfSouls,
        /// <summary>VS Free Passive: "Puts a random passive item near the player."</summary>
        WanderingRelic,
        /// <summary>VS Light Sources: "creates several Light Sources around the player."</summary>
        LanternBloom,
        /// <summary>VS The Stalker: "Summons the Stalker that slowly follows the player for ~2 minutes." Stages 5 and 9 only (owner).</summary>
        DeathsShadow,
        /// <summary>VS Ultra Wave: "Increases the enemy spawn rate for 1 minute."</summary>
        SwellingHorde,
        /// <summary>VS Shooting Stars: "Summons groups of shooting stars at the player." (Soul Meteors)</summary>
        SoulMeteors,
        /// <summary>VS Luck Boost: "Permanently increases the player's luck by 50%."</summary>
        FortunesFavour,
        /// <summary>VS Jelly Wall: "Summons a ring of Jellyfish surrounding the player that closes in and moves away from them."</summary>
        ClosingRing,
        /// <summary>VS Skull, Jelly, Merdusa, Green, Ghost and Imp Swarms: a swarm "that fly toward the player in a fixed direction".</summary>
        HordeSwarm,
        /// <summary>VS Bat Swarm: "The player gets rushed by several swarms of small bats ... for ~30 seconds."</summary>
        SwarmRush,
        /// <summary>VS Skeleton Swarm: "Summons a swarm of Skeletons that move toward the player in a fixed direction."</summary>
        MarchingLine,
    }

    /// <summary>
    /// The Omens timetable and table, as pure functions (OmenTableTests). VS (wiki Stages, Randomizers): "At the 30-second
    /// mark of every minute, the roulette selects a random event, which then triggers at the start of the following minute";
    /// Random_Events: "This happens at every XX:30 mark, where the three rings will start rotating and selecting a random
    /// event, which will then trigger at the next XX:00 mark." Hurry (wiki Stages, Hurry Mode): "All time-based events ...
    /// occur at their usual timestamps on the in-game timer", so the marks are on the stage clock.
    ///
    /// Left out, with the reason (each needs an enemy, item or hazard Soul Hunter does not have):
    /// - Candybox (a Candybox item and its pick-any-weapon screen);
    /// - Mino Rush (Mignotaurs), Scorpion Lair (big Twin Skulls), Crab Fest (Non-Giant Enemy Crabs), Shrimp Swarm (weaving
    ///   Serpentine Dragon Shrimps): VS-only enemies and flights;
    /// - Panic Time and Roulette (Poltergeists and Red Shades; self-destructing Poltergeists);
    /// - Summon II Molise and Summon Night (every enemy replaced by Il Molise plants or by bats);
    /// - The Drowner (rising damaging water);
    /// - Flower Wall (a still wall of plants that slowly closes for ~50 s; Soul Hunter's closing ring flies through and
    ///   out, which is Jelly Wall, built as Closing Ring);
    /// - The Stalker outside Stages 5 and 9: the owner placed Death's Shadow on those stages only (2026-10-09).
    /// </summary>
    public static class OmenTable
    {
        /// <summary>The seconds between marks: XX:30 picks, XX:00 fires.</summary>
        public const float MarkSeconds = 30f;
        /// <summary>
        /// A mark the clock jumped more than this far past (the Debug "skip to" tools) is dropped, as EnemySpawner drops
        /// stage events skipped more than 10 s past.
        /// </summary>
        public const float StaleSeconds = 10f;

        /// <summary>Owner placement (2026-09-27 hazards, kept for Omens 2026-10-09): Death's Shadow only on Stages 5 and 9.</summary>
        public static bool ShadowStage(int stage) => stage == 5 || stage == 9;

        /// <summary>Events that send a formation through EnemySpawner (refused by the spawner while a stage boss lives).</summary>
        public static bool IsFormation(OmenEvent e) =>
            e == OmenEvent.ClosingRing || e == OmenEvent.HordeSwarm || e == OmenEvent.SwarmRush || e == OmenEvent.MarchingLine;

        /// <summary>
        /// The events the wheel can pick on a stage. Death's Shadow only on Stages 5 and 9 (owner). While a stage boss is
        /// expected or alive, formations are left out: the spawner sends no formation during a boss fight, so a picked one
        /// would never come (balance method; the wiki does not say whether Random Events fire during a boss).
        /// </summary>
        public static List<OmenEvent> Pool(int stage, bool formationsAllowed)
        {
            var pool = new List<OmenEvent>();
            foreach (OmenEvent e in System.Enum.GetValues(typeof(OmenEvent)))
            {
                if (e == OmenEvent.DeathsShadow && !ShadowStage(stage)) continue;
                if (!formationsAllowed && IsFormation(e)) continue;
                pool.Add(e);
            }
            return pool;
        }

        /// <summary>
        /// One pick. The wiki gives no weights ("The exact number of possible events is not yet certain") and no Luck rule, so
        /// every event of the pool is equally likely (balance method, 2026-10-09). The same event may come again: the wiki's
        /// v1.6.0 note fixed a crash "when certain trisection events triggered for a second time in the same run".
        /// </summary>
        public static OmenEvent Pick(IReadOnlyList<OmenEvent> pool, float roll01)
        {
            if (pool == null || pool.Count == 0) return OmenEvent.FortunesFavour;
            return pool[Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(roll01) * pool.Count), 0, pool.Count - 1)];
        }

        /// <summary>
        /// The marks the stage clock crossed going from <paramref name="previous"/> to <paramref name="now"/> seconds, oldest
        /// first: (mark seconds, true for a pick at XX:30, false for a fire at XX:00). 0:00 is not a mark (nothing was picked).
        /// Marks more than <see cref="StaleSeconds"/> behind <paramref name="now"/> are dropped.
        /// </summary>
        public static void Crossed(float previous, float now, List<(float Mark, bool Pick)> marks)
        {
            marks.Clear();
            if (now <= previous) return;
            int first = Mathf.FloorToInt(previous / MarkSeconds) + 1;
            int last = Mathf.FloorToInt(now / MarkSeconds);
            for (int k = Mathf.Max(1, first); k <= last; k++)
            {
                float mark = k * MarkSeconds;
                if (now - mark > StaleSeconds) continue;
                marks.Add((mark, k % 2 == 1));
            }
        }

        /// <summary>Soul Hunter's own name of an event, as the wheel and the log show it.</summary>
        public static string Name(OmenEvent e)
        {
            switch (e)
            {
                case OmenEvent.MendingRing: return "Mending Ring";
                case OmenEvent.FeastRing: return "Feast Ring";
                case OmenEvent.HellfireBreath: return FireBreath.DisplayName;
                case OmenEvent.GildedFever: return "Gilded Fever";
                case OmenEvent.GallowsRing: return "Gallows Ring";
                case OmenEvent.TollOfSouls: return "Toll of Souls";
                case OmenEvent.WanderingRelic: return "Wandering Relic";
                case OmenEvent.LanternBloom: return "Lantern Bloom";
                case OmenEvent.DeathsShadow: return "Death's Shadow";
                case OmenEvent.SwellingHorde: return "Swelling Horde";
                case OmenEvent.SoulMeteors: return "Soul Meteors";
                case OmenEvent.FortunesFavour: return "Fortune's Favour";
                case OmenEvent.ClosingRing: return "Closing Ring";
                case OmenEvent.HordeSwarm: return "Horde Swarm";
                case OmenEvent.SwarmRush: return "Swarm Rush";
                case OmenEvent.MarchingLine: return "Marching Line";
            }
            return e.ToString();
        }

        // --- Numbers. VS gives the ones quoted; the rest are Soul Hunter's (balance method, 2026-10-09), each with its reason.

        /// <summary>VS Pet: "each healing them for 2 hit points".</summary>
        public const int MendingHealAmount = 2;
        /// <summary>"a big ring": no count. 24 hearts heal 48 at most, between one and two Floor Chickens, so the ring helps
        /// without replacing the lanterns' chickens (the heal balance pass, 2026-09-29).</summary>
        public const int MendingCount = 24;
        /// <summary>VS Chicken Bonanza: "each one healing for 30 hit points" (the Floor Chicken's own heal).</summary>
        public const int FeastHeal = 30;
        /// <summary>No count on the wiki. 8 chickens: a full heal from near death even without Recovery, the "bonanza".</summary>
        public const int FeastCount = 8;
        /// <summary>Both rings lie this far out: inside the 55-degree camera's view (about 12 units up and down), clear of
        /// the base Magnet radius (4), so the player walks the ring as in VS rather than having it fly in at once.</summary>
        public const float RingRadius = 6.5f;
        /// <summary>VS Pile Assault: "Disappears after 1 minute."</summary>
        public const float GallowsSeconds = 60f;
        /// <summary>VS Light Sources: "several". 6: half a Cappella Magna-sized stage's 20 lanterns, more than Mad Forest's
        /// 10-lantern cap could ever show at once near the player.</summary>
        public const int LanternCount = 6;
        public const float LanternRadius = 7.5f;
        /// <summary>VS The Stalker: "for ~2 minutes" (Death's Shadow's own default length).</summary>
        public const float ShadowSeconds = StageHazardTable.ShadowLifetimeSeconds;
        /// <summary>VS Ultra Wave: "for 1 minute".</summary>
        public const float SwellingSeconds = 60f;
        /// <summary>"Increases the enemy spawn rate": no factor. x2 (spawn interval halved): clearly felt, and still under
        /// the 300 periodic-spawn cap, which holds the swell inside the screen budget.</summary>
        public const float SwellingFactor = 2f;
        /// <summary>VS Luck Boost: "by 50%" (Luck is 1 at base, so +0.5).</summary>
        public const float FortuneLuck = 0.5f;
        /// <summary>Formation sizes: no counts on the wiki. Soul Hunter's own stage events use 16-24 members (WaveTables), so
        /// Omens stay in the same range.</summary>
        public const int RingMembers = 24, SwarmMembers = 24, RushMembers = 16, LineMembers = 20;
        /// <summary>VS Bat Swarm: "several swarms ... for ~30 seconds": three swarms, 10 s apart.</summary>
        public const int RushWaves = 3;
        public const float RushGapSeconds = 10f;
        /// <summary>Wandering Relic lies this far from the player, on open ground ("near the player").</summary>
        public const float RelicDistance = 4f;
    }

    /// <summary>
    /// Runs Omens (VS Random Events) when the mode is on: at each XX:30 of the stage clock it picks an event (shown by
    /// OmenRouletteUI), and at the next XX:00 it fires it with Soul Hunter's existing systems. GameSessionManager adds it.
    /// Events fire on game time, never during a stage transition, and not at the 30:00 mark of a timed stage (the stage
    /// clears there; a pick at 29:30 of such a stage is not made).
    /// </summary>
    public sealed class RandomEventDirector : MonoBehaviour
    {
        public static RandomEventDirector Instance { get; private set; }

        private LevelProgressionManager _progression;
        private float _lastClock;
        private readonly List<(float Mark, bool Pick)> _marks = new List<(float, bool)>();
        private float _swellingLeft;

        /// <summary>The event picked at the last XX:30, waiting for XX:00 (null when none).</summary>
        public OmenEvent? Pending { get; private set; }
        /// <summary>The stage-clock second the pending event fires at.</summary>
        public float PendingFiresAt { get; private set; }
        /// <summary>The last event fired (null before the first).</summary>
        public OmenEvent? LastFired { get; private set; }
        public int FiredCount { get; private set; }

        /// <summary>Raised at XX:30 with the picked event and the stage second it fires at.</summary>
        public event System.Action<OmenEvent, float> OnPicked;
        /// <summary>Raised at XX:00 when the event fires.</summary>
        public event System.Action<OmenEvent> OnFired;

        /// <summary>Tests: the next pick, instead of a random one (null = random).</summary>
        public System.Func<OmenEvent?> ForcedPick { get; set; }

        private void Awake() => Instance = this;

        private void Start()
        {
            _progression = LevelProgressionManager.Instance;
            if (_progression == null) return;
            _progression.OnStageStarted += HandleStageStarted;
            _progression.OnStageTimeChanged += HandleClock;
            _lastClock = _progression.StageElapsedTime;
        }

        private void OnDestroy()
        {
            if (_progression != null)
            {
                _progression.OnStageStarted -= HandleStageStarted;
                _progression.OnStageTimeChanged -= HandleClock;
            }
            RunModes.EventFrequencyBoost = 1f;
            if (Instance == this) Instance = null;
        }

        private void HandleStageStarted(int stage)
        {
            Pending = null; // a new stage starts its own timetable at 0:00
            _lastClock = 0f;
        }

        private void Update()
        {
            // VS Ultra Wave lasts a minute of play: game time, so a pause holds it.
            if (_swellingLeft <= 0f) return;
            _swellingLeft -= Time.deltaTime;
            if (_swellingLeft <= 0f) RunModes.EventFrequencyBoost = 1f;
        }

        private void HandleClock(float now)
        {
            float previous = _lastClock;
            _lastClock = now;
            if (!RunModes.RandomEvents || _progression == null) return;
            OmenTable.Crossed(previous, now, _marks);
            foreach (var (mark, pick) in _marks)
            {
                if (pick) PickAt(mark);
                else if (Pending.HasValue && Mathf.Approximately(PendingFiresAt, mark)) FirePending();
            }
        }

        /// <summary>True when a XX:00 mark at <paramref name="seconds"/> can fire on the current stage.</summary>
        private bool CanFireAt(float seconds)
        {
            if (_progression == null || _progression.IsStageTransitioning) return false;
            // A timed stage clears at 30:00 (the screen is wiped there); Level 10 has no time limit (owner, the ending).
            return _progression.CurrentStage >= LevelProgressionManager.MaxStages
                   || seconds < _progression.CurrentLevel.RunDurationSeconds;
        }

        private void PickAt(float mark)
        {
            float firesAt = mark + OmenTable.MarkSeconds;
            if (!CanFireAt(firesAt)) { Pending = null; return; }
            var forced = ForcedPick != null ? ForcedPick() : null;
            var pool = OmenTable.Pool(_progression.CurrentStage, FormationsAllowed());
            Pending = forced ?? OmenTable.Pick(pool, Random.value);
            PendingFiresAt = firesAt;
            Debug.Log($"[Omens] {OmenTable.Name(Pending.Value)} picked at {mark:0}s; fires at {firesAt:0}s.");
            OnPicked?.Invoke(Pending.Value, firesAt);
        }

        private bool FormationsAllowed() => !_progression.IsBossWindow || _progression.IsBossDefeated;

        private void FirePending()
        {
            var e = Pending.Value;
            Pending = null;
            if (!CanFireAt(PendingFiresAt)) return;
            Fire(e);
        }

        /// <summary>Fires an event now (the director at XX:00, and tests). Returns false when it could not happen.</summary>
        public bool Fire(OmenEvent e)
        {
            var player = PlayerController.Instance;
            if (player == null) return false;
            bool done = Run(e, player);
            LastFired = e;
            FiredCount++;
            Debug.Log($"[Omens] {OmenTable.Name(e)} {(done ? "fired" : "could not happen")}.");
            OnFired?.Invoke(e);
            return done;
        }

        private bool Run(OmenEvent e, PlayerController player)
        {
            Vector3 at = player.transform.position;
            var spawner = FindFirstObjectByType<EnemySpawner>();
            switch (e)
            {
                case OmenEvent.MendingRing: return HealRing(at, OmenTable.MendingCount, OmenTable.MendingHealAmount, true);
                case OmenEvent.FeastRing: return HealRing(at, OmenTable.FeastCount, OmenTable.FeastHeal, false);
                case OmenEvent.HellfireBreath: return FireBreath.Begin(player.transform) != null;
                case OmenEvent.GildedFever: return GoldFever.Begin(player.gameObject) != null;
                case OmenEvent.GallowsRing:
                    return StageHazards.Instance != null
                           && StageHazards.Instance.StartPileAssault(StageHazardTable.PileCount, OmenTable.GallowsSeconds).Count > 0;
                case OmenEvent.TollOfSouls:
                {
                    ScreenWipePickup.Wipe(Camera.main);
                    SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.LevelUp, at + Vector3.up * 0.5f);
                    var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
                    if (audio != null) audio.PlaySFX(audio.ExplosionSound);
                    return true;
                }
                case OmenEvent.WanderingRelic: return DropRelic(at);
                case OmenEvent.LanternBloom: return LanternRing(at);
                case OmenEvent.DeathsShadow:
                    return OmenTable.ShadowStage(_progression != null ? _progression.CurrentStage : 0)
                           && StageHazards.Instance != null && StageHazards.Instance.SpawnDeathsShadow(OmenTable.ShadowSeconds) != null;
                case OmenEvent.SwellingHorde:
                    RunModes.EventFrequencyBoost = OmenTable.SwellingFactor;
                    _swellingLeft = OmenTable.SwellingSeconds;
                    return true;
                case OmenEvent.SoulMeteors:
                    if (StageHazards.Instance == null) return false;
                    StageHazards.Instance.StartMeteorVolley();
                    return true;
                case OmenEvent.FortunesFavour:
                    if (player.Stats == null) return false;
                    player.Stats.AddLuck(OmenTable.FortuneLuck);
                    return true;
                case OmenEvent.ClosingRing: return Formation(spawner, StageEventType.ClosingRing, OmenTable.RingMembers);
                case OmenEvent.HordeSwarm: return Formation(spawner, StageEventType.Swarm, OmenTable.SwarmMembers);
                case OmenEvent.MarchingLine: return Formation(spawner, StageEventType.Wall, OmenTable.LineMembers);
                case OmenEvent.SwarmRush:
                    if (!Formation(spawner, StageEventType.Swarm, OmenTable.RushMembers)) return false;
                    StartCoroutine(MoreSwarms(spawner));
                    return true;
            }
            return false;
        }

        private static bool Formation(EnemySpawner spawner, StageEventType type, int count) =>
            spawner != null && spawner.TriggerStageEvent(type, count).Count > 0;

        private System.Collections.IEnumerator MoreSwarms(EnemySpawner spawner)
        {
            for (int i = 1; i < OmenTable.RushWaves; i++)
            {
                yield return new WaitForSeconds(OmenTable.RushGapSeconds); // game time: a pause holds the rush
                if (spawner == null || _progression == null || _progression.IsStageTransitioning) yield break;
                Formation(spawner, StageEventType.Swarm, OmenTable.RushMembers);
            }
        }

        /// <summary>A ring of heals around the player. Small heals are pooled chickens drawn at half size (no heart art yet).</summary>
        private static bool HealRing(Vector3 centre, int count, int heal, bool small)
        {
            var pool = PickupPoolManager.Instance;
            if (pool == null) return false;
            int placed = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, 360f * i / count, 0f) * Vector3.forward * OmenTable.RingRadius;
                var chicken = pool.SpawnChicken(StageCorridor.Clamp(centre + offset));
                if (chicken == null) continue;
                var pickup = chicken.GetComponent<HealthPickup>();
                if (pickup != null)
                {
                    if (small) MendingHeal.Mark(pickup, heal);
                    else pickup.HealAmount = heal;
                }
                placed++;
            }
            return placed > 0;
        }

        /// <summary>VS Free Passive: a random passive item near the player (a walk-over item, as stage items are).</summary>
        private static bool DropRelic(Vector3 centre)
        {
            var passives = RelicChoices();
            if (passives.Count == 0) return false;
            var type = passives[Random.Range(0, passives.Count)];
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
            Vector3 spot = GroundCheck.NearestOpen(StageCorridor.Clamp(centre + new Vector3(dir.x, 0f, dir.y) * OmenTable.RelicDistance), 0.9f);
            spot.y = centre.y;
            StageItemPickup.Create(type, spot);
            return true;
        }

        /// <summary>
        /// The passives Wandering Relic can give. VS: "It is currently unknown if any items are excluded." Soul Hunter leaves
        /// out the Metaglios, which "cannot be obtained from level ups" and lie as stage items on every stage (balance method).
        /// A passive the player cannot take (no slot, or maxed) pays the stage item's coin bag, as a stage item does.
        /// </summary>
        public static List<UpgradeData.UpgradeType> RelicChoices()
        {
            var list = new List<UpgradeData.UpgradeType>();
            var catalog = GameContentCatalog.Load();
            if (catalog == null || catalog.WeaponDefinitions == null) return list;
            foreach (var definition in catalog.WeaponDefinitions)
                if (definition != null && definition.IsPassive && !UpgradeRarity.IsStageItemOnly(definition.Type) && !list.Contains(definition.Type))
                    list.Add(definition.Type);
            return list;
        }

        /// <summary>VS Light Sources: "several Light Sources around the player" (Soul Lanterns, with their own drops).</summary>
        private static bool LanternRing(Vector3 centre)
        {
            var spawner = FindFirstObjectByType<LightSourceSpawner>();
            if (spawner == null) return false;
            int placed = 0;
            float turn = Random.Range(0f, 360f);
            for (int i = 0; i < OmenTable.LanternCount; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, turn + 360f * i / OmenTable.LanternCount, 0f) * Vector3.forward * OmenTable.LanternRadius;
                Vector3 spot = GroundCheck.NearestOpen(StageCorridor.Clamp(centre + offset), 1f);
                spot.y = centre.y;
                if (spawner.Spawn(spot) != null) placed++;
            }
            return placed > 0;
        }
    }

    /// <summary>
    /// A Mending Ring heal: a pooled Floor Chicken that heals <see cref="HealthPickup.HealAmount"/> = 2 and is drawn at half
    /// size, so it reads as a small heal next to a full chicken. When it goes back to the pool it becomes an ordinary
    /// 30-point chicken again. (VS draws hearts; Soul Hunter has no heart pickup art yet.)
    /// </summary>
    public sealed class MendingHeal : MonoBehaviour
    {
        public const float Scale = 0.5f;
        private HealthPickup _pickup;
        private int _heal;
        private Vector3 _scale;

        public static void Mark(HealthPickup pickup, int heal)
        {
            if (pickup == null) return;
            var mark = pickup.GetComponent<MendingHeal>();
            if (mark == null)
            {
                mark = pickup.gameObject.AddComponent<MendingHeal>();
                mark._pickup = pickup;
                mark._heal = pickup.HealAmount;
                mark._scale = pickup.transform.localScale;
            }
            pickup.HealAmount = heal;
            pickup.transform.localScale = mark._scale * Scale;
        }

        private void OnDisable()
        {
            if (_pickup != null)
            {
                _pickup.HealAmount = _heal;
                _pickup.transform.localScale = _scale;
            }
            Destroy(this);
        }
    }
}
