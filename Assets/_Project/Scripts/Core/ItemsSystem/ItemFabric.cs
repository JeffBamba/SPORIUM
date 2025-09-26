using UnityEngine;

namespace _Project.Sporae.Core
{
    public static class ItemFabric
    {
        private static int _uniqueId = 0;
        
        public static Item CreateItemByType(string typeId)
        {
            var config = Resources.Load<ItemData>("Items/" + typeId);
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