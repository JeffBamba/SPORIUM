using System;
using UnityEngine;
using Sporae.Dome.PotSystem.Growth;

/// <summary>
/// Sistema di eventi per il sistema dei vasi.
/// Fornisce eventi statici per la comunicazione tra componenti del sistema.
/// Utilizzato per notificare azioni sui vasi e cambiamenti di stato.
/// </summary>
public static class PotEvents
{
    /// <summary>
    /// Tipi di azioni disponibili sui vasi
    /// </summary>
    public enum PotActionType 
    { 
        Plant,      // ACT-001: Piantare seme
        Water,      // ACT-002: Annaffiare pianta
        Light,       // ACT-003: Illuminare pianta
        Uproot       // ACT-004: Uproot pianta
    }
    
    /// <summary>
    /// Evento emesso quando viene eseguita un'azione su un vaso
    /// </summary>
    public static event Action<PotActionType, PotSlot> OnPotAction;
    
    /// <summary>
    /// Evento emesso quando cambia lo stato di un vaso
    /// </summary>
    public static event Action<PotSlot> OnPotStateChanged;
    
    /// <summary>
    /// Evento emesso quando un vaso viene selezionato
    /// </summary>
    public static event Action<PotSlot> OnPotSelected;
    
    /// <summary>
    /// Evento emesso quando un'azione fallisce (risorse insufficienti, gating non soddisfatto)
    /// </summary>
    public static event Action<PotActionType, PotSlot, string> OnPotActionFailed;
    
    /// <summary>
    /// Evento emesso quando una pianta cresce (punti aggiunti)
    /// </summary>
    public static event Action<string, PlantStage, int, int> OnPlantGrew;
    
    /// <summary>
    /// Evento emesso quando una pianta cambia stadio
    /// </summary>
    public static event Action<string, PlantStage> OnPlantStageChanged;
    
    /// <summary>
    /// Emette l'evento di azione su vaso
    /// </summary>
    /// <param name="type">Tipo di azione eseguita</param>
    /// <param name="pot">Vaso su cui è stata eseguita l'azione</param>
    public static void EmitAction(PotActionType type, PotSlot pot)
    {
        if (pot == null)
        {
            Debug.LogWarning("[PotEvents] Tentativo di emettere evento con vaso null");
            return;
        }
        
        Debug.Log($"[PotEvents] Emesso evento OnPotAction: {type} su {pot.PotId}");
        OnPotAction?.Invoke(type, pot);
    }
    
    /// <summary>
    /// Emette l'evento di cambio stato vaso
    /// </summary>
    /// <param name="pot">Vaso il cui stato è cambiato</param>
    public static void EmitChanged(PotSlot pot)
    {
        if (pot == null)
        {
            Debug.LogWarning("[PotEvents] Tentativo di emettere evento con vaso null");
            return;
        }
        
        Debug.Log($"[PotEvents] Emesso evento OnPotStateChanged per {pot.PotId}");
        OnPotStateChanged?.Invoke(pot);
    }
    
    /// <summary>
    /// Emette l'evento di selezione vaso
    /// </summary>
    /// <param name="pot">Vaso selezionato</param>
    public static void EmitSelected(PotSlot pot)
    {
        if (pot == null)
        {
            Debug.LogWarning("[PotEvents] Tentativo di emettere evento con vaso null");
            return;
        }
        
        Debug.Log($"[PotEvents] Emesso evento OnPotSelected per {pot.PotId}");
        OnPotSelected?.Invoke(pot);
    }
    
    /// <summary>
    /// Emette l'evento di fallimento azione
    /// </summary>
    /// <param name="type">Tipo di azione fallita</param>
    /// <param name="pot">Vaso su cui è fallita l'azione</param>
    /// <param name="reason">Motivo del fallimento</param>
    public static void EmitActionFailed(PotActionType type, PotSlot pot, string reason)
    {
        if (pot == null)
        {
            Debug.LogWarning("[PotEvents] Tentativo di emettere evento con vaso null");
            return;
        }
        
        Debug.LogWarning($"[PotEvents] Azione {type} fallita su {pot.PotId}: {reason}");
        OnPotActionFailed?.Invoke(type, pot, reason);
    }
    
