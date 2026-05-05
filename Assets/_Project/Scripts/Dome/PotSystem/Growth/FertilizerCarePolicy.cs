namespace Sporae.Dome.PotSystem.Growth
{
    /// <summary>
    /// Regole UX / avanzamento: quando il fertilizzante è richiesto e come valutare il minimo giornaliero.
    /// I valori numerici per stadio vivono in <see cref="StageRequirements"/> (PlantData).
    /// </summary>
    public static class FertilizerCarePolicy
    {
        /// <summary>
        /// HUD: nessuna banda attiva (es. HarvestReady con min/max a 0 nel dato).
        /// </summary>
        public static bool ShouldTreatFertilizerAsOptional(PlantStage stage, StageRequirements req)
        {
            if (stage == PlantStage.Empty)
                return true;
            if (req == null)
                return false;
            return req.fertilizerMin <= 0 && req.fertilizerMax <= 0;
        }

        /// <summary>
        /// Avanzamento stadio (DayCycle): sotto il minimo blocca; sopra il massimo è tollerato come nel legacy Growth+.
        /// </summary>
        public static bool IsFertilizerSufficientForStageAdvancement(int fertilizerLevel, StageRequirements req)
        {
            if (req == null)
                return true;
            if (req.fertilizerMin <= 0 && req.fertilizerMax <= 0)
                return true;
            return fertilizerLevel >= req.fertilizerMin;
        }
    }
}
