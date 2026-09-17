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

        public void Initialize()
        {
            _saveService = GameServices.Instance.Get<SaveService>();
            Debug.Log($"[EconomyService] Initialized. Current Gold: {CurrentGold}");
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
    }
}
