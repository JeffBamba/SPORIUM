using System;
using UnityEngine;

/// <summary>
/// Modello dati per lo stato di un vaso.
/// Contiene tutte le informazioni necessarie per tracciare lo stato della pianta,
/// idratazione, esposizione alla luce e timestamp delle azioni.
/// Compatibile con BLK-01.04 (crescita) e BLK-01.05 (frutto).
/// </summary>
[Serializable]
public class PotStateModel
{
    [Header("Pot Identification")]
    public string PotId;          // "POT-001", "POT-002"
    
    [Header("Plant State")]
    public bool HasPlant;         // True se il vaso contiene una pianta
    public int Stage;             // 0=Seeded (placeholder BLK-01.02), 1-3 per crescita futura
    
    [Header("Resource Levels")]
    public int Hydration;         // 0..MaxHydration
    public int LightExposure;     // 0..MaxLightExposure
    
    [Header("Growth System (BLK-01.03A)")]
    public int GrowthPoints;              // Progress interno allo stadio attuale
    public int DaysSincePlant;            // Giorni dalla semina
    public int DaysNeglectedStreak;       // Giorni consecutivi senza cura
    
    [Header("Timestamps (BLK-01.03A)")]
    public int PlantedDay;        // Giorno in cui è stato piantato il seme
    public int LastWateredDay;    // Ultimo giorno di annaffiatura
    public int LastLitDay;        // Ultimo giorno di illuminazione
    
    /// <summary>
    /// Crea un nuovo stato di vaso vuoto
    /// </summary>
    public PotStateModel(string potId)
    {
        PotId = potId;
        HasPlant = false;
        Stage = 0;
        Hydration = 0;
        LightExposure = 0;
        GrowthPoints = 0;
        DaysSincePlant = 0;
        DaysNeglectedStreak = 0;
        PlantedDay = 0;
        LastWateredDay = 0;
        LastLitDay = 0;
    }
    
    /// <summary>
    /// Crea un nuovo stato di vaso con pianta
    /// </summary>
    public PotStateModel(string potId, int plantedDay)
    {
        PotId = potId;
        HasPlant = true;
        Stage = 1; // Seeded (1 = Seed, non 0 = Empty)
        Hydration = 0;
        LightExposure = 0;
        GrowthPoints = 0;
        DaysSincePlant = 0;
        DaysNeglectedStreak = 0;
        PlantedDay = plantedDay;
        LastWateredDay = 0;
        LastLitDay = 0;
    }
    
    /// <summary>
    /// Verifica se il vaso è vuoto
    /// </summary>
    public bool IsEmpty => !HasPlant;
    
    /// <summary>
    /// Verifica se il vaso ha una pianta
    /// </summary>
    public bool HasPlantGrowing => HasPlant && Stage >= 1; // 1 = Seed, 2 = Sprout, 3 = Mature
    
    /// <summary>
    /// Verifica se l'idratazione è al massimo
    /// </summary>
    public bool IsHydrationMax(int maxHydration) => Hydration >= maxHydration;
    
    /// <summary>
    /// Verifica se l'esposizione alla luce è al massimo
    /// </summary>
    public bool IsLightExposureMax(int maxLightExposure) => LightExposure >= maxLightExposure;
    
    /// <summary>
    /// Aumenta l'idratazione di 1, rispettando il limite massimo
    /// </summary>
    public bool IncreaseHydration(int maxHydration)
    {
        if (Hydration < maxHydration)
        {
            Hydration++;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Aumenta l'esposizione alla luce di 1, rispettando il limite massimo
    /// </summary>
    public bool IncreaseLightExposure(int maxLightExposure)
    {
        if (LightExposure < maxLightExposure)
        {
            LightExposure++;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Piantare un seme nel vaso
    /// </summary>
    public void PlantSeed(int currentDay)
    {
        HasPlant = true;
        Stage = 1; // Seeded (1 = Seed, non 0 = Empty)
        Hydration = 0;
        LightExposure = 0;
        GrowthPoints = 0;
        DaysSincePlant = 0;
        DaysNeglectedStreak = 0;
        PlantedDay = currentDay;
        LastWateredDay = 0;
        LastLitDay = 0;
    }
    
    /// <summary>
    /// Aggiorna il timestamp dell'ultima annaffiatura
    /// </summary>
    public void UpdateWateringDay(int currentDay)
    {
        LastWateredDay = currentDay;
    }
    
    /// <summary>
    /// Aggiorna il timestamp dell'ultima illuminazione
    /// </summary>
    public void UpdateLightingDay(int currentDay)
    {
        LastLitDay = currentDay;
    }
    
    /// <summary>
    /// Resetta il vaso allo stato vuoto
    /// </summary>
    public void ResetToEmpty()
    {
        HasPlant = false;
        Stage = 0; // Empty (0 = Empty, 1 = Seed)
        Hydration = 0;
        LightExposure = 0;
        GrowthPoints = 0;
        DaysSincePlant = 0;
        DaysNeglectedStreak = 0;
        PlantedDay = 0;
        LastWateredDay = 0;
        LastLitDay = 0;
    }
    
    /// <summary>
    /// Restituisce una descrizione testuale dello stato
    /// </summary>
    public string GetStatusDescription()
    {
        if (!HasPlant)
        {
            return "Vaso vuoto";
        }
        
        string status = $"Pianta ({GetStageName(Stage)})";
        
        if (Hydration > 0)
        {
            status += $", Idratazione: {Hydration}";
        }
        
        if (LightExposure > 0)
        {
            status += $", Luce: {LightExposure}";
        }
        
        return status;
    }
    
    /// <summary>
    /// Restituisce una rappresentazione stringa del modello
    /// </summary>
    public override string ToString()
    {
        return $"[{PotId}] Plant:{HasPlant} Stage:{Stage}({GetStageName(Stage)}) H:{Hydration} L:{LightExposure} GP:{GrowthPoints} Day:{PlantedDay}";
    }
    
    /// <summary>
    /// Restituisce il nome localizzato per uno stadio
    /// </summary>
    private string GetStageName(int stage)
    {
        switch (stage)
        {
            case 0: return "Empty";
            case 1: return "Seed";
            case 2: return "Sprout";
            case 3: return "Mature";
            default: return $"Stadio {stage}";
        }
    }
}
