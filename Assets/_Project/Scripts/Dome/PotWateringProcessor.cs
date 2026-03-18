using _Project;
using _Project.Sporae.Core;
using Sporae.DevTools;

public sealed class PotWateringProcessor
{
    public bool ApplyToggle(PotStateModel potState, PotSlot potSlot, bool showDebugLogs)
    {
        if (potState == null || potSlot == null)
            return false;

        potState.WateringSystemOn = !potState.WateringSystemOn;

        if (!potState.WateringSystemOn)
        {
            potState.DaysWateringSystemOn = 0;
            potState.WateringRawWaterAccumulator = 0f;
        }

        var dayActivityLog = ServiceContainer.Instance.Get<DayActivityLog>(suppressWarning: true);
        if (dayActivityLog != null)
            dayActivityLog.RecordWateringToggle(potSlot.PotId, potState.WateringSystemOn);

        var diaryStats = ServiceContainer.Instance.Get<DiaryStatistics>(suppressWarning: true);
        if (diaryStats != null && potState.WateringSystemOn)
            diaryStats.PlantsWatered++;

        PotEvents.EmitAction(PotEvents.PotActionType.Water, potSlot);
        PotEvents.EmitChanged(potSlot);

        if (showDebugLogs)
        {
            string stateMsg = potState.WateringSystemOn ? "ON" : "OFF";
            SporiumLogger.LogInfo(LogCategory.Pot, $"[ACT-002][{potSlot.PotId}] Watering System Toggle: {stateMsg} (consumo risorse a fine giornata)");
        }

        return true;
    }
}
