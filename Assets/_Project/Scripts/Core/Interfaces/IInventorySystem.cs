using System.Collections.Generic;

namespace Sporae.Core.Interfaces
{
    /// <summary>
    /// Interfaccia per il sistema inventario
    /// </summary>
    public interface IInventorySystem
    {
        /// <summary>
        /// Aggiunge un item all'inventario
        /// </summary>
        /// <param name="id">ID dell'item</param>
        /// <param name="quantity">Quantità da aggiungere</param>
        /// <returns>True se aggiunto con successo</returns>
        bool AddItem(string id, int quantity = 1);
        
        /// <summary>
        /// Rimuove un item dall'inventario
        /// </summary>
        /// <param name="id">ID dell'item</param>
        /// <param name="quantity">Quantità da rimuovere</param>
        /// <returns>True se rimosso con successo</returns>
        bool RemoveItem(string id, int quantity = 1);
        
        /// <summary>
        /// Verifica se l'inventario contiene un item
        /// </summary>
        /// <param name="id">ID dell'item</param>
        /// <param name="quantity">Quantità richiesta</param>
        /// <returns>True se l'item è presente in quantità sufficiente</returns>
        bool HasItem(string id, int quantity = 1);
        
        /// <summary>
        /// Ottiene la quantità di un item
        /// </summary>
        /// <param name="id">ID dell'item</param>
        /// <returns>Quantità dell'item</returns>
        int GetItemQuantity(string id);
        
        /// <summary>
        /// Ottiene tutti gli item nell'inventario
        /// </summary>
        /// <returns>Lista di tutti gli item</returns>
        IReadOnlyCollection<InventoryItem> GetAllItems();
        
        /// <summary>
        /// Ottiene tutti gli ID degli item
        /// </summary>
        /// <returns>Lista di tutti gli ID</returns>
        List<string> GetAllItemIds();
        
        /// <summary>
        /// Svuota l'inventario
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Verifica se l'inventario è vuoto
        /// </summary>
        bool IsEmpty { get; }
        
        /// <summary>
        /// Numero totale di item (somma delle quantità)
        /// </summary>
        int TotalItems { get; }
        
        /// <summary>
        /// Numero di tipi di item diversi
        /// </summary>
        int UniqueItems { get; }
    }
}