    /// <summary>
    /// Restituisce il nome localizzato per un tipo di azione
    /// </summary>
    /// <param name="actionType">Tipo di azione</param>
    /// <returns>Nome localizzato dell'azione</returns>
    public static string GetActionName(PotActionType actionType)
    {
        switch (actionType)
        {
            case PotActionType.Plant:
                return "Piantare";
            case PotActionType.Water:
                return "Annaffiare";
            case PotActionType.Light:
                return "Illuminare";
            default:
                return "Sconosciuto";
        }
    }
    
    /// <summary>
    /// Restituisce il codice univoco per un tipo di azione
    /// </summary>
    /// <param name="actionType">Tipo di azione</param>
    /// <returns>Codice univoco dell'azione</returns>
    public static string GetActionCode(PotActionType actionType)
    {
        switch (actionType)
        {
            case PotActionType.Plant:
                return "ACT-001";
            case PotActionType.Water:
                return "ACT-002";
            case PotActionType.Light:
                return "ACT-003";
            default:
                return "ACT-000";
        }
    }
    
    /// <summary>
    /// Restituisce la descrizione per un tipo di azione
    /// </summary>
    /// <param name="actionType">Tipo di azione</param>
    /// <returns>Descrizione dell'azione</returns>
    public static string GetActionDescription(PotActionType actionType)
    {
        switch (actionType)
        {
            case PotActionType.Plant:
                return "Piantare un seme generico nel vaso";
            case PotActionType.Water:
                return "Aumentare l'idratazione della pianta";
            case PotActionType.Light:
                return "Aumentare l'esposizione alla luce della pianta";
            default:
                return "Azione non riconosciuta";
        }
    }
    
    /// <summary>
    /// Restituisce il costo in azioni per un tipo di azione
    /// </summary>
    /// <param name="actionType">Tipo di azione</param>
    /// <returns>Costo in azioni (sempre 1 per BLK-01.02)</returns>
    public static int GetActionCost(PotActionType actionType)
    {
        // Per BLK-01.02, tutte le azioni costano 1 azione
        return 1;
    }
    
    /// <summary>
    /// Restituisce il costo in CRY per un tipo di azione
    /// </summary>
    /// <param name="actionType">Tipo di azione</param>
    /// <returns>Costo in CRY (sempre 1 per BLK-01.02)</returns>
    public static int GetCryCost(PotActionType actionType)
    {
        // Per BLK-01.02, tutte le azioni costano 1 CRY (GDD Blocking_01)
        return 1;
    }
    
    /// <summary>
    /// Emette l'evento di crescita pianta
    /// </summary>
    /// <param name="potId">ID del vaso</param>
    /// <param name="stage">Stadio corrente della pianta</param>
    /// <param name="addedPoints">Punti aggiunti oggi</param>
    /// <param name="totalPoints">Punti totali nello stadio</param>
    public static void RaiseOnPlantGrew(string potId, PlantStage stage, int addedPoints, int totalPoints)
    {
        Debug.Log($"[PotEvents] Emesso evento OnPlantGrew: {potId} - {stage} (+{addedPoints} punti, totali: {totalPoints})");
        OnPlantGrew?.Invoke(potId, stage, addedPoints, totalPoints);
    }
    
    /// <summary>
    /// Emette l'evento di cambio stadio pianta
    /// </summary>
    /// <param name="potId">ID del vaso</param>
    /// <param name="stage">Nuovo stadio della pianta</param>
    public static void RaiseOnPlantStageChanged(string potId, PlantStage stage)
    {
        Debug.Log($"[PotEvents] Emesso evento OnPlantStageChanged: {potId} - Nuovo stadio: {stage}");
        OnPlantStageChanged?.Invoke(potId, stage);
    }
    
    /// <summary>
    /// BLK-01.04: Emette l'evento di cambio stadio pianta (alias per compatibilità)
    /// </summary>
    /// <param name="potId">ID del vaso</param>
    /// <param name="stage">Nuovo stadio della pianta</param>
    public static void EmitPlantStageChanged(string potId, PlantStage stage)
    {
        RaiseOnPlantStageChanged(potId, stage);
    }
}
