namespace Sporae.Core
{
    /// <summary>Categorie entrate CRY per tooltip P/L (CompactBottomBar).</summary>
    public enum CryIncomeLedgerCategory
    {
        Other = 0,
        BlackMarketSell = 1,
        MissionReward = 2,
    }

    /// <summary>Categorie uscite CRY per tooltip P/L.</summary>
    public enum CrySpendLedgerCategory
    {
        Other = 0,
        /// <summary>Energia alba + costi ricorrenti Dome (seed storage, cucina).</summary>
        DomeUpkeep = 1,
        BlackMarketBuy = 2,
    }
}
