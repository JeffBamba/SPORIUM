using System;
using UnityEngine;
using Object = UnityEngine.Object;
using Sporae.DevTools;

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

        /// <summary>
        /// Imposta il giorno corrente (usato dal SaveManager al caricamento).
        /// Notifica la UI tramite OnDayChanged.
        /// </summary>
        public void SetCurrentDay(int day)
        {
            if (day < 1) return;
            CurrentDay = day;
            OnDayChanged?.Invoke(CurrentDay);
        }

        public bool CanEndDay()
        {
            // BUG FIX: Controllo null per evitare crash
            if (_gameManager == null || _gameManager.EconomySystem == null)
            {
                SporiumLogger.LogWarning(LogCategory.Core, "GameManager o EconomySystem non disponibili!");
                return false;
            }
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