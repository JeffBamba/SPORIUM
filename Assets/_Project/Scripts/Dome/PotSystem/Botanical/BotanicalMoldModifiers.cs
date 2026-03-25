namespace Sporae.Dome.PotSystem.Botanical
{
    /// <summary>
    /// Modifica i giorni di eccesso oltre soglia overwatering prima del mapping a livello muffa 0–3.
    /// Ferric Fern (attivo): floor(excess * 0.9). Glasscap (passivo cryo): moltiplicatore 1.15 per slot.
    /// </summary>
    public static class BotanicalMoldModifiers
    {
        public static int ApplyToRawExcess(int rawExcessDays, in BotanicalRosterSnapshot roster)
        {
            if (rawExcessDays <= 0)
                return 0;

            double v = rawExcessDays;
            if (roster.AnyFerricFernActive)
                v = System.Math.Floor(v * 0.9);

            for (int i = 0; i < roster.GlasscapPassiveSlotCount; i++)
                v = System.Math.Ceiling(v * 1.15);

            if (v < 0) v = 0;
            if (v > 3) v = 3;
            return (int)v;
        }
    }
}
