using System;
using _Project;
using _Project.Sporae.Core;
using Sporae.Dome.PotSystem.Growth;

public struct PotActionValidationContext
{
    public PotStateModel PotState;
    public Inventory PlayerInventory;
    public GameManager GameManager;
    public DayCycleSystem DayCycleSystem;
    public bool IsAutomationContext;
    public int ActionsCost;
    public int CryCost;
    public int MaxLightExposure;
    public Func<bool> IsPlayerInRange;
    public Func<bool> IsDead;
    public Func<bool> HasPlayerSeed;
}

public sealed class PotActionValidator
{
    private readonly PotActionValidationContext _context;

    public PotActionValidator(PotActionValidationContext context)
    {
        _context = context;
    }

    public bool CanPlant()
    {
        if (_context.PotState == null || _context.IsDead())
            return false;

        bool isEmpty = _context.PotState.IsEmpty;
        bool hasSeed = _context.IsAutomationContext || _context.HasPlayerSeed?.Invoke() == true;
        bool inRange = _context.IsAutomationContext || _context.IsPlayerInRange?.Invoke() == true;
        bool hasResources = _context.IsAutomationContext || CanConsumeResources();
        bool notWateredOnThisDay = _context.IsAutomationContext
            || (_context.DayCycleSystem != null && _context.PotState.LastWateredDay != _context.DayCycleSystem.CurrentDay);

        return isEmpty && hasSeed && inRange && hasResources && notWateredOnThisDay;
    }

    public bool CanWater()
    {
        if (_context.PotState == null || _context.IsDead())
            return false;

        bool hasPlant = _context.PotState.HasPlantGrowing;
        bool inRange = _context.IsPlayerInRange?.Invoke() == true;
        if (!hasPlant || !inRange)
            return false;

        if (_context.PotState.WateringSystemOn)
            return true;

        return CanConsumeResources();
    }

    public bool CanLight()
    {
        if (_context.PotState == null || _context.IsDead())
            return false;

        bool hasPlant = _context.PotState.HasPlantGrowing;
        bool inRange = _context.IsPlayerInRange?.Invoke() == true;
        bool hasResources = CanConsumeResources();
        return hasPlant && inRange && hasResources;
    }

    public bool CanApplyAdditive()
    {
        if (_context.PotState == null || _context.IsDead())
            return false;

        bool hasPlant = _context.PotState.HasPlantGrowing;
        bool inRange = _context.IsPlayerInRange?.Invoke() == true;
        bool hasResources = CanConsumeResources();
        return hasPlant && inRange && hasResources;
    }

    public bool CanHarvest()
    {
        if (_context.PotState == null || _context.IsDead())
            return false;

        bool isHarvestReady = _context.PotState.Stage == (int)PlantStage.HarvestReady;
        bool hasFruits = _context.PotState.AmountFruits > 0f;
        bool inRange = _context.IsAutomationContext || _context.IsPlayerInRange?.Invoke() == true;
        bool hasResources = _context.IsAutomationContext || CanConsumeResources();
        return isHarvestReady && hasFruits && inRange && hasResources;
    }

    public bool CanFertilize()
    {
        if (_context.PotState == null || _context.IsDead())
            return false;

        bool hasPlant = _context.PotState.HasPlantGrowing;
        bool inRange = _context.IsPlayerInRange?.Invoke() == true;
        bool hasResources = CanConsumeResources();
        return hasPlant && inRange && hasResources;
    }

    public bool CanPruning()
    {
        if (_context.PotState == null || _context.IsDead())
            return false;

        bool hasPlant = _context.PotState.HasPlantGrowing;
        bool inRange = _context.IsPlayerInRange?.Invoke() == true;
        bool hasResources = CanConsumeResources();
        return hasPlant && inRange && hasResources;
    }

    public string GetPlantFailureReason()
    {
        if (_context.PotState == null) return "Stato vaso non valido";
        if (!_context.PotState.IsEmpty) return "Vaso non vuoto";
        if (_context.HasPlayerSeed?.Invoke() != true) return "Nessun seme disponibile";
        if (_context.IsPlayerInRange?.Invoke() != true) return "Troppo lontano";
        if (!CanConsumeResources()) return "Azioni o CRY insufficienti";
        return "Azione non permessa";
    }

    public string GetWaterFailureReason()
    {
        if (_context.PotState == null) return "Stato vaso non valido";
        if (!_context.PotState.HasPlantGrowing) return "Vaso vuoto";
        if (_context.IsPlayerInRange?.Invoke() != true) return "Troppo lontano";
        if (!CanConsumeResources()) return "Azioni insufficienti";
        return "Azione non permessa";
    }

    public string GetLightFailureReason()
    {
        if (_context.PotState == null) return "Stato vaso non valido";
        if (!_context.PotState.HasPlantGrowing) return "Vaso vuoto";
        if (_context.PotState.IsLightExposureMax(_context.MaxLightExposure)) return "Luce al massimo";
        if (_context.IsPlayerInRange?.Invoke() != true) return "Troppo lontano";
        if (!CanConsumeResources()) return "Azioni o CRY insufficienti";
        return "Azione non permessa";
    }

    public string GetApplyAdditiveFailureReason(string additiveTypeId)
    {
        if (_context.PotState == null) return "Stato vaso non valido";
        if (!_context.PotState.HasPlantGrowing) return "Vaso vuoto";
        if (_context.IsPlayerInRange?.Invoke() != true) return "Troppo lontano";
        if (!CanConsumeResources()) return "Azioni o CRY insufficienti";

        if (_context.PlayerInventory != null && !string.IsNullOrEmpty(additiveTypeId))
        {
            if (additiveTypeId == Items.AdditiveBasic
                && !_context.PlayerInventory.Has(Items.AdditiveBasic, 1)
                && _context.PlayerInventory.Has(Items.SprayAntifungal, 1))
            {
                return "Azione non permessa";
            }

            if (!_context.PlayerInventory.Has(additiveTypeId, 1))
                return "Additivo non disponibile";
        }

        return "Azione non permessa";
    }

    public string GetHarvestFailureReason()
    {
        if (_context.PotState == null) return "Stato vaso non valido";
        if (_context.PotState.Stage != (int)PlantStage.HarvestReady) return "Pianta non in HarvestReady";
        if (_context.PotState.AmountFruits <= 0f) return "Nessun frutto disponibile";
        if (_context.IsPlayerInRange?.Invoke() != true) return "Troppo lontano";
        if (!CanConsumeResources()) return "Azioni o CRY insufficienti";
        return "Azione non permessa";
    }

    public string GetFertilizeFailureReason()
    {
        if (_context.PotState == null) return "Stato vaso non valido";
        if (!_context.PotState.HasPlantGrowing) return "Vaso vuoto";
        if (_context.IsPlayerInRange?.Invoke() != true) return "Troppo lontano";
        if (!CanConsumeResources()) return "Azioni insufficienti";
        return "Azione non permessa";
    }

    public string GetPruningFailureReason()
    {
        if (_context.PotState == null) return "Stato vaso non valido";
        if (!_context.PotState.HasPlantGrowing) return "Vaso vuoto";
        if (_context.IsPlayerInRange?.Invoke() != true) return "Troppo lontano";
        if (!CanConsumeResources()) return "Azioni o CRY insufficienti";
        return "Azione non permessa";
    }

    private bool CanConsumeResources()
    {
        if (_context.IsAutomationContext)
            return true;

        if (_context.GameManager == null)
            return false;

        return _context.GameManager.ActionsLeft >= _context.ActionsCost
            && _context.GameManager.CurrentCRY >= _context.CryCost;
    }
}
