namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// Tipo di LED utilizzato per l'illuminazione delle piante
    /// </summary>
    public enum LedType
    {
        Blue = 0,   // LED Blu: accelera Growth → Flowering, pH +5
        Red = 1    // LED Rosso: accelera Flowering → HarvestReady, pH -5
    }
    
    /// <summary>
    /// BLK-02.07: Stato sistema LED persistente (toggle Off/Blue/Red)
    /// </summary>
    public enum LedSystemState
    {
        Off = 0,   // Sistema LED spento
        Blue = 1,  // LED Blu attivo (Growth/stabilità)
        Red = 2    // LED Rosso attivo (Flowering/produzione)
    }
}

