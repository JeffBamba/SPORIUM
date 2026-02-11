namespace _Project.Sporae.Core
{
    public static class Items
    {
        public const string Fruits = "fruits-001";
        public const string Water = "wat-raw";
        public const string WholePlant = "whole-plant";
        public const string OrganicScrap001 = "org-scr-001";
        public const string SporeGeneric = "spore-generic";
        /// <summary>Output della Pipette (Fusione); usato come input nell'Incubatore.</summary>
        public const string PreSeed = "pre-seed";
        public const string Seed001 = "seed-001";  // Standard
        public const string Seed002 = "seed-002";  // Pure
        public const string Seed003 = "seed-003";  // Evil
        
        // BLK-03.01-T1: Fertilizzanti
        public const string FertilizerStandard = "fertilizer-standard";    // 25 CRY, +25%
        public const string FertilizerPure = "fertilizer-pure";            // 75 CRY, +40%
        public const string FertilizerProhibited = "fertilizer-prohibited"; // 75 CRY, +40%
        
        // AZ-13/AZ-14: Spray Antifungino
        public const string SprayAntifungal = "STR-004";                   // Spray Antifungino (rimuove muffe, pH +5)

        // BLK-??: Additivi pH (sostituiscono Spray Antifungino come consumabile selezionabile)
        public const string AdditiveBasic = "STR-004-Basic";               // Additivo Basico (pH +5, riduce muffe)
        public const string AdditiveAcid = "STR-004-Acid";                 // Additivo Acido (pH -5, aumenta muffe)

        // Lab GDD42 / Dimenticanze: Cellule staminali, residui proteici, reagenti
        public const string StemCellVegetable = "CELL-001";   // Cellula staminale vegetale
        public const string StemCellFungus = "CELL-002";      // Cellula staminale fungina
        public const string StemCellAnimal = "CELL-003";      // Cellula staminale animale
        public const string ProteinResidue = "RES-PROT-001";   // Residui proteici
        public const string ReagentX = "REAG-X";               // Reagente X (Incubatore)
        public const string ReagentY = "REAG-Y";              // Reagente Y (Incubatore)

        /// <summary> Tutti i typeId degli item esistenti in game (per inventario iniziale / debug ). </summary>
        public static readonly string[] AllTypeIds =
        {
            Fruits, Water, WholePlant, OrganicScrap001, SporeGeneric,
            Seed001, Seed002, Seed003,
            FertilizerStandard, FertilizerPure, FertilizerProhibited,
            SprayAntifungal, AdditiveBasic, AdditiveAcid,
            StemCellVegetable, StemCellFungus, StemCellAnimal, ProteinResidue,
            ReagentX, ReagentY
        };
    }
}