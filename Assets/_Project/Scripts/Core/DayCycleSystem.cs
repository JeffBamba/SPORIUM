using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Project.Sporae.Core
{
    public class DayCycleSystem
    {
        public event Action<int> OnDayChanged;
        public int CurrentDay { get; private set; } = 1;
        public int DailyPowerCost { get; set; } = 20;

        private readonly GameManager _gameManager;
        private readonly FadeToBlackAnimation _fadeToBlackAnimation;
        
        public DayCycleSystem(FadeToBlackAnimation fadeToBlackAnimation)
        {
            _fadeToBlackAnimation = fadeToBlackAnimation;
            _gameManager = Object.FindObjectOfType<GameManager>();

            _fadeToBlackAnimation.OnFaded += HandleFaded;
        }

        private void HandleFaded()
        {
            CurrentDay++;
            OnDayChanged?.Invoke(CurrentDay);
        }

        public bool CanEndDay()
        {
            return _gameManager.EconomySystem.CanAfford(DailyPowerCost);
        }
        
        public bool EndDay()
        {
            if (!CanEndDay())
                return false;
                
            _fadeToBlackAnimation.Show();
            
            return true;
        }
    }
}