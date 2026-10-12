using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// VS stage items (vampire.survivors.wiki, stage pages / Stage items): items lie on the ground at set distances
    /// from the start and are picked up by walking over them. Soul Hunter follows the Dairy Plant layout, four items
    /// on the diagonals a few tiles out: one passive, one weapon, a floor chicken and a Vacuum or an Orologion
    /// (Magnet / Time Freeze). Which passive and weapon each stage offers is Soul Hunter content (provisional).
    /// </summary>
    public static class StageItems
    {
        public enum Kind { Passive, Weapon, Chicken, Magnet, TimeFreeze }

        public struct Entry
        {
            public Kind Kind;
            public UpgradeType Upgrade;   // for Passive and Weapon
            public Vector3 Offset;        // from the player's position when the stage starts
        }

        /// <summary>Distances from the start, in world units (a floor tile is 20): 2.75 to 4.25 tiles.</summary>
        public static readonly float[] Distances = { 55f, 65f, 75f, 85f };

        private static readonly UpgradeType[] Passives =
        {
            UpgradeType.Spinach, UpgradeType.Armor, UpgradeType.EmptyTome, UpgradeType.Candelabrador, UpgradeType.Bracer,
            UpgradeType.Clover, UpgradeType.Spellbinder, UpgradeType.Duplicator, UpgradeType.Pummarola, UpgradeType.Attractorb,
        };

        private static readonly UpgradeType[] Weapons =
        {
            UpgradeType.Knife, UpgradeType.Axe, UpgradeType.Cross, UpgradeType.SantaWater, UpgradeType.FireWand,
            UpgradeType.LightningRing, UpgradeType.Runetracer, UpgradeType.Bone, UpgradeType.Garlic, UpgradeType.Peachone,
        };

        /// <summary>VS pressure plates: four Rune Plates straight N/E/S/W of the start, between the item diagonals. Provisional.</summary>
        public const float PlateDistance = 30f;
        /// <summary>How far west / east of the start the Metaglios lie (Soul Hunter's distance; the wiki gives none).</summary>
        public const float MetaglioDistance = 45f;
        /// <summary>
        /// How far north / south of the start the two rings lie (VS Silver Ring / Gold Ring). The wiki gives no distance
        /// ("Generally, it will spawn north (south) of the starting area"). Balance method: 60, further out than the
        /// Metaglios (45) so that in the Castle corridor, where every item lies along the hallway, a ring never lands on
        /// a Metaglio, and clear of the Rune Plates (30) and the diagonal items (55-85 on the diagonals).
        /// </summary>
        public const float RingDistance = 60f;
        /// <summary>
        /// VS wiki (Inlaid_Library, raw, fetched 2026-10-12, "Stage items"): "Stone Mask: 5 tilesets, east of the
        /// starting area." Soul Hunter's Level 2 is Inlaid Library's counterpart; five of its 20-unit floor tiles.
        /// </summary>
        public const float MisersIdolDistance = 100f;
        public const int MisersIdolStage = 2;

        public static List<Vector3> PlateOffsets()
        {
            var offsets = new List<Vector3>();
            for (int i = 0; i < 4; i++) offsets.Add(Quaternion.Euler(0f, 90f * i, 0f) * Vector3.forward * PlateDistance);
            return offsets;
        }

        /// <summary>The four items of a stage (1-10), one per diagonal, turned a quarter per stage.</summary>
        public static List<Entry> Plan(int stage)
        {
            int s = Mathf.Clamp(stage, 1, 10) - 1;
            var kinds = new[] { Kind.Passive, Kind.Weapon, Kind.Chicken, s % 2 == 0 ? Kind.Magnet : Kind.TimeFreeze };
            var plan = new List<Entry>();
            for (int i = 0; i < 4; i++)
            {
                float angle = 45f + 90f * ((i + s) % 4);
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                plan.Add(new Entry
                {
                    Kind = kinds[i],
                    Upgrade = kinds[i] == Kind.Passive ? Passives[s] : kinds[i] == Kind.Weapon ? Weapons[s] : default,
                    Offset = direction * Distances[i],
                });
            }
            // VS (wiki Metaglio_Left / Metaglio_Right): they "cannot be obtained from level ups, but can be found as a stage
            // item in every stage"; "Generally, it will spawn west (east) of the starting area". The distance is Soul
            // Hunter's. The VS guards (City and Volcano Atlanteans) are not built (they need their own enemies).
            plan.Add(new Entry { Kind = Kind.Passive, Upgrade = UpgradeType.MetaglioLeft, Offset = Vector3.left * MetaglioDistance });
            plan.Add(new Entry { Kind = Kind.Passive, Upgrade = UpgradeType.MetaglioRight, Offset = Vector3.right * MetaglioDistance });
            // VS (wiki Silver_Ring / Gold_Ring, fetched 2026-10-12): stage items "in every stage" (with a few VS-only
            // exceptions); Silver "will spawn north of the starting area", Gold "south". The VS guards (Moon and Sun
            // Atlanteans) are not built, as for the Metaglios.
            plan.Add(new Entry { Kind = Kind.Passive, Upgrade = UpgradeType.RingOfLongHours, Offset = Vector3.forward * RingDistance });
            plan.Add(new Entry { Kind = Kind.Passive, Upgrade = UpgradeType.RingOfDarkHours, Offset = Vector3.back * RingDistance });
            // VS Stone Mask lies east of the start on Inlaid Library (Level 2). Its other VS stages (Moongolow, Boss Rash,
            // Mt.Moonspell, Abyss Foscari) are not mapped: the wiki pages of Soul Hunter's Level 5 and 6 sources were not
            // read for its place there.
            if (s + 1 == MisersIdolStage)
                plan.Add(new Entry { Kind = Kind.Passive, Upgrade = UpgradeType.MisersIdol, Offset = Vector3.right * MisersIdolDistance });
            return plan;
        }
    }

    /// <summary>Open, walkable ground: no stage obstacle at or around the spot (the blocker boxes are hollow meshes).</summary>
    public static class GroundCheck
    {
        public static bool IsOpen(Vector3 spot, float radius)
        {
            int obstacles = LayerMask.GetMask("Obstacle");
            if (obstacles == 0) return true;
            return !UnityEngine.Physics.CheckSphere(spot + Vector3.up * 0.8f, radius, obstacles, QueryTriggerInteraction.Collide) &&
                   !UnityEngine.Physics.Raycast(spot + Vector3.up * 20f, Vector3.down, 20f, obstacles, QueryTriggerInteraction.Collide);
        }

        /// <summary>The planned spot if it is open, else the nearest open spot on rings around it (or the plan).</summary>
        public static Vector3 NearestOpen(Vector3 spot, float radius)
        {
            if (IsOpen(spot, radius)) return spot;
            for (int ring = 1; ring <= 6; ring++)
            for (int k = 0; k < 8; k++)
            {
                Vector3 candidate = spot + Quaternion.Euler(0f, k * 45f, 0f) * Vector3.forward * (ring * 1.5f);
                if (IsOpen(candidate, radius)) return candidate;
            }
            return spot;
        }
    }

    /// <summary>Places each stage's items and Rune Plates when it starts and removes the previous stage's.</summary>
    public class StageItemSpawner : MonoBehaviour
    {
        private readonly List<GameObject> _placed = new List<GameObject>();
        private readonly List<PressurePlate> _plates = new List<PressurePlate>();
        private SoulHunter.Gameplay.Core.LevelProgressionManager _progression;
        private int _placedFor;

        public IReadOnlyList<GameObject> Placed => _placed;
        public IReadOnlyList<PressurePlate> Plates => _plates;

        private void Start()
        {
            _progression = SoulHunter.Gameplay.Core.LevelProgressionManager.Instance;
            if (_progression != null) _progression.OnStageStarted += HandleStageStarted;
            StartCoroutine(PlaceWhenReady(_progression != null ? _progression.CurrentStage : 1));
        }

        private void OnDestroy()
        {
            if (_progression != null) _progression.OnStageStarted -= HandleStageStarted;
        }

        private void HandleStageStarted(int stage) => StartCoroutine(PlaceWhenReady(stage));

        /// <summary>One frame later, after InfiniteMap has laid this stage's floor (same event, no fixed order).</summary>
        private System.Collections.IEnumerator PlaceWhenReady(int stage)
        {
            yield return null;
            UnityEngine.Physics.SyncTransforms();
            Place(stage);
        }

        public void Place(int stage)
        {
            Clear();
            var player = PlayerController.Instance;
            if (player == null) return;
            _placedFor = stage;
            Vector3 origin = player.transform.position;
            var pool = PickupPoolManager.Instance;
            foreach (var entry in StageItems.Plan(stage))
            {
                // VS Inverse (wiki Stages): "On certain stages, stage items may be placed differently due to the inverted
                // layout": with the inverted layout on, every planned spot is turned 180 degrees about the start (InverseMap).
                Vector3 spot = GroundCheck.NearestOpen(origin + CorridorOffset(SoulHunter.Gameplay.Data.InverseMap.Layout(entry.Offset)), 0.9f);
                spot.y = origin.y;
                GameObject item = null;
                switch (entry.Kind)
                {
                    case StageItems.Kind.Passive:
                    case StageItems.Kind.Weapon:
                        item = StageItemPickup.Create(entry.Upgrade, spot).gameObject;
                        break;
                    case StageItems.Kind.Chicken: item = pool != null ? pool.SpawnChicken(spot) : null; break;
                    case StageItems.Kind.Magnet: item = pool != null ? pool.SpawnMagnet(spot) : null; break;
                    case StageItems.Kind.TimeFreeze: item = pool != null ? pool.SpawnTimeFreeze(spot) : null; break;
                }
                if (item != null)
                {
                    _placed.Add(item);
                    // VS: Pentagram erases every item on screen "except for stage items". The mark goes when the item is
                    // picked up or cleared, so a pooled copy reused later is an ordinary pickup again.
                    if (item.GetComponent<StageItemMark>() == null) item.AddComponent<StageItemMark>();
                }
            }

            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            if (catalog == null || catalog.RunePlate == null) return;
            var plateOffsets = StageItems.PlateOffsets();
            PressurePlate.ResetGlobalCooldown(); // a new stage's plates start ready
            for (int i = 0; i < plateOffsets.Count; i++)
            {
                // A corridor has no north or south: its plates lie along it, two each way.
                Vector3 offset = SoulHunter.Gameplay.Data.StageCorridor.Active
                    ? new Vector3((i < 2 ? 1f : -1f) * (i % 2 == 0 ? StageItems.PlateDistance : StageItems.PlateDistance * 1.4f), 0f, 0f)
                    : plateOffsets[i];
                Vector3 spot = GroundCheck.NearestOpen(origin + SoulHunter.Gameplay.Data.InverseMap.Layout(offset), 1.4f);
                spot.y = origin.y;
                var plate = Instantiate(catalog.RunePlate, spot, Quaternion.Euler(0f, 22.5f, 0f)).GetComponent<PressurePlate>();
                if (plate != null) _plates.Add(plate);
            }
        }

        /// <summary>In a corridor the diagonal items lie along it instead, at the same distance, near a wall each.</summary>
        private static Vector3 CorridorOffset(Vector3 offset)
        {
            if (!SoulHunter.Gameplay.Data.StageCorridor.Active) return offset;
            return new Vector3(Mathf.Sign(offset.x) * offset.magnitude, 0f,
                               Mathf.Sign(offset.z) * SoulHunter.Gameplay.Data.StageCorridor.HalfWidth * 0.45f);
        }

        /// <summary>Removes the items of the previous stage that were not picked up.</summary>
        public void Clear()
        {
            foreach (var item in _placed)
            {
                if (item == null) continue;
                if (item.GetComponent<StageItemPickup>() != null) Destroy(item);
                else item.SetActive(false); // pooled pickups go back to their pools
            }
            _placed.Clear();
            foreach (var plate in _plates) if (plate != null) Destroy(plate.gameObject);
            _plates.Clear();
        }
    }

    /// <summary>Marks an item placed on the map by the stage (VS "stage item"); removed when the item leaves the ground.</summary>
    public sealed class StageItemMark : MonoBehaviour
    {
        private void OnDisable() => Destroy(this);
    }

    /// <summary>
    /// A passive or weapon lying on the ground (VS stage item). Walking over it gives it like a level-up would: new at
    /// level 1, owned +1 level, and past the six slots when they are full (VS wiki Passive_items "Stage items": "If they
    /// already have 6 or more passive items, it becomes an additional passive item"; Weapons: "it is possible to carry more
    /// by picking up weapons that generate with the stage"). At max level it becomes a coin bag (10 gold; wiki silent,
    /// balance method). The VS pick-up-or-discard prompt is not built: walking over the item takes it.
    /// </summary>
    public class StageItemPickup : MonoBehaviour
    {
        public const int FallbackGold = 10;
        public UpgradeType Upgrade { get; private set; }
        private bool _taken;

        public static StageItemPickup Create(UpgradeType upgrade, Vector3 position)
        {
            var go = new GameObject("StageItem_" + upgrade);
            go.transform.position = position;
            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.9f;
            trigger.center = new Vector3(0f, 0.8f, 0f);
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var item = go.AddComponent<StageItemPickup>();
            item.Upgrade = upgrade;
            item.BuildVisual();
            return item;
        }

        private void BuildVisual()
        {
            var pivot = new GameObject("Visual").transform;
            pivot.SetParent(transform, false);
            pivot.localPosition = new Vector3(0f, 1.1f, 0f);
            var icon = new GameObject("Icon").AddComponent<SpriteRenderer>();
            icon.transform.SetParent(pivot, false);
            icon.sprite = FindUpgrade(Upgrade, 1)?.Icon;
            var cam = Camera.main;
            if (cam != null) icon.transform.rotation = cam.transform.rotation; // face the tilted game camera
            if (icon.sprite != null)
            {
                float size = Mathf.Max(icon.sprite.bounds.size.x, icon.sprite.bounds.size.y);
                icon.transform.localScale = Vector3.one * (1.3f / Mathf.Max(size, 0.01f));
            }
            // The Soul Lantern's soft ground glow, so an item reads as special from a distance.
            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            var lanternGlow = catalog != null && catalog.SoulLantern != null ? catalog.SoulLantern.transform.Find("GroundGlow") : null;
            if (lanternGlow != null)
            {
                var glow = Instantiate(lanternGlow.gameObject, transform);
                glow.name = "GroundGlow";
                glow.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                glow.transform.localScale = Vector3.one * 2.2f;
            }
            var motion = gameObject.AddComponent<SoulHunter.Gameplay.VFX.PickupIdleMotion>();
            motion.Visual = pivot;
            motion.BobHeight = 0.15f;
            motion.SpinDegreesPerSecond = 0f;
        }

        private static UpgradeData FindUpgrade(UpgradeType type, int level)
        {
            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            return catalog != null && catalog.Upgrades != null
                ? catalog.Upgrades.FirstOrDefault(u => u != null && u.Type == type && u.Level == level)
                : null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_taken) return;
            if (!other.CompareTag("Player") && other.GetComponentInParent<PlayerController>() == null) return;
            _taken = true;
            Take();
        }

        /// <summary>Gives the item to the player (also called by tests). Returns true if it was an upgrade, false if gold.</summary>
        public bool Take()
        {
            _taken = true;
            var weapons = WeaponManager.Instance;
            bool upgraded = false;
            if (weapons != null)
            {
                int current = weapons.GetWeaponLevel(Upgrade);
                var next = FindUpgrade(Upgrade, current + 1);
                if (next != null && current == 0 && WeaponManager.IsStagePassive(Upgrade))
                {
                    // Metaglio Left/Right: their own way in, beyond the six passive slots (WeaponManager.AcquireStagePassive).
                    upgraded = weapons.AcquireStagePassive(next);
                }
                else if (next != null && current + 1 <= weapons.GetMaxWeaponLevel(Upgrade))
                {
                    // VS wiki (Passive_items, "Stage items"; Weapons): a stage item goes past the six slots.
                    upgraded = weapons.AcquireStageItem(next);
                }
            }
            if (!upgraded && PickupPoolManager.Instance != null) PickupPoolManager.Instance.SpawnGold(transform.position, FallbackGold);
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.LevelUp, transform.position + Vector3.up * 0.5f);
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.UIConfirmSound, 0.8f);
            Destroy(gameObject);
            return upgraded;
        }
    }
}
