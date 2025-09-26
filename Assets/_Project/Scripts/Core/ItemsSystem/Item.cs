using System;

namespace _Project.Sporae.Core
{
    [Serializable]
    public class Item
    {
        public ItemData ItemConfig { get; private set; }

        public Item(ItemData config, int itemId)
        {
            ItemConfig = config;
            
            ItemId = itemId;
            Quality = config.MaxQuality;
        }

        public string TypeId => ItemConfig.TypeId;
        public int ItemId { get; }

        public float Quality { set; get; }
    }
}