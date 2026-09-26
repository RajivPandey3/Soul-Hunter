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
                Vector3 spot = GroundCheck.NearestOpen(origin + entry.Offset, 0.9f);
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
                if (item != null) _placed.Add(item);
            }

            var catalog = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            if (catalog == null || catalog.RunePlate == null) return;
            foreach (var offset in StageItems.PlateOffsets())
            {
                Vector3 spot = GroundCheck.NearestOpen(origin + offset, 1.4f);
                spot.y = origin.y;
                var plate = Instantiate(catalog.RunePlate, spot, Quaternion.Euler(0f, 22.5f, 0f)).GetComponent<PressurePlate>();
                if (plate != null) _plates.Add(plate);
            }
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

    /// <summary>
    /// A passive or weapon lying on the ground (VS stage item). Walking over it gives it like a level-up would: new at
    /// level 1, owned +1 level. With no free slot or at max level it becomes a coin bag (10 gold; provisional).
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
                if (next != null && weapons.HasSlotFor(Upgrade) && current + 1 <= weapons.GetMaxWeaponLevel(Upgrade))
                {
                    weapons.ApplyUpgrade(next);
                    upgraded = weapons.GetWeaponLevel(Upgrade) == current + 1;
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
