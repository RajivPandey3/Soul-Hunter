using SoulHunter.Core.Persistence;
using UnityEngine;
using System;

namespace SoulHunter.Core.Services
{
    /// <summary>
    /// Learning Comment:
    /// Game ki economy (Gold) ko manage karne ki Service.
    /// Ye EventBus/SaveService ki tarah decoupled hai. Gold coins uthane par ye add karegi,
    /// aur Meta Progression (Menu) mein kharch karne par minus karegi.
    /// </summary>
    public class EconomyService : IGameService
    {
        private SaveService _saveService;
        
        public int CurrentGold => _saveService != null ? _saveService.CurrentData.Currency : 0;
        
        public event Action<int> OnGoldChanged;

        // Learning Comment: Exposes harvested souls collected in the current run for mid-stage transactions.
        public int HarvestedSouls { get; private set; }

        // Learning Comment: Event fired whenever the harvested souls balance changes.
        public event Action<int> OnSoulsChanged;

        public void Initialize()
        {
            _saveService = GameServices.Instance.Get<SaveService>();
            HarvestedSouls = 0;
            Debug.Log($"[EconomyService] Initialized. Current Gold: {CurrentGold}, Harvested Souls: {HarvestedSouls}");
        }

        public void Terminate()
        {
            // Do cleanup if necessary
        }

        public void AddGold(int amount)
        {
            if (_saveService == null || _saveService.CurrentData == null) return;
            
            _saveService.CurrentData.Currency += amount;
            _saveService.SaveGame(); // Save immediately (VS does this, or we can save at end of round)
            
            OnGoldChanged?.Invoke(_saveService.CurrentData.Currency);
        }

        public bool SpendGold(int amount)
        {
            if (_saveService == null || _saveService.CurrentData == null) return false;
            
            if (_saveService.CurrentData.Currency >= amount)
            {
                _saveService.CurrentData.Currency -= amount;
                _saveService.SaveGame();
                OnGoldChanged?.Invoke(_saveService.CurrentData.Currency);
                return true;
            }
            return false;
        }

        // Learning Comment: Adds harvested souls to the current run total and invokes OnSoulsChanged.
        public void AddSouls(int amount)
        {
            if (amount <= 0) return;
            HarvestedSouls += amount;
            OnSoulsChanged?.Invoke(HarvestedSouls);
        }

        // Learning Comment: Deducts harvested souls if available, returning true on success, or false if insufficient.
        public bool SpendSouls(int amount)
        {
            if (amount < 0) return false;
            if (HarvestedSouls >= amount)
            {
                HarvestedSouls -= amount;
                OnSoulsChanged?.Invoke(HarvestedSouls);
                return true;
            }
            return false;
        }

        // Learning Comment: Resets harvested souls to zero for run restarts and notifies listeners.
        public void ResetHarvestedSouls()
        {
            HarvestedSouls = 0;
            OnSoulsChanged?.Invoke(HarvestedSouls);
        }
    }
}
