using _Project.Sporae.Core;
using Sporae.Core;

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

        /// <summary>Entrate da vendite mercato nero (oggi).</summary>
        public int CryIncomeBlackMarket { get; private set; }
        /// <summary>Entrate premi/missioni (oggi).</summary>
        public int CryIncomeMission { get; private set; }
        /// <summary>Altre entrate non classificate (oggi).</summary>
        public int CryIncomeOther { get; private set; }

        /// <summary>Uscite manutenzione Dome (energia alba + seed storage + cucina ricorrente).</summary>
        public int CrySpendDomeUpkeep { get; private set; }
        /// <summary>Acquisti mercato nero.</summary>
        public int CrySpendBlackMarket { get; private set; }
        /// <summary>Altre uscite (elevator, azioni varie, ecc.).</summary>
        public int CrySpendOther { get; private set; }

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
            CryIncomeBlackMarket = 0;
            CryIncomeMission = 0;
            CryIncomeOther = 0;
            CrySpendDomeUpkeep = 0;
            CrySpendBlackMarket = 0;
            CrySpendOther = 0;
            FruitsHarvested = 0;
            SporesExtracted = 0;
            PlantsWatered = 0;
        }

        public void RegisterCryIncomeLedger(int amount, CryIncomeLedgerCategory category)
        {
            if (amount <= 0) return;
            switch (category)
            {
                case CryIncomeLedgerCategory.BlackMarketSell:
                    CryIncomeBlackMarket += amount;
                    break;
                case CryIncomeLedgerCategory.MissionReward:
                    CryIncomeMission += amount;
                    break;
                default:
                    CryIncomeOther += amount;
                    break;
            }
        }

        public void RegisterCrySpendLedger(int amount, CrySpendLedgerCategory category)
        {
            if (amount <= 0) return;
            switch (category)
            {
                case CrySpendLedgerCategory.DomeUpkeep:
                    CrySpendDomeUpkeep += amount;
                    break;
                case CrySpendLedgerCategory.BlackMarketBuy:
                    CrySpendBlackMarket += amount;
                    break;
                default:
                    CrySpendOther += amount;
                    break;
            }
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