using UnityEngine;
using Sporae.DevTools;

namespace _Project.Sporae.Core
{
    public static class ItemFabric
    {
        private static int _uniqueId = 0;
        
        /// <summary>
        /// Crea un Item dal typeId specificato.
        /// </summary>
        /// <param name="typeId">ID dell'item da creare</param>
        /// <returns>Item creato, o null se il config non esiste</returns>
        /// <remarks>
        /// BUG FIX: Documentato che può restituire null. I chiamanti devono controllare null.
        /// </remarks>
        public static Item CreateItemByType(string typeId)
        {
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
    }
}