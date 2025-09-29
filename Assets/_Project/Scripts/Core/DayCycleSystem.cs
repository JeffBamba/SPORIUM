using System;

namespace _Project.Sporae.Core
{
    public class DayCycleSystem
    {
        public event Action<int> OnDayChanged;
        public int CurrentDay { get; private set; } = 1;
        
        public void EndDay()
        {
            CurrentDay++;
            OnDayChanged?.Invoke(CurrentDay); 
        }
    }
}