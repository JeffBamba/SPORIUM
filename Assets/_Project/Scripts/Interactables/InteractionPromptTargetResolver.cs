using Sporae.UI.UIToolkit.BedroomPc;
using Sporae.UI.UIToolkit.PlantCardV3;
using UnityEngine;

namespace _Project
{
    /// <summary>
    /// Mappa componenti Interactable → chiavi localizzazione per nomi player-friendly nel prompt [E].
    /// </summary>
    internal static class InteractionPromptTargetResolver
    {
        public static string ResolveLocKey(MonoBehaviour host, string overrideKey)
        {
            if (!string.IsNullOrWhiteSpace(overrideKey))
                return overrideKey.Trim();

            if (host == null)
                return null;

            if (host.GetComponent<ElevatorFloorDisplay>() != null)
                return "gameplay.interact.target.elevator";
            if (host.GetComponent<WardrobeStation>() != null)
                return "gameplay.interact.target.wardrobe";
            if (host.GetComponent<PlantCardV3TerminalOpener>() != null)
                return "gameplay.interact.target.pot_terminal";
            if (host.GetComponent<BedroomPcTerminal>() != null)
                return "gameplay.interact.target.bedroom_pc";
            if (host.GetComponent<Bed>() != null)
                return "gameplay.interact.target.bed";
            if (host.GetComponent<CryoMachineOpener>() != null)
                return "gameplay.interact.target.cryo_machine";
            if (host.GetComponent<LabTerminalOpener>() != null)
                return "gameplay.interact.target.lab_terminal";
            if (host.GetComponent<DispensaRefrigerataOpener>() != null)
                return "gameplay.interact.target.pantry";
            if (host.GetComponent<FoodSynthMachine>() != null)
                return "gameplay.interact.target.food_synth";
            if (host.GetComponent<Incubator>() != null)
                return "gameplay.interact.target.incubator";
            if (host.GetComponent<CondenseTankMachine>() != null)
                return "gameplay.interact.target.condensation_tank";
            if (host.GetComponent<KitchenCondensationCollectPoint>() != null)
                return "gameplay.interact.target.condensation_collect";
            if (host.GetComponent<Extractor>() != null)
                return "gameplay.interact.target.extractor";
            if (host.GetComponent<Microscope>() != null)
                return "gameplay.interact.target.microscope";
            if (host.GetComponent<Pipette>() != null)
                return "gameplay.interact.target.pipette";
            if (host.GetComponent<Catalizzatore>() != null)
                return "gameplay.interact.target.catalyst";
            if (host.GetComponent<BlackMarketTerminal>() != null)
                return "gameplay.interact.target.black_market";
            if (host.GetComponent<SeedStorage>() != null)
                return "gameplay.interact.target.seed_storage";
            if (host.GetComponent<PotSlot>() != null)
                return "gameplay.interact.target.pot";

            return null;
        }
    }
}
