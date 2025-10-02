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
        
        public void Reset()
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