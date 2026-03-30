using System;
using UnityEngine;
using _Project.Sporae.Core;
using Sporae.Dome.PotSystem.Growth;
using Sporae.DevTools;

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
    public string LabCareProfileMetadata;
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
            LabCareProfileMetadata = s.LabCareProfileMetadata,
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
        s.LabCareProfileMetadata = LabCareProfileMetadata;
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

    /// <summary>
    /// Payload per riempire la Cryo da debug (Task 4 / QA) senza trasferimento da vaso.
    /// </summary>
    public static CryoPlantPayload FromPlantCodeDebug(string plantCode, int plantLevel = 3)
    {
        if (string.IsNullOrWhiteSpace(plantCode))
            return null;
        plantCode = plantCode.Trim();
        if (PlantDatabase.Instance == null)
        {
            SporiumLogger.LogWarning(LogCategory.Dome, "FromPlantCodeDebug: PlantDatabase non inizializzato");
            return null;
        }

        var pd = PlantDatabase.Instance.GetPlantDataByCode(plantCode);
        if (pd == null)
        {
            SporiumLogger.LogWarning(LogCategory.Dome, $"FromPlantCodeDebug: nessun PlantData per {plantCode}");
            return null;
        }

        string fam = ItemFabric.NormalizeFamily(pd.Family.ToString());
        string traits = ItemFabric.BuildCandidateTraitsCsv(fam, fam);
        return new CryoPlantPayload
        {
            PlantCode = plantCode,
            PlantLevel = Mathf.Max(1, plantLevel),
            PlantFamilyMetadata = fam,
            PlantGeneticType = (int)pd.DefaultGeneticType,
            SelectedTraitsCsv = traits,
            TraitPowerPercent = 100,
            ActivePowerLabel = pd.ActivePower,
            PassivePowerLabel = pd.PassivePower,
            SourcePlantDisplayName = pd.name,
            IsHybrid = false,
            IsMutated = false,
        };
    }
}
