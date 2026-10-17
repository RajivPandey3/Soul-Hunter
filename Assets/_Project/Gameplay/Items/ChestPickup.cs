using UnityEngine;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Learning Comment:
    /// Boss ke marne par Chest (Khazana) nikalta hai.
    /// Jab player isey chhuta hai, toh ye ChestUI ko trigger karta hai.
    /// </summary>
    public class ChestPickup : MonoBehaviour
    {
        [SerializeField] private float _pickupRadius = 1.5f;
        [SerializeField] private LayerMask _playerLayer;
        private bool _isCollected = false;

        /// <summary>
        /// The config of the boss that dropped this chest (vampire.survivors.wiki/w/Treasure_Chest: "individually configured
        /// per boss enemy"): its 5/3/1-item chances, fallback, evolution flag and item slots. Set when dropped (EnemyDrop);
        /// a pooled chest starts with the stage boss's config.
        /// </summary>
        public SoulHunter.Gameplay.Core.ChestTiers Tiers { get => _tiers; set { _tiers = value; ApplyLook(); } }
        private SoulHunter.Gameplay.Core.ChestTiers _tiers = SoulHunter.Gameplay.Core.ChestTiers.StageBoss;

        /// <summary>VS: only chests whose boss config has evo=1 can evolve a weapon (silver); the others are bronze.</summary>
        public bool CanEvolve { get => _tiers.CanEvolve; set { _tiers.CanEvolve = value; ApplyLook(); } }

        /// <summary>VS Arcana mode: an Arcana chest (11:00 and 21:00) gives an Arcana choice instead of items.</summary>
        public bool IsArcana { get => _tiers.IsArcana; set { _tiers.IsArcana = value; ApplyLook(); } }

        /// <summary>VS: a bronze, silver or Arcana chest, shown by ChestLook.</summary>
        public ChestKind Kind => ChestLook.KindFor(_tiers.CanEvolve, _tiers.IsArcana);

        private void ApplyLook()
        {
            var visual = transform.Find("Visual");
            ChestLook.Apply(visual != null ? visual : transform, Kind);
        }

        private readonly Collider[] _playerHits = new Collider[1];

        private void Update()
        {
            if (_isCollected || !isActiveAndEnabled || Time.timeScale <= 0f) return;

            int hitCount = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, _pickupRadius, _playerHits, _playerLayer);
            if (hitCount > 0)
            {
                Collect(_playerHits[0].transform);
            }
        }

        private SoulHunter.Gameplay.Core.ChestLogicController _logicController;
        private SoulHunter.Gameplay.UI.ChestUI _chestUI;

        public void Initialize(SoulHunter.Gameplay.Core.ChestLogicController logicController, SoulHunter.Gameplay.UI.ChestUI chestUI)
        {
            _logicController = logicController;
            _chestUI = chestUI;
        }

        private void Collect(Transform playerTransform)
        {
            using (SoulHunter.Core.Services.SlowSpan.Measure("ChestPickup.Collect"))
            {
                if (_isCollected || !isActiveAndEnabled || Time.timeScale <= 0f) return;
                _isCollected = true;

                using (SoulHunter.Core.Services.SlowSpan.Measure("Chest.Vfx")) SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.ChestOpen, transform.position);

                // VS (wiki Treasure_Chest, Patch 0.2.4): "a brief moment of invulnerability after picking up a treasure"; 240 ms
                // (owner, 2026-10-01). Game time stands still while the chest screen is open, so it covers the resume.
                var pickerHealth = playerTransform != null ? playerTransform.GetComponentInParent<SoulHunter.Gameplay.Combat.HealthController>() : null;
                if (pickerHealth != null) pickerHealth.GrantInvulnerability(SoulHunter.Gameplay.Combat.HealthController.VsInvulnerabilityTime);

                // Chest UI aur Backend Logic Trigger karo
                string rewardMessage = "";
                if (_logicController != null)
                using (SoulHunter.Core.Services.SlowSpan.Measure("Chest.Process"))
                {
                    // VS Arcana mode: an Arcana chest gives an Arcana instead of items.
                    rewardMessage = IsArcana
                        ? _logicController.ProcessArcanaChest(_tiers, offerChoice: Application.isPlaying)
                        : _logicController.ProcessChest(_tiers); // UI bypass / Backend processing
                }
                else
                {
                    Debug.LogWarning("[ChestPickup] ChestLogicController nahi mila!");
                }

                // UI call
                if (_chestUI != null)
                {
                    var result = _logicController != null ? _logicController.LastResult : null;
                    // An Arcana chest that could give no Arcana opened as a bronze or silver chest (wiki Treasure_Chest, Arcana);
                    // ChestLogicController set that look. Every other chest shows its own look.
                    bool arcanaFellBack = IsArcana && result != null && !result.IsArcana;
                    if (result != null && !arcanaFellBack) result.Kind = Kind;
                    // VS Results: chests are listed by size (wiki Treasure_Chest: silver for 3 items, gold for 5).
                    if (SoulHunter.Gameplay.Core.RunStatsTracker.Instance != null)
                        SoulHunter.Gameplay.Core.RunStatsTracker.Instance.RecordPickup(PickupNames.ForChest(result != null ? result.ItemCount : 1, IsArcana && !arcanaFellBack));
                    if (result != null && result.ArcanaDraft != null && SoulHunter.Gameplay.Core.ArcanaManager.Instance != null)
                    {
                        // VS Arcana chest: the Arcana choice screen instead of the chest screen (release item P2-01).
                        _chestUI.RecordOpened(1);
                        var logic = _logicController;
                        var ui = _chestUI;
                        var tiers = _tiers;
                        SoulHunter.Gameplay.Core.ArcanaManager.Instance.OpenChestDraft(result.ArcanaDraft, logic, () =>
                        {
                            // VS Game Killer (0) (wiki Treasure_Chest: "Will provide two additional items if the player has Game
                            // Killer (0)"): after the pick, the chest's two extra items open on the usual chest screen.
                            int extra = SoulHunter.Gameplay.Core.ArcanaManager.ArcanaChestExtraItems;
                            if (extra <= 0 || logic == null || ui == null) return;
                            var weaponManager = SoulHunter.Gameplay.Combat.WeaponManager.Instance != null ? SoulHunter.Gameplay.Combat.WeaponManager.Instance : Object.FindFirstObjectByType<SoulHunter.Gameplay.Combat.WeaponManager>();
                            var levelUpManager = SoulHunter.Gameplay.Core.LevelUpManager.Instance != null ? SoulHunter.Gameplay.Core.LevelUpManager.Instance : Object.FindFirstObjectByType<SoulHunter.Gameplay.Core.LevelUpManager>();
                            string message = logic.ProcessChest(weaponManager,
                                levelUpManager, extra, tiers.AsRegular(false), Random.value);
                            if (logic.LastResult != null) logic.LastResult.Kind = ChestKind.Arcana;
                            ui.OpenChest(message, logic.LastResult, countAsChest: false);
                        });
                    }
                    else _chestUI.OpenChest(rewardMessage, result);
                }

                using (SoulHunter.Core.Services.SlowSpan.Measure("Chest.Deactivate")) gameObject.SetActive(false);
            }
        }

        public void ResetPickup()
        {
            _isCollected = false;
            _tiers = SoulHunter.Gameplay.Core.ChestTiers.StageBoss; // silver, as before
            ApplyLook();
        }
    }
}
