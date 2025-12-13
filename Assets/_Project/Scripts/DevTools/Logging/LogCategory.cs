namespace Sporae.DevTools
{
    /// <summary>
    /// Categorie di log per filtraggio e organizzazione
    /// </summary>
    public enum LogCategory
    {
        UI = 0,        // Interfaccia utente
        Core = 1,      // Sistemi core (GameManager, ServiceContainer, ecc.)
        Dome = 2,      // Sistema Dome (DayCycle, PotSystem, ecc.)
        Pot = 3,       // Sistema Pot (PotActions, PotState, ecc.)
        Ph = 4,        // Sistema pH
        Inventory = 5, // Sistema inventario
        Save = 6,      // Sistema salvataggio
        Audio = 7,     // Sistema audio
        All = 8        // Tutte le categorie
    }
}

