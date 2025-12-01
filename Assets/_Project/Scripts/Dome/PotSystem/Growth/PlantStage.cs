namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// Stadi di crescita delle piante nel sistema Sporium
    /// </summary>
    public enum PlantStage 
    { 
        Empty = 0,        // Vaso vuoto
        Seed = 1,         // Seme piantato
        Sprout = 2,       // Germoglio
        Growth = 3,       // Accrescimento vegetativo (BLK-02.05)
        Flowering = 4,    // Fioritura attiva (BLK-02.05)
        HarvestReady = 5, // Finestra di raccolta multi-giorno (BLK-02.05)
        Resting = 6       // Riposo post-raccolta (BLK-02.05)
    }
}
