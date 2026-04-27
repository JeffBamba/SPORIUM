namespace _Project.Sporae.Core
{
    public static class Items
    {
        public const string Fruits = "fruits-001";
        public const string FruitsKnown = "fruits-known-001";
        public const string FruitFerricPod = "fruit-ferric-pod";
        public const string FruitArcticPod = "fruit-arctic-pod";
        public const string FruitGlassPod = "fruit-glass-pod";
        public const string Water = "WAT-RAW";
        public const string WholePlant = "whole-plant";
        public const string SporeGeneric = "spore-generic";
        /// <summary>Output della Pipette (Fusione); usato come input nell'Incubatore.</summary>
        public const string PreSeed = "pre-seed";
        /// <summary>TypeId seme Legacy Wave 1 (allineati a PLT-STD/PURE/EVIL-001). Nuove specie usano altri seed-* da PlantData.</summary>
        public const string Seed001 = "seed-001";
        public const string Seed002 = "seed-002";
        public const string Seed003 = "seed-003";
        
        // BLK-03.01-T1: Fertilizzanti
        public const string FertilizerStandard = "fertilizer-standard";    // 25 CRY, +25%
        public const string FertilizerPure = "fertilizer-pure";            // 75 CRY, +40%
        public const string FertilizerProhibited = "fertilizer-prohibited"; // 75 CRY, +40%
        
        // BLK-??: Additivi pH (consumabili selezionabili; ex STR-004 legacy rimosso)
        public const string AdditiveBasic = "STR-004-Basic";               // Additivo Basico (pH +5, riduce muffe)
        public const string AdditiveAcid = "STR-004-Acid";                 // Additivo Acido (pH -5, aumenta muffe)

        // Lab GDD42 / Dimenticanze: Cellule staminali, residui proteici, reagenti
        public const string StemCellVegetable = "CELL-001";   // Cellula staminale vegetale
        public const string StemCellFungus = "CELL-002";      // Cellula staminale fungina
        public const string StemCellAnimal = "CELL-003";      // Cellula staminale animale
        public const string ProteinResidue = "RES-PROT-001";   // Residui proteici
        public const string ReagentX = "REAG-X";               // Reagente X (Incubatore)
        public const string ReagentY = "REAG-Y";              // Reagente Y (Incubatore)

        // BLK-04.01: Food Room Items
        public const string FoodVegetable = "FOOD-101";      // Vegetali sintetici (+1 Azione)
        public const string FoodFungus = "FOOD-201";         // Funghi sintetici (+2 Azioni)
        public const string FoodMeat = "FOOD-301";           // Carne sintetica (+3 Azioni)
        public const string WaterPotable = "WAT-POT";        // Acqua Potabile
        public const string OrganicResidue = "ORG-RES-001";  // Residui organici (deperimento / cucina / Lab)

        /// <summary> Tutti i typeId degli item esistenti in game (per inventario iniziale / debug ). </summary>
        public static readonly string[] AllTypeIds =
        {
            FruitFerricPod, FruitArcticPod, FruitGlassPod,
            Water, WholePlant, SporeGeneric, PreSeed,
            Seed001, Seed002, Seed003,
            FertilizerStandard, FertilizerPure, FertilizerProhibited,
            AdditiveBasic, AdditiveAcid,
            StemCellVegetable, StemCellFungus, StemCellAnimal, ProteinResidue,
            ReagentX, ReagentY,
            FoodVegetable, FoodFungus, FoodMeat, WaterPotable, OrganicResidue
        };

        public static readonly string[] SpecificFruitTypeIds =
        {
            FruitFerricPod, FruitArcticPod, FruitGlassPod
        };

        public static readonly string[] LegacyFruitTypeIds =
        {
            Fruits, FruitsKnown
        };

        public static readonly string[] AllFruitTypeIds =
        {
            FruitFerricPod, FruitArcticPod, FruitGlassPod, Fruits, FruitsKnown
        };

        public static readonly string[] StarterInventoryTypeIds =
        {
            FruitFerricPod, FruitArcticPod, FruitGlassPod,
            Water, WholePlant,
            FertilizerStandard, FertilizerPure, FertilizerProhibited,
            AdditiveBasic, AdditiveAcid,
            StemCellVegetable, StemCellFungus, StemCellAnimal, ProteinResidue,
            ReagentX, ReagentY,
            FoodVegetable, FoodFungus, FoodMeat, WaterPotable, OrganicResidue
        };

        public static bool IsSpecificFruitType(string typeId)
        {
            return typeId == FruitFerricPod || typeId == FruitArcticPod || typeId == FruitGlassPod;
        }

        public static bool IsLegacyFruitType(string typeId)
        {
            return typeId == Fruits || typeId == FruitsKnown;
        }

        public static bool IsFruitType(string typeId, bool includeLegacy = true)
        {
            return IsSpecificFruitType(typeId) || (includeLegacy && IsLegacyFruitType(typeId));
        }

        public static bool IsStarterInventoryType(string typeId)
        {
            if (string.IsNullOrWhiteSpace(typeId))
                return false;

            for (int i = 0; i < StarterInventoryTypeIds.Length; i++)
            {
                if (StarterInventoryTypeIds[i] == typeId)
                    return true;
            }

            return false;
        }
    }
}