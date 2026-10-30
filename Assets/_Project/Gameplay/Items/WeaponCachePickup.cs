using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// The Armoury Cache (Omens; VS Random Event "Candybox", vampire.survivors.wiki/w/Random_Events, raw, 2026-10-13: "Puts
    /// a Candybox on the floor near the player, enabling them to add any weapon they want to their inventory"; /w/Candybox:
    /// "Obtaining Candybox opens a list of all base weapons remaining to be obtained in the run. For a weapon to appear in
    /// the Candybox, it has to be used at least once in the entire save file beforehand. Weapons that were banished will
    /// also still appear in the list").
    ///
    /// Walking over it opens the list (the Arcana choice screen in its weapon form); the chosen weapon joins the inventory
    /// at level 1. It lies on the floor like a stage item, so it goes in as one: past the six slots when they are full
    /// (wiki Passive_items, "Stage items"; WeaponManager.AcquireStageItem). With nothing left to choose it pays a Big
    /// Coin Bag (25 gold, the game's rule for an item with nothing left to give).
    /// Not built: the Candybox as a level-up or chest item, and its gift (Super Candybox II Turbo): only the Omen has it.
    /// </summary>
    public class WeaponCachePickup : MonoBehaviour
    {
        public const string DisplayName = "Armoury Cache";
        public const int FallbackGold = 25;

        private bool _taken;
        /// <summary>The weapons offered by the open list (tests).</summary>
        public IReadOnlyList<UpgradeType> Offered { get; private set; } = new List<UpgradeType>();
        /// <summary>Opens the list; null uses the game's choice screen. Tests set their own to answer it.</summary>
        public static System.Func<IReadOnlyList<UpgradeType>, System.Action<UpgradeType?>, bool> Chooser { get; set; }

        private static readonly List<WeaponCachePickup> Live = new List<WeaponCachePickup>();
        /// <summary>Caches lying on the ground now (tests).</summary>
        public static int LiveCount { get { Live.RemoveAll(c => c == null); return Live.Count; } }

        /// <summary>
        /// A cache left unopened goes with its stage, like the stage's own items (audit L-11, 2026-10-30): before, every
        /// unopened cache stayed until the scene ended, a slow pile-up in long Endless runs. A guardian's Soul Egg is an
        /// earned reward and stays (at most four guardians a stage).
        /// </summary>
        public static void RemoveLeftovers()
        {
            foreach (var cache in Live) if (cache != null && !cache._taken) Destroy(cache.gameObject);
            Live.Clear();
        }

        private void OnDestroy() => Live.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Live.Clear();

        public static WeaponCachePickup Create(Vector3 position)
        {
            var go = new GameObject("WeaponCache");
            go.transform.position = position;
            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.9f;
            trigger.center = new Vector3(0f, 0.8f, 0f);
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var cache = go.AddComponent<WeaponCachePickup>();
            cache.BuildVisual();
            Live.Add(cache);
            return cache;
        }

        /// <summary>
        /// Placeholder look (not owner-approved art): the approved Soul Reliquary chest model, small, over the Soul
        /// Lantern's ground glow, so it reads as "a box that holds something" without new art.
        /// </summary>
        private void BuildVisual()
        {
            var catalog = GameContentCatalog.Load();
            var pivot = new GameObject("Visual").transform;
            pivot.SetParent(transform, false);
            pivot.localPosition = new Vector3(0f, 0.4f, 0f);
            var chest = PickupPoolManager.Instance != null ? PickupPoolManager.Instance.ChestPrefab : null;
            if (chest != null)
            {
                // Only the chest's drawn meshes are copied (from the prefab itself): no pickup script, trigger or pool.
                var box = new GameObject("Box").transform;
                box.SetParent(pivot, false);
                box.localScale = Vector3.one * 0.7f;
                var toRoot = chest.transform.worldToLocalMatrix;
                foreach (var filter in chest.GetComponentsInChildren<MeshFilter>(true))
                {
                    var source = filter.GetComponent<MeshRenderer>();
                    if (source == null || !source.enabled || filter.sharedMesh == null) continue;
                    var part = new GameObject(filter.name);
                    part.transform.SetParent(box, false);
                    var local = toRoot * filter.transform.localToWorldMatrix;
                    part.transform.localPosition = local.GetColumn(3);
                    part.transform.localRotation = local.rotation;
                    part.transform.localScale = local.lossyScale;
                    part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                    var renderer = part.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = source.sharedMaterials;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            var lanternGlow = catalog != null && catalog.SoulLantern != null ? catalog.SoulLantern.transform.Find("GroundGlow") : null;
            if (lanternGlow != null)
            {
                var glow = Instantiate(lanternGlow.gameObject, transform);
                glow.name = "GroundGlow";
                glow.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                glow.transform.localScale = Vector3.one * 2.6f;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_taken) return;
            if (!other.CompareTag("Player") && other.GetComponentInParent<PlayerController>() == null) return;
            Take();
        }

        /// <summary>
        /// The weapons the cache can give now: base weapons with a level-1 upgrade that the hero does not hold, that level-ups
        /// could ever give (not union results), and that this save has used at least once (its Collection entry is found).
        /// </summary>
        public static List<UpgradeType> Choices(WeaponManager weapons, GameData data)
        {
            var list = new List<UpgradeType>();
            var catalog = GameContentCatalog.Load();
            if (weapons == null || catalog == null || catalog.WeaponDefinitions == null) return list;
            foreach (var definition in catalog.WeaponDefinitions)
            {
                if (definition == null || definition.IsPassive || list.Contains(definition.Type)) continue;
                if (WeaponUnionData.IsUnionResult(definition.Type) || UpgradeRarity.IsStageItemOnly(definition.Type)) continue;
                if (weapons.GetWeaponLevel(definition.Type) > 0) continue;
                if (FindUpgrade(definition.Type) == null) continue;
                if (data != null && !CollectionBook.IsFound(data, CollectionCatalog.ItemId(definition.Type))) continue;
                list.Add(definition.Type);
            }
            return list;
        }

        /// <summary>Opens the list (also called by tests). Returns false when there was nothing to choose (gold instead).</summary>
        public bool Take()
        {
            if (_taken) return false;
            _taken = true;
            var weapons = WeaponManager.Instance;
            var choices = Choices(weapons, GameSettings.Data);
            Offered = choices;
            SoulHunter.Gameplay.Core.RunStatsTracker.Instance?.RecordPickup(DisplayName);
            if (choices.Count == 0)
            {
                if (PickupPoolManager.Instance != null) PickupPoolManager.Instance.SpawnGold(transform.position, FallbackGold);
                Finish();
                return false;
            }
            bool opened = Chooser != null ? Chooser(choices, Give) : WeaponCacheChoice.Open(choices, Give);
            if (!opened) Give(choices[0]); // another choice is on screen: never lose the cache
            return true;
        }

        private void Give(UpgradeType? chosen)
        {
            var weapons = WeaponManager.Instance;
            var upgrade = chosen.HasValue ? FindUpgrade(chosen.Value) : null;
            bool given = weapons != null && upgrade != null && weapons.AcquireStageItem(upgrade);
            if (!given && PickupPoolManager.Instance != null) PickupPoolManager.Instance.SpawnGold(transform.position, FallbackGold);
            Finish();
        }

        private void Finish()
        {
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.LevelUp, transform.position + Vector3.up * 0.5f);
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.UIConfirmSound, 0.8f);
            Destroy(gameObject);
        }

        private static UpgradeData FindUpgrade(UpgradeType type)
        {
            var catalog = GameContentCatalog.Load();
            return catalog != null && catalog.Upgrades != null
                ? catalog.Upgrades.FirstOrDefault(u => u != null && u.Type == type && u.Level == 1)
                : null;
        }
    }

    /// <summary>
    /// Shows the Armoury Cache's list on the Arcana choice screen: one stand-in card per weapon (its name, icon and what
    /// it does). The cards exist only while the list is open.
    /// </summary>
    public static class WeaponCacheChoice
    {
        public static bool Open(IReadOnlyList<UpgradeType> choices, System.Action<UpgradeType?> chosen)
        {
            var catalog = GameContentCatalog.Load();
            if (choices == null || choices.Count == 0 || catalog == null) return false;
            var cards = new List<ArcanaData>();
            var typeOf = new Dictionary<ArcanaData, UpgradeType>();
            foreach (var type in choices)
            {
                var definition = catalog.WeaponDefinitions != null
                    ? System.Array.Find(catalog.WeaponDefinitions, d => d != null && d.Type == type) : null;
                var card = ScriptableObject.CreateInstance<ArcanaData>();
                card.name = "WeaponCard_" + type;
                card.CardNumber = "";
                card.CardName = UpgradeText.NameOf(type);
                card.Description = definition != null && !string.IsNullOrEmpty(definition.Description)
                    ? definition.Description : UpgradeText.FallbackDescription(type);
                card.CardIcon = definition != null ? definition.Icon : null;
                cards.Add(card);
                typeOf[card] = type;
            }
            var draft = new SoulHunter.Gameplay.Core.ArcanaDraft(SoulHunter.Gameplay.Core.ArcanaDraft.Kind.Weapons, cards, 0, 0, null);
            bool shown = SoulHunter.Gameplay.UI.ArcanaChoiceScreen.Show(draft, card =>
            {
                UpgradeType? picked = card != null && typeOf.TryGetValue(card, out var type) ? type : (UpgradeType?)null;
                foreach (var made in cards) if (made != null) Object.Destroy(made);
                chosen?.Invoke(picked);
            });
            if (!shown) foreach (var made in cards) if (made != null) Object.Destroy(made);
            return shown;
        }
    }
}
