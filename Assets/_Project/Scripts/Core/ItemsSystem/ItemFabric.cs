using UnityEngine;
using Sporae.DevTools;

namespace _Project.Sporae.Core
{
    public static class ItemFabric
    {
        private static int _uniqueId = 0;
        
        /// <summary>
        /// Crea un Item dal typeId. Per SporeGeneric restituisce sempre una spora con status (Raw + Stabile):
        /// la spora senza status non esiste come item.
        /// </summary>
        /// <returns>Item creato, o null se il config non esiste (eccetto SporeGeneric che usa fallback).</returns>
        public static Item CreateItemByType(string typeId)
        {
            if (typeId == Items.SporeGeneric)
                return CreateSporeWithFallbackMetadata();

            var config = Resources.Load<ItemConfig>("Items/" + typeId);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config by id: {typeId}");
                return null;
            }

            var item = new Item(config, _uniqueId++);
            return item;
        }
        
        /// <summary>
        /// Crea un Item dal typeId specificato con qualità personalizzata.
        /// </summary>
        /// <param name="typeId">ID dell'item da creare</param>
        /// <param name="quality">Qualità da impostare (normalmente MaxQuality, ma può essere maggiore per bonus livello)</param>
        /// <returns>Item creato con qualità personalizzata, o null se il config non esiste</returns>
        public static Item CreateItemWithQuality(string typeId, float quality)
        {
            var config = Resources.Load<ItemConfig>("Items/" + typeId);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config by id: {typeId}");
                return null;
            }

            var item = new Item(config, _uniqueId++);
            item.Quality = quality;
            return item;
        }

        /// <summary>
        /// Crea un Item (es. frutto) con metadata da harvest (GDD 42 Fase 0).
        /// </summary>
        public static Item CreateItemWithMetadata(string typeId, float quality,
            GeneticType? geneticType, string family, string sourcePlantCode)
        {
            var config = Resources.Load<ItemConfig>("Items/" + typeId);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config by id: {typeId}");
                return null;
            }
            var item = new Item(config, _uniqueId++);
            item.Quality = quality;
            item.GeneticTypeValue = geneticType;
            item.FamilyMetadata = family;
            item.SourcePlantCodeMetadata = sourcePlantCode;
            return item;
        }

        /// <summary>
        /// Crea una spora con metadata fallback per save vecchi (Raw + STABLE).
        /// </summary>
        public static Item CreateSporeWithFallbackMetadata()
        {
            var config = Resources.Load<ItemConfig>("Items/" + Items.SporeGeneric);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config for {Items.SporeGeneric}");
                return null;
            }
            var item = new Item(config, _uniqueId++);
            item.GeneticTypeValue = GeneticType.Stable;
            item.SporeStageValue = SporeStage.Raw;
            return item;
        }

        /// <summary>
        /// Crea una spora maturata (output Catalizzatore). Metadata: Matured + Stable.
        /// </summary>
        public static Item CreateSporeMatured()
        {
            var config = Resources.Load<ItemConfig>("Items/" + Items.SporeGeneric);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config for {Items.SporeGeneric}");
                return null;
            }
            var item = new Item(config, _uniqueId++);
            item.GeneticTypeValue = GeneticType.Stable;
            item.SporeStageValue = SporeStage.Matured;
            return item;
        }
    }
}