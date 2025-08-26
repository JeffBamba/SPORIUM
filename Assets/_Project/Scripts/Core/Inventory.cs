using System.Collections.Generic;

namespace Sporae.Core
{
    public class Inventory
    {
        private readonly List<InventoryItem> items = new List<InventoryItem>();
        public IReadOnlyList<InventoryItem> Items => items;

        public void Add(string id, int qty = 1)
        {
            var it = Find(id);
            if (it == null) items.Add(new InventoryItem(id, qty));
            else it.quantity += qty;
        }

        public bool Has(string id, int qty = 1)
        {
            var it = Find(id);
            return it != null && it.quantity >= qty;
        }

        public bool Consume(string id, int qty = 1)
        {
            var it = Find(id);
            if (it == null || it.quantity < qty) return false;
            it.quantity -= qty;
            if (it.quantity <= 0) items.Remove(it);
            return true;
        }

        private InventoryItem Find(string id)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].id == id) return items[i];
            }
            return null;
        }
    }
}
