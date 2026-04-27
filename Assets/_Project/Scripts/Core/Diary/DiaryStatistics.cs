using _Project.Sporae.Core;

namespace _Project
{
    public class DiaryStatistics
    {
        [System.Serializable]
        public struct SnapshotMetricsData
        {
            public int Day;
            public int ActionsUsed;
            public int ActionsMax;
            public int CryEarned;
            public int CrySpent;
            public int CurrentCry;
            public int HarvestCount;
            public int WaterCount;
            public int StageChangesCount;
            public int ActiveAlerts;
            public int ActiveMissionCount;
            public int CompletedMissionCount;
        }

        public int ActionsSpent { get; set; }
        public int CrySpent { get; set; }
        public int CryEarned { get; set; }
        public int FruitsHarvested { get; set; }
        public int SporesExtracted { get; set; }
        public int PlantsWatered { get; set; }
        public bool HasPreviousSnapshot { get; private set; }
        public SnapshotMetricsData PreviousSnapshot { get; private set; }

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

        public void StorePreviousSnapshot(SnapshotMetricsData snapshot)
        {
            PreviousSnapshot = snapshot;
            HasPreviousSnapshot = true;
        }

        public bool TryGetPreviousSnapshot(out SnapshotMetricsData snapshot)
        {
            snapshot = PreviousSnapshot;
            return HasPreviousSnapshot;
        }

        public void RestorePreviousSnapshot(bool hasSnapshot, SnapshotMetricsData snapshot)
        {
            HasPreviousSnapshot = hasSnapshot;
            PreviousSnapshot = snapshot;
        }
    }
}