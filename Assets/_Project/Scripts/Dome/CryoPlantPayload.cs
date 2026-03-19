using System;
using UnityEngine;
using _Project.Sporae.Core;

/// <summary>
/// Snapshot serializzabile dei campi identitari di una pianta al momento del trasferimento
/// in un CryoSlot. Preserva genetica, livello, tratti e poteri senza dipendere da PotStateModel.
/// </summary>
[Serializable]
public class CryoPlantPayload
{
    public string PlantCode;
    public int PlantLevel;
    public string PlantFamilyMetadata;
    public int PlantGeneticType; // GeneticType enum salvato come int per serializzazione JSON
    public string SelectedTraitsCsv;
    public int TraitPowerPercent = 100;
    public string CustomPlantName;
    public string ActivePowerLabel;
    public string PassivePowerLabel;
    public bool IsHybrid;
    public bool IsMutated;
    public string ParentFamilyA;
    public string ParentFamilyB;
    public string SourcePlantDisplayName;

    /// <summary>
    /// Costruisce un payload dallo stato corrente di un PotStateModel.
    /// </summary>
    public static CryoPlantPayload FromPotState(PotStateModel s)
    {
        if (s == null)
            return null;

        return new CryoPlantPayload
        {
            PlantCode              = s.PlantCode,
            PlantLevel             = s.PlantLevel,
            PlantFamilyMetadata    = s.PlantFamilyMetadata,
            PlantGeneticType       = (int)s.PlantGeneticType,
            SelectedTraitsCsv      = s.SelectedTraitsCsv,
            TraitPowerPercent      = s.TraitPowerPercent,
            CustomPlantName        = s.CustomPlantName,
            ActivePowerLabel       = s.ActivePowerLabel,
            PassivePowerLabel      = s.PassivePowerLabel,
            IsHybrid               = s.IsHybrid,
            IsMutated              = s.IsMutated,
            ParentFamilyA          = s.ParentFamilyA,
            ParentFamilyB          = s.ParentFamilyB,
            SourcePlantDisplayName = s.SourcePlantDisplayName,
        };
    }

    /// <summary>
    /// Reinietta i campi identitari del payload in un PotStateModel esistente.
    /// Usato quando si reimpianta una pianta da cryo in un pot attivo.
    /// </summary>
    public void ApplyToPotState(PotStateModel s)
    {
        if (s == null) return;

        s.PlantCode              = PlantCode;
        s.PlantLevel             = PlantLevel;
        s.PlantFamilyMetadata    = PlantFamilyMetadata;
        s.PlantGeneticType       = (GeneticType)PlantGeneticType;
        s.SelectedTraitsCsv      = SelectedTraitsCsv;
        s.TraitPowerPercent      = TraitPowerPercent;
        s.CustomPlantName        = CustomPlantName;
        s.ActivePowerLabel       = ActivePowerLabel;
        s.PassivePowerLabel      = PassivePowerLabel;
        s.IsHybrid               = IsHybrid;
        s.IsMutated              = IsMutated;
        s.ParentFamilyA          = ParentFamilyA;
        s.ParentFamilyB          = ParentFamilyB;
        s.SourcePlantDisplayName = SourcePlantDisplayName;
        s.IsInPassiveSlot        = false;
    }
}
