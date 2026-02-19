using _Project.Sporae.Core;

namespace _Project.Systems.FoodRoom
{
    public enum SlotState
    {
        Free,
        Growing,
        Ready
    }

    public class FoodProductionSlot
    {
        public FoodProductionType Type;
        public int DaysRemaining;
        public int StartDay;
        public bool HasStemCell;
        public string StemCellTypeId;
        public SlotState State;

        public bool IsFree => State == SlotState.Free;
        public bool IsGrowing => State == SlotState.Growing;
        public bool IsReady => State == SlotState.Ready;
    }
}
