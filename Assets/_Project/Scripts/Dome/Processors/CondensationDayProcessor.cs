using System.Collections.Generic;
using UnityEngine;
using Sporae.Dome.PotSystem.Growth;

public readonly struct CondensationDayResult
{
    public CondensationDayResult(bool applied, bool hasActiveLed, float production, float accumulation)
    {
        Applied = applied;
        HasActiveLed = hasActiveLed;
        Production = production;
        Accumulation = accumulation;
    }

    public bool Applied { get; }
    public bool HasActiveLed { get; }
    public float Production { get; }
    public float Accumulation { get; }
}

public sealed class CondensationDayProcessor
{
    public CondensationDayResult Apply(GameManager gameManager, List<PotStateModel> registeredPots)
    {
        if (gameManager == null || gameManager.CondensationSystem == null)
            return new CondensationDayResult(false, false, 0f, 0f);

        bool hasActiveLed = false;
        foreach (var pot in registeredPots)
        {
            if (pot == null || !pot.HasPlant)
                continue;

            if (pot.LedSystemState != LedSystemState.Off)
            {
                hasActiveLed = true;
                break;
            }
        }

        gameManager.CondensationSystem.DayChanged(registeredPots, hasActiveLed);
        gameManager.NotifyCondensationChanged();

        return new CondensationDayResult(
            applied: true,
            hasActiveLed: hasActiveLed,
            production: gameManager.CondensationSystem.DailyProduction,
            accumulation: gameManager.CondensationSystem.CurrentAccumulation);
    }
}
