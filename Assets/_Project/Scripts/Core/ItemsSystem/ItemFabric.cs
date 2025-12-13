using UnityEngine;

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
                Debug.LogError($"Cannot find item config by id: {typeId}");
                return null;
            }

            var item = new Item(config, _uniqueId++);
            return item;
        }
    }
}