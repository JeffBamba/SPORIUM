using _Project.Sporae.Core;

namespace _Project
{
    public class DiaryStatistics
    {
        public int ActionsSpent { get; set; }
        public int CrySpent { get; set; }
        public int CryEarned { get; set; }
        public int FruitsHarvested { get; set; }
        public int SporesExtracted { get; set; }
        public int PlantsWatered { get; set; }

        private readonly DayCycleSystem _dayCycleSystem;
        
        public DiaryStatistics()
        {
            _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
            _dayCycleSystem.OnDayChanged += Reset;
        }

        ~DiaryStatistics()
        {
            _dayCycleSystem.OnDayChanged -= Reset;
        }

        private void Reset(int i)
        {
            ActionsSpent = 0;
            CrySpent = 0;
            CryEarned = 0;
            FruitsHarvested = 0;
            SporesExtracted = 0;
            PlantsWatered = 0;
        }
    }
}