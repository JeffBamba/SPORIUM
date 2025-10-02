using System;

using Object = UnityEngine.Object;

namespace _Project.Sporae.Core
{
    public class DayCycleSystem
    {
        public event Action<int> OnDayChanged;
        public int CurrentDay { get; private set; } = 1;
        public int DailyPowerCost { get; set; } = 20;

        private readonly GameManager _gameManager;
        
        public DayCycleSystem()
        {
            _gameManager = Object.FindObjectOfType<GameManager>();
        }

        public bool CanEndDay()
        {
            return _gameManager.EconomySystem.CanAfford(DailyPowerCost);
        }
        
        public bool EndDay()
        {
            if (!CanEndDay())
                return false;
                
            CurrentDay++;
            OnDayChanged?.Invoke(CurrentDay);

            return true;
        }
    }
}