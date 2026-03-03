namespace _Project.Systems.FoodRoom
{
    public class WaterProductionSlot
    {
        /// <summary>Total units to process (raw water consumed at start).</summary>
        public int RawWaterInput;
        /// <summary>Units already produced and ready to collect.</summary>
        public int PotableWaterOutput;
        /// <summary>Progress 0-1 for the current unit being processed.</summary>
        public float CurrentUnitProgress;
        public bool IsActive;

        /// <summary>Real seconds per one unit (2 minutes).</summary>
        public const float SecondsPerUnit = 120f;
    }
}
