namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// Nome comune della specie per UI (toast, collection, inventario).
    /// <see cref="PlantData"/> usa spesso come asset name lo stesso valore di <see cref="PlantData.PlantCode"/>,
    /// quindi non va usato <c>plantData.name</c> come etichetta giocatore.
    /// </summary>
    public static class PlantSpeciesDisplayNames
    {
        public static string FromPlantCode(string plantCode)
        {
            if (string.IsNullOrWhiteSpace(plantCode))
                return null;
            var c = plantCode.Trim();
            return c switch
            {
                "PLT-STD-001" => "Ferric Fern",
                "PLT-PURE-001" => "Arctic Hask",
                "PLT-EVIL-001" => "Glasscap Fungus",
                _ => null
            };
        }

        /// <summary>
        /// Nome specie da <see cref="PlantData"/>; allineato a DomeStatusHUD / PlantCardFormatters.
        /// </summary>
        public static string FromPlantData(PlantData plantData)
        {
            if (plantData == null)
                return null;

            var mapped = FromPlantCode(plantData.PlantCode);
            if (!string.IsNullOrWhiteSpace(mapped))
                return mapped;

            if (string.IsNullOrWhiteSpace(plantData.PlantCode))
                return plantData.name;

            return plantData.name.Replace("PLT-", "").Replace("-", " ");
        }
    }
}
