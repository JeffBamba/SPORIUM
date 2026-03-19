using System;
using System.Collections.Generic;
using System.Linq;
using Sporae.Dome.PotSystem.Growth;

namespace Sporae.Dome
{
    /// <summary>
    /// Registro runtime dei vasi e dei rispettivi controller di crescita.
    /// Evita scan completi della scena per recuperare collezioni e lookup per PotId.
    /// </summary>
    public class DomePotRegistry
    {
        private readonly HashSet<PotSlot> _pots = new();
        private readonly HashSet<PotGrowthController> _growthControllers = new();

        public void RegisterPot(PotSlot pot)
        {
            if (pot != null)
                _pots.Add(pot);
        }

        public void UnregisterPot(PotSlot pot)
        {
            if (pot != null)
                _pots.Remove(pot);
        }

        public void RegisterGrowthController(PotGrowthController controller)
        {
            if (controller != null)
                _growthControllers.Add(controller);
        }

        public void UnregisterGrowthController(PotGrowthController controller)
        {
            if (controller != null)
                _growthControllers.Remove(controller);
        }

        public List<PotSlot> GetPotsSnapshot()
        {
            return _pots
                .Where(pot => pot != null)
                .OrderBy(pot => pot.PotId, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Alias esplicito di GetPotsSnapshot() per enfatizzare che questo registro
        /// contiene solo i 4 slot attivi della Dome (mai i CryoSlot passivi).
        /// </summary>
        public List<PotSlot> GetActivePotsSnapshot() => GetPotsSnapshot();

        public PotSlot FindPotById(string potId)
        {
            if (string.IsNullOrEmpty(potId))
                return null;

            foreach (var pot in _pots)
            {
                if (pot == null)
                    continue;

                if (string.Equals(pot.PotId, potId, StringComparison.OrdinalIgnoreCase))
                    return pot;

                var statePotId = pot.PotActions != null && pot.PotActions.PotState != null ? pot.PotActions.PotState.PotId : null;
                if (!string.IsNullOrEmpty(statePotId) && string.Equals(statePotId, potId, StringComparison.OrdinalIgnoreCase))
                    return pot;
            }

            return null;
        }

        public PotGrowthController FindGrowthController(string potId)
        {
            if (string.IsNullOrEmpty(potId))
                return null;

            foreach (var controller in _growthControllers)
            {
                if (controller == null)
                    continue;

                var potState = controller.GetPotState();
                if (potState != null && string.Equals(potState.PotId, potId, StringComparison.OrdinalIgnoreCase))
                    return controller;
            }

            return null;
        }
    }
}
