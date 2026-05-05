using System;
using _Project;
using _Project.Sporae.Core;

namespace Sporae.Core
{
    public class EconomySystem
    {
        private readonly DiaryStatistics _diaryStatistics;
        
        public int CurrentCRY { get; private set; }
        public int MaxCRY { get; private set; } = 999999; // Limite massimo ragionevole
        
        public event Action<int> OnCRYChanged;

        public EconomySystem(int startingCRY)
        {
            _diaryStatistics = ServiceContainer.Instance.Get<DiaryStatistics>();
            CurrentCRY = Math.Max(0, startingCRY);
        }

        public bool CanAfford(int amount)
        {
            return amount >= 0 && CurrentCRY >= amount;
        }

        public bool Add(int amount, CryIncomeLedgerCategory ledgerCategory = CryIncomeLedgerCategory.Other)
        {
            if (amount <= 0) 
                return false;
            
            // BUG FIX: Se già al max, operazione è "riuscita" (non c'è errore)
            if (CurrentCRY >= MaxCRY)
                return true;
            
            _diaryStatistics.CryEarned += amount;
            _diaryStatistics.RegisterCryIncomeLedger(amount, ledgerCategory);
            
            int newAmount = Math.Min(CurrentCRY + amount, MaxCRY);
            CurrentCRY = newAmount;
            OnCRYChanged?.Invoke(CurrentCRY);
            return true;
        }

        public bool Spend(int amount, CrySpendLedgerCategory ledgerCategory = CrySpendLedgerCategory.Other)
        {
            if (!CanAfford(amount))
                return false;
            
            _diaryStatistics.CrySpent += amount;
            _diaryStatistics.RegisterCrySpendLedger(amount, ledgerCategory);
            
            CurrentCRY -= amount;
            OnCRYChanged?.Invoke(CurrentCRY);
            return true;
        }

        public float GetCRYPercentage()
        {
            return (float)CurrentCRY / MaxCRY;
        }

        public void SetCRY(int amount)
        {
            int clampedAmount = Math.Max(0, Math.Min(amount, MaxCRY));
            if (clampedAmount != CurrentCRY)
            {
                CurrentCRY = clampedAmount;
                OnCRYChanged?.Invoke(CurrentCRY);
            }
        }
        
        /// <summary>
        /// Ripristina lo stato del sistema economico da dati salvati.
        /// Usato durante il caricamento del gioco.
        /// </summary>
        /// <param name="cryAmount">Quantità di CRY da ripristinare</param>
        public void RestoreState(int cryAmount)
        {
            SetCRY(cryAmount);
        }
    }
}
