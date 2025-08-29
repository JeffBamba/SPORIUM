using System;

namespace Sporae.Core
{
    public class EconomySystem
    {
        public int CurrentCRY { get; private set; }
        public int MaxCRY { get; private set; } = 999999; // Limite massimo ragionevole
        
        public event Action<int> OnCRYChanged;

        public EconomySystem(int startingCRY)
        {
            CurrentCRY = Math.Max(0, startingCRY);
        }

        public bool CanAfford(int amount)
        {
            return amount >= 0 && CurrentCRY >= amount;
        }

        public bool Add(int amount)
        {
            if (amount <= 0) return false;
            
            int newAmount = Math.Min(CurrentCRY + amount, MaxCRY);
            if (newAmount != CurrentCRY)
            {
                CurrentCRY = newAmount;
                OnCRYChanged?.Invoke(CurrentCRY);
                return true;
            }
            return false;
        }

        public bool Spend(int amount)
        {
            if (!CanAfford(amount)) return false;
            
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
    }
}
