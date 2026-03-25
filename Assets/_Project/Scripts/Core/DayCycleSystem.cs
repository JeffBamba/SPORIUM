using System;
using UnityEngine;
using Object = UnityEngine.Object;
using Sporae.DevTools;
using _Project.Systems.FoodRoom;

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

            if (_fadeToBlackAnimation != null)
                _fadeToBlackAnimation.OnFaded += HandleFaded;
        }

        /// <summary>
        /// Scollega HandleFaded dalla FadeToBlackAnimation. Da chiamare prima di creare una nuova istanza
        /// per evitare che la vecchia istanza rimanga agganciata e avanzi il giorno una seconda volta.
        /// </summary>
        public void Dispose()
        {
            if (_fadeToBlackAnimation != null)
                _fadeToBlackAnimation.OnFaded -= HandleFaded;
        }

        private void HandleFaded()
        {
            CurrentDay++;
            /* Al mattino dopo End of Day la potabilizzazione deve essere completata (tempo reale passato = notte). */
            if (_gameManager?.FoodRoomSystem != null)
                _gameManager.FoodRoomSystem.AdvanceWaterProductionByRealSeconds(8 * 3600f);
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