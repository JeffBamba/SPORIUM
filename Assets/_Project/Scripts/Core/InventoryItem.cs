using System;

namespace Sporae.Core
{
    [Serializable]
    public class InventoryItem
    {
        public string id;
        public int quantity;

        public InventoryItem(string id, int qty = 1)
        {
            this.id = id;
            this.quantity = qty;
        }
    }
}
