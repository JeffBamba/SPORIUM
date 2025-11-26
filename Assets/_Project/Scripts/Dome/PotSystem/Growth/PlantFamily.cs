namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// Famiglia di appartenenza della pianta.
    /// Determina il drift pH giornaliero e le caratteristiche base.
    /// </summary>
    public enum PlantFamily
    {
        /// <summary>
        /// Piante standard neutre, drift pH minimo o nullo
        /// </summary>
        Standard = 0,
        
        /// <summary>
        /// Piante pure, drift pH positivo (+2/giorno, range +2 a +3)
        /// </summary>
        Pure = 1,
        
        /// <summary>
        /// Piante evil, drift pH negativo (-2/giorno, range -1 a -3)
        /// </summary>
        Evil = 2
    }
}

