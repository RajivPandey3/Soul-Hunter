using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// Learning Comment:
    /// Vampire Survivors Drop System: Dushman ke marne par gem girta hai.
    /// Ye script HealthController ke OnDied event ko sunti hai.
    /// </summary>
    [RequireComponent(typeof(HealthController))]
    public class EnemyDrop : MonoBehaviour
    {
        [SerializeField] private GameObject _xpGemPrefab;
        [SerializeField] private GameObject _healthChickenPrefab;
        [Tooltip("Unused since 2026-09-29 (VS rule: enemies do not drop chickens); kept for saved data and old tests.")]
        public float ChickenDropChance = 5f;
        public bool DropsChest = false;
        [Tooltip("Unused since 2026-09-29 (VS rule: enemies do not drop coins); kept for saved data and old tests.")]
        public float GoldDropChance = 2f;
        public int GoldValue = 1;
        [Tooltip("XP in the dropped Soul Gem; its colour and size follow it")]
        public int GemValue = 10;

        private HealthController _health;
        private EnemyController _enemy;
        private bool _dropsChestThisLife;
        private bool _erasedThisLife;
        private bool _arcanaThisLife;

        /// <summary>VS Arcana mode: this life's chest is an Arcana chest (11:00 and 21:00). Cleared on respawn.</summary>
        public void CarryArcanaThisLife() => _arcanaThisLife = true;
        public bool CarriesArcana => _arcanaThisLife;

        /// <summary>
        /// VS Pentagram: "if a boss that would have dropped [a chest] is erased, the Treasure Chest is then dropped by a
        /// different enemy". Chests of erased carriers wait here, with their boss's ChestTiers (and Arcana flag), for the
        /// next enemy the spawner sends. Before 2026-10-09 only a count was kept, so the passed-on chest lost its config.
        /// </summary>
        public static readonly System.Collections.Generic.Queue<SoulHunter.Gameplay.Core.ChestTiers> PendingChests =
            new System.Collections.Generic.Queue<SoulHunter.Gameplay.Core.ChestTiers>();

        // The chest config of this life's chest (null: none given, see ChestTiersAtDeath).
        private SoulHunter.Gameplay.Core.ChestTiers? _tiersThisLife;

        /// <summary>
        /// Makes this enemy drop a chest the next time it dies, with no scheduled config (tests, debug spawns): its tiers
        /// are judged at death (ChestTiers.Unscheduled). Cleared when it respawns.
        /// </summary>
        public void DropChestThisLife() => _dropsChestThisLife = true;

        /// <summary>
        /// Makes this enemy drop a chest with its boss's config <paramref name="tiers"/> the next time it dies (timed chest
        /// elites, the stage boss, a passed-on chest). vampire.survivors.wiki/w/Treasure_Chest: "The chances for obtaining
        /// a 1, 3, or 5-item chest are individually configured per boss enemy." Cleared when it respawns.
        /// </summary>
        public void DropChestThisLife(SoulHunter.Gameplay.Core.ChestTiers tiers)
        {
            _dropsChestThisLife = true;
            _tiersThisLife = tiers;
        }

        /// <summary>This life's chest config: the one given, else the unscheduled rule at the current stage clock.</summary>
        public SoulHunter.Gameplay.Core.ChestTiers ChestTiersAtDeath
        {
            get
            {
                var tiers = _tiersThisLife ?? SoulHunter.Gameplay.Core.ChestTiers.Unscheduled(
                    SoulHunter.Gameplay.Core.LevelProgressionManager.Instance != null
                        ? SoulHunter.Gameplay.Core.LevelProgressionManager.Instance.StageElapsedTime : 0f);
                if (_arcanaThisLife) tiers.IsArcana = true;
                return tiers;
            }
        }

        /// <summary>VS Pentagram: this life ends with no drops (a chest it carried passes to another enemy). Cleared on respawn.</summary>
        public void EraseDropsThisLife() => _erasedThisLife = true;

        /// <summary>True when this enemy drops a chest on death (bosses, and chest elites for this life).</summary>
        public bool WillDropChest => DropsChest || _dropsChestThisLife;

        private void Awake()
        {
            _health = GetComponent<HealthController>();
            _enemy = GetComponent<EnemyController>();
        }

        private void OnEnable()
        {
            _dropsChestThisLife = false;
            _erasedThisLife = false;
            _arcanaThisLife = false;
            _tiersThisLife = null;
            _health.OnDied += HandleDeath;
        }

        private void OnDisable()
        {
            _health.OnDied -= HandleDeath;
        }

        private Renderer[] _bodyRenderers;

        /// <summary>The drawn body (its model's meshes; effects and pickups are left out). Read at death, while it is still shown.</summary>
        private Bounds BodyBounds()
        {
            if (_bodyRenderers == null || _bodyRenderers.Length == 0)
                _bodyRenderers = System.Array.FindAll(GetComponentsInChildren<Renderer>(true), r => r is SkinnedMeshRenderer || r is MeshRenderer);
            bool any = false;
            var bounds = new Bounds(transform.position + Vector3.up, Vector3.one * 2f);
            foreach (var r in _bodyRenderers)
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return bounds;
        }

        /// <summary>
        /// Deaths in one frame that still get their look (the soul puff, the neck spray and the sinking body). A screen wipe
        /// (Pentagram, Soul Toll, Eclipse Sigil, the stage clear) ends up to 300 lives in one frame; a build run on
        /// 2026-10-13 stalled 650-800 ms every time a level-8 Pentagram fired. Past this many the death is counted and
        /// drops as usual but shows nothing more: 32 is the corpse cap, so no body that could be shown is lost.
        /// </summary>
        public const int ShownDeathsPerFrame = 32;
        private static int _deathFrame = -1;
        private static int _deathsThisFrame;

        /// <summary>Counts a death in this frame; true while it is still one of the frame's shown deaths.</summary>
        public static bool CountShownDeath()
        {
            if (Time.frameCount != _deathFrame) { _deathFrame = Time.frameCount; _deathsThisFrame = 0; }
            return ++_deathsThisFrame <= ShownDeathsPerFrame;
        }

        private void HandleDeath()
        {
            // Learning Comment:
            // Performance Optimization: Dushmano ki bheed (horde) marne par FindFirstObjectByType
            // call karne se game lag/freeze ho jati thi. Ab hum O(1) PlayerController.Instance use karte hain.
            var player = SoulHunter.Gameplay.Player.PlayerController.Instance;

            if (CountShownDeath())
            {
                SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.EnemyDeath, transform.position + Vector3.up * 0.8f);
                // Ghosts (Level 7) have no body to bleed; everything else spurts blood from the neck unless Disable Blood is on.
                Bounds body = BodyBounds();
                if (_enemy == null || !_enemy.IsSpectral)
                {
                    Vector3 away = player != null ? transform.position - player.transform.position : transform.forward;
                    SoulHunter.Gameplay.VFX.VFXPoolManager.PlayBlood(SoulHunter.Gameplay.VFX.VFXPoolManager.NeckOf(body), away);
                }
                // VS-style death (owner, 2026-09-29): the body stays a moment and sinks while the blood sprays.
                SoulHunter.Gameplay.VFX.EnemyCorpses.Leave(_bodyRenderers, body);
            }

            if (SoulHunter.Gameplay.Core.RunStatsTracker.Instance != null)
            {
                SoulHunter.Gameplay.Core.RunStatsTracker.Instance.AddKill(_enemy != null ? _enemy.Data : null); // per type for the VS Bestiary
            }

            if (_erasedThisLife)
            {
                if (WillDropChest) PendingChests.Enqueue(ChestTiersAtDeath); // the chest keeps its boss's config
                return;
            }

            // VS Gold Fever (Gilded Charm): while it runs, each kill has a 75% chance to drop a fever coin.
            SoulHunter.Gameplay.Pickups.GoldFever.OnEnemyKilled(transform.position);

            if (DropsChest || _dropsChestThisLife)
            {
                if (SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance != null)
                {
                    var chest = SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance.SpawnChest(transform.position);
                    var chestPickup = chest != null ? chest.GetComponent<SoulHunter.Gameplay.Pickups.ChestPickup>() : null;
                    // P1-14 (2026-10-09): the chest's tiers and evolution flag (its bronze/silver look) come from the boss that
                    // carried it, not from the stage clock at death: a 9:00 elite killed after 10:00 still drops a bronze chest.
                    if (chestPickup != null) chestPickup.Tiers = ChestTiersAtDeath;
                    // Owner's story (2026-09-27): Kael harvests souls, so a boss or chest elite also leaves its big
                    // soul gem (EnemyData.ExperienceValue: 50 elite, 250 boss), set a little aside from the chest.
                    SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance.SpawnGem(transform.position + new Vector3(-1.2f, 0f, 0f), GemValue);
                    Debug.Log("Boss killed! Chest dropped!");
                }
                return;
            }

            // Owner (2026-09-29, heavy-kill pass): "VS rule: no enemy drops". VS wiki: Floor Chicken, Gold Coin, Coin Bag,
            // Vacuum and Orologion drop from light sources (Floor_Chicken, Gold_Coin_(pickup), Coin_Bag, Vacuum and
            // Orologion pages); enemies drop Experience Gems (Experience_Gem). Before, each kill also rolled a Magnet (1%),
            // a Time Freeze (1%), a coin (2%) and a chicken (5%, which replaced the gem), and with a strong hero thousands of
            // coins and chickens piled up on the ground and slowed the game. Those now come from Soul Lanterns, stage items,
            // chests and Gold Fever only. EnemyData's DropChanceChicken/DropChanceGold/GoldValue are no longer used here.
            if (SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance != null)
            {
                SoulHunter.Gameplay.Pickups.PickupPoolManager.Instance.SpawnGem(transform.position, GemValue);
            }
        }
    }
}
