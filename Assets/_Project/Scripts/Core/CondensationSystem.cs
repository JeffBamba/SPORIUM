using UnityEngine;

namespace _Project
{
    public class CondensationSystem
    {
        private CondensationConfig _config;
        private float _condensationAmount;
        
        public float CondensationAmount => _condensationAmount;
        
        public CondensationSystem()
        {
            _config = Resources.Load<CondensationConfig>("Configs/CondensationConfig");
        }
        
        public void DayChanged()
        {
            _condensationAmount += _config.CondensationGrowthPerDay;
            _condensationAmount = Mathf.Clamp(_condensationAmount, 0f, _config.MaxCondensation);
        }

        public void Reset()
        {
            _condensationAmount = 0f;
        }

        public float GetMax()
        {
            return _config.MaxCondensation;
        }
    }
}