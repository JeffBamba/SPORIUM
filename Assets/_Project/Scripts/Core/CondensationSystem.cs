using UnityEngine;
using Sporae.DevTools;

namespace _Project
{
    public class CondensationSystem
    {
        private CondensationConfig _config;
        private float _condensationAmount;
        
        public float CondensationAmount => _condensationAmount;
        
        // BUG FIX: Valori default se config non trovato
        private const float DEFAULT_GROWTH_PER_DAY = 3f;
        private const float DEFAULT_MAX_CONDENSATION = 10f;
        
        public CondensationSystem()
        {
            _config = Resources.Load<CondensationConfig>("Configs/CondensationConfig");
            if (_config == null)
            {
                SporiumLogger.LogError(LogCategory.Core, "CondensationConfig non trovato! Usando valori default.");
            }
        }
        
        public void DayChanged()
        {
            float growthPerDay = _config != null ? _config.CondensationGrowthPerDay : DEFAULT_GROWTH_PER_DAY;
            float maxCondensation = _config != null ? _config.MaxCondensation : DEFAULT_MAX_CONDENSATION;
            
            _condensationAmount += growthPerDay;
            _condensationAmount = Mathf.Clamp(_condensationAmount, 0f, maxCondensation);
        }

        public void Reset()
        {
            _condensationAmount = 0f;
        }

        public float GetMax()
        {
            return _config != null ? _config.MaxCondensation : DEFAULT_MAX_CONDENSATION;
        }
    }
}