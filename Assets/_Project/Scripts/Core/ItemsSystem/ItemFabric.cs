using UnityEngine;
using Sporae.DevTools;
using Sporae.Core.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using Sporae.Dome.PotSystem.Growth;

namespace _Project.Sporae.Core
{
    public static class ItemFabric
    {
        private static int _uniqueId = 0;
        private static readonly HashSet<string> _loggedMissingTypeIds = new HashSet<string>();
        private sealed class FruitDefinition
        {
            public string TypeId;
            public string PlantCode;
            public string DisplayName;
            public string PassivePowerLabel;
        }

        private static readonly Dictionary<string, FruitDefinition> _fruitDefinitionsByTypeId = new Dictionary<string, FruitDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [Items.FruitFerricPod] = new FruitDefinition
            {
                TypeId = Items.FruitFerricPod,
                PlantCode = "PLT-STD-001",
                DisplayName = "Ferric Pod",
                PassivePowerLabel = "Filtro Ferrico: stabilizza il bioma e attenua il rischio muffe residuo."
            },
            [Items.FruitArcticPod] = new FruitDefinition
            {
                TypeId = Items.FruitArcticPod,
                PlantCode = "PLT-PURE-001",
                DisplayName = "Arctic Pod",
                PassivePowerLabel = "Permafrost Core: sostiene il recupero del pH e la purezza ambientale."
            },
            [Items.FruitGlassPod] = new FruitDefinition
            {
                TypeId = Items.FruitGlassPod,
                PlantCode = "PLT-EVIL-001",
                DisplayName = "Glass Pod",
                PassivePowerLabel = "Nebbia Sporale: alza la pressione mutagena e la volatilita biologica."
            }
        };

        /// <summary>
        /// Crea un Item dal typeId. Per SporeGeneric restituisce sempre una spora con status (Raw + Stabile):
        /// la spora senza status non esiste come item.
        /// </summary>
        /// <returns>Item creato, o null se il config non esiste (eccetto SporeGeneric che usa fallback).</returns>
        public static Item CreateItemByType(string typeId)
        {
            if (typeId == Items.SporeGeneric)
                return CreateSporeWithFallbackMetadata();

            var config = Resources.Load<ItemConfig>("Items/" + typeId);
            if (!config)
            {
                if (_loggedMissingTypeIds.Add(typeId))
                    SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config by id: {typeId}");
                return null;
            }

            var item = new Item(config, _uniqueId++);
            ApplyBaseFruitMetadata(item, typeId);
            return item;
        }
        
        /// <summary>
        /// Crea un Item dal typeId specificato con qualità personalizzata.
        /// </summary>
        /// <param name="typeId">ID dell'item da creare</param>
        /// <param name="quality">Qualità da impostare (normalmente MaxQuality, ma può essere maggiore per bonus livello)</param>
        /// <returns>Item creato con qualità personalizzata, o null se il config non esiste</returns>
        public static Item CreateItemWithQuality(string typeId, float quality)
        {
            var config = Resources.Load<ItemConfig>("Items/" + typeId);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config by id: {typeId}");
                return null;
            }

            var item = new Item(config, _uniqueId++);
            ApplyBaseFruitMetadata(item, typeId);
            item.Quality = quality;
            return item;
        }

        /// <summary>
        /// Crea un Item (es. frutto) con metadata da harvest (GDD 42 Fase 0).
        /// </summary>
        public static Item CreateItemWithMetadata(string typeId, float quality,
            GeneticType? geneticType, string family, string sourcePlantCode, int plantLevel = 0,
            string sourcePlantDisplayName = null, string activePowerLabel = null, string passivePowerLabel = null)
        {
            var config = Resources.Load<ItemConfig>("Items/" + typeId);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config by id: {typeId}");
                return null;
            }
            var item = new Item(config, _uniqueId++);
            ApplyBaseFruitMetadata(item, typeId);
            item.Quality = quality;
            item.GeneticTypeValue = geneticType;
            item.FamilyMetadata = !string.IsNullOrWhiteSpace(family) ? NormalizeFamily(family) : item.FamilyMetadata;
            item.SourcePlantCodeMetadata = !string.IsNullOrWhiteSpace(sourcePlantCode) ? sourcePlantCode : item.SourcePlantCodeMetadata;
            item.SourcePlantDisplayName = !string.IsNullOrWhiteSpace(sourcePlantDisplayName) ? sourcePlantDisplayName : item.SourcePlantDisplayName;
            item.PlantLevelMetadata = Mathf.Max(0, plantLevel);
            item.ActivePowerLabel = !string.IsNullOrWhiteSpace(activePowerLabel) ? activePowerLabel : item.ActivePowerLabel;
            item.PassivePowerLabel = !string.IsNullOrWhiteSpace(passivePowerLabel) ? passivePowerLabel : item.PassivePowerLabel;
            ApplyPlantMetadataFromCode(item, item.SourcePlantCodeMetadata, onlyIfEmpty: true);
            return item;
        }

        /// <summary>
        /// Crea una spora con metadata fallback per save vecchi (Raw + STABLE).
        /// </summary>
        public static Item CreateSporeWithFallbackMetadata()
        {
            var config = Resources.Load<ItemConfig>("Items/" + Items.SporeGeneric);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config for {Items.SporeGeneric}");
                return null;
            }
            var item = new Item(config, _uniqueId++);
            item.GeneticTypeValue = GeneticType.Stable;
            item.SporeStageValue = SporeStage.Raw;
            return item;
        }

        /// <summary>
        /// Crea una spora maturata (output Catalizzatore). Metadata: Matured + Stable.
        /// </summary>
        public static Item CreateSporeMatured()
        {
            var config = Resources.Load<ItemConfig>("Items/" + Items.SporeGeneric);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config for {Items.SporeGeneric}");
                return null;
            }
            var item = new Item(config, _uniqueId++);
            item.GeneticTypeValue = GeneticType.Stable;
            item.SporeStageValue = SporeStage.Matured;
            return item;
        }

        /// <param name="geneticOverride">Se valorizzato, sostituisce la genetica ereditata dal frutto (seconda spora Task 7).</param>
        public static Item CreateSporeRawFromFruit(Item fruit, GeneticType? geneticOverride = null)
        {
            var config = Resources.Load<ItemConfig>("Items/" + Items.SporeGeneric);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config for {Items.SporeGeneric}");
                return null;
            }

            var item = new Item(config, _uniqueId++);
            item.SporeStageValue = SporeStage.Raw;
            item.GeneticTypeValue = geneticOverride ?? fruit?.GeneticTypeValue ?? GeneticType.Stable;
            item.FamilyMetadata = !string.IsNullOrWhiteSpace(fruit?.FamilyMetadata)
                ? fruit.FamilyMetadata
                : (fruit?.TypeId == Items.FruitsKnown ? "STANDARD" : null);
            item.SourcePlantCodeMetadata = !string.IsNullOrWhiteSpace(fruit?.SourcePlantCodeMetadata)
                ? fruit.SourcePlantCodeMetadata
                : (fruit?.TypeId == Items.FruitsKnown ? "PLT-PURE-001" : null);
            item.PlantLevelMetadata = fruit != null ? Mathf.Max(0, fruit.PlantLevelMetadata) : (fruit?.TypeId == Items.FruitsKnown ? 4 : 0);
            CopyPlantPowerMetadata(fruit, item);
            ApplyBaseFruitMetadata(item, fruit?.TypeId);
            ApplyPlantMetadataFromCode(item, item.SourcePlantCodeMetadata, onlyIfEmpty: true);
            EnsureSourcePlantDisplayIsHumanReadable(item);
            return item;
        }

        public static Item CreateSporeMaturedFromRaw(Item rawSpore)
        {
            var config = Resources.Load<ItemConfig>("Items/" + Items.SporeGeneric);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config for {Items.SporeGeneric}");
                return null;
            }

            var item = new Item(config, _uniqueId++);
            item.SporeStageValue = SporeStage.Matured;
            item.GeneticTypeValue = rawSpore?.GeneticTypeValue ?? GeneticType.Stable;
            item.FamilyMetadata = rawSpore?.FamilyMetadata;
            item.SourcePlantCodeMetadata = rawSpore?.SourcePlantCodeMetadata;
            item.PlantLevelMetadata = rawSpore != null ? Mathf.Max(0, rawSpore.PlantLevelMetadata) : 0;
            CopyPlantPowerMetadata(rawSpore, item);
            ApplyPlantMetadataFromCode(item, item.SourcePlantCodeMetadata, onlyIfEmpty: true);
            EnsureSourcePlantDisplayIsHumanReadable(item);
            return item;
        }

        public static Item CloneSpore(Item sourceSpore)
        {
            if (sourceSpore == null)
                return null;

            var config = Resources.Load<ItemConfig>("Items/" + Items.SporeGeneric);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config for {Items.SporeGeneric}");
                return null;
            }

            var item = new Item(config, _uniqueId++);
            item.SporeStageValue = sourceSpore.SporeStageValue ?? SporeStage.Raw;
            item.GeneticTypeValue = sourceSpore.GeneticTypeValue;
            item.FamilyMetadata = sourceSpore.FamilyMetadata;
            item.SourcePlantCodeMetadata = sourceSpore.SourcePlantCodeMetadata;
            item.SourcePlantDisplayName = sourceSpore.SourcePlantDisplayName;
            item.PlantLevelMetadata = sourceSpore.PlantLevelMetadata;
            item.ActivePowerLabel = sourceSpore.ActivePowerLabel;
            item.PassivePowerLabel = sourceSpore.PassivePowerLabel;
            item.ParentFamilyA = sourceSpore.ParentFamilyA;
            item.ParentFamilyB = sourceSpore.ParentFamilyB;
            item.CandidateTraitsCsv = sourceSpore.CandidateTraitsCsv;
            item.SelectedTraitsCsv = sourceSpore.SelectedTraitsCsv;
            item.TraitPowerPercent = sourceSpore.TraitPowerPercent;
            item.ReagentUsedMetadata = sourceSpore.ReagentUsedMetadata;
            item.CustomPlantName = sourceSpore.CustomPlantName;
            item.ResolvedPlantCodeMetadata = sourceSpore.ResolvedPlantCodeMetadata;
            EnsureSourcePlantDisplayIsHumanReadable(item);
            return item;
        }

        public static GeneticType CombineGeneticTypeForPreSeed(GeneticType left, GeneticType right)
        {
            if (left == GeneticType.Fixed && right == GeneticType.Fixed)
                return GeneticType.Fixed;
            if ((left == GeneticType.Fixed && right == GeneticType.Unstable) ||
                (left == GeneticType.Unstable && right == GeneticType.Fixed))
                return GeneticType.Stable;
            if ((left == GeneticType.Fixed && right == GeneticType.Stable) ||
                (left == GeneticType.Stable && right == GeneticType.Fixed))
                return GeneticType.Stable;
            if (left == GeneticType.Stable && right == GeneticType.Stable)
                return GeneticType.Stable;
            if ((left == GeneticType.Stable && right == GeneticType.Unstable) ||
                (left == GeneticType.Unstable && right == GeneticType.Stable))
                return GeneticType.Unstable;
            return GeneticType.Unstable;
        }

        public static Item CreatePreSeedFromSpores(Item sporeA, Item sporeB)
        {
            var config = Resources.Load<ItemConfig>("Items/" + Items.PreSeed);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config for {Items.PreSeed}");
                return null;
            }

            var item = new Item(config, _uniqueId++);
            var gA = sporeA?.GeneticTypeValue ?? GeneticType.Stable;
            var gB = sporeB?.GeneticTypeValue ?? GeneticType.Stable;
            item.GeneticTypeValue = CombineGeneticTypeForPreSeed(gA, gB);
            item.ParentFamilyA = NormalizeFamily(sporeA?.FamilyMetadata);
            item.ParentFamilyB = NormalizeFamily(sporeB?.FamilyMetadata);
            item.CandidateTraitsCsv = BuildCandidateTraitsCsv(item.ParentFamilyA, item.ParentFamilyB);
            item.SourcePlantCodeMetadata = $"{sporeA?.SourcePlantCodeMetadata}|{sporeB?.SourcePlantCodeMetadata}";
            item.SourcePlantDisplayName = CombineDistinctValues(
                sporeA?.SourcePlantDisplayName,
                sporeB?.SourcePlantDisplayName);
            item.ActivePowerLabel = CombineDistinctValues(
                sporeA?.ActivePowerLabel,
                sporeB?.ActivePowerLabel);
            item.PassivePowerLabel = CombineDistinctValues(
                sporeA?.PassivePowerLabel,
                sporeB?.PassivePowerLabel);
            return item;
        }

        public static string ResolveFamilyWithReagentY(string familyA, string familyB)
        {
            string a = NormalizeFamily(familyA);
            string b = NormalizeFamily(familyB);
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
                return a;

            // Symmetric matrix (deterministic)
            if (PairIs(a, b, "PURE", "STANDARD")) return "PURE";
            if (PairIs(a, b, "PURE", "EVIL")) return "STANDARD";
            if (PairIs(a, b, "PURE", "IPNOTICHE")) return "PURE";
            if (PairIs(a, b, "STANDARD", "EVIL")) return "EVIL";
            if (PairIs(a, b, "STANDARD", "IPNOTICHE")) return "IPNOTICHE";
            if (PairIs(a, b, "EVIL", "IPNOTICHE")) return "EVIL";
            return a.CompareTo(b) <= 0 ? a : b;
        }

        /// <summary>
        /// Incubatore: crea un Item seme il cui TypeId è quello della specie di riferimento (<see cref="PlantData.SeedItemConfig"/>),
        /// non più un unico seme per famiglia (Task 6).
        /// </summary>
        /// <param name="referencePlantCodeOverride">Se valorizzato (es. PLT-STD-001), forza la specie del seme.</param>
        /// <param name="activePowerOverride">Incubatore Reagente X: sovrascrive <see cref="Item.ActivePowerLabel"/>.</param>
        /// <param name="passivePowerOverride">Incubatore Reagente X: sovrascrive <see cref="Item.PassivePowerLabel"/>.</param>
        /// <param name="labCareProfileMetadata">BLEND | PARENT_A | PARENT_B per range cure in Dome (Task 6).</param>
        public static Item CreateSeedFromPreSeed(
            Item preSeed,
            string resolvedFamily,
            string selectedTraitsCsv,
            int traitPowerPercent,
            string reagentTypeId,
            string chosenPlantName = null,
            string referencePlantCodeOverride = null,
            string activePowerOverride = null,
            string passivePowerOverride = null,
            string labCareProfileMetadata = null)
        {
            string plantCode = referencePlantCodeOverride;
            if (string.IsNullOrWhiteSpace(plantCode))
                plantCode = ResolveReferencePlantCodeForLabSeed(preSeed, resolvedFamily);
            if (string.IsNullOrWhiteSpace(plantCode))
                plantCode = MapFamilyToWave1PlantCode(NormalizeFamily(resolvedFamily));

            var plantData = ResolvePlantDataByCode(plantCode);
            string seedTypeId = plantData?.SeedItemConfig?.TypeId;
            if (string.IsNullOrEmpty(seedTypeId))
            {
                SporiumLogger.LogError(LogCategory.Inventory,
                    $"CreateSeedFromPreSeed: SeedItemConfig/TypeId mancante per plantCode={plantCode}");
                return null;
            }

            var config = Resources.Load<ItemConfig>("Items/" + seedTypeId);
            if (!config)
            {
                SporiumLogger.LogError(LogCategory.Inventory, $"Cannot find item config for {seedTypeId}");
                return null;
            }

            var item = new Item(config, _uniqueId++);
            item.GeneticTypeValue = preSeed?.GeneticTypeValue ?? GeneticType.Stable;
            item.FamilyMetadata = string.IsNullOrWhiteSpace(resolvedFamily)
                ? NormalizeFamily(plantData.Family.ToString())
                : resolvedFamily.Trim();
            item.ParentFamilyA = preSeed?.ParentFamilyA;
            item.ParentFamilyB = preSeed?.ParentFamilyB;
            item.CandidateTraitsCsv = preSeed?.CandidateTraitsCsv;
            item.SelectedTraitsCsv = selectedTraitsCsv;
            item.TraitPowerPercent = Mathf.Clamp(traitPowerPercent, 1, 100);
            item.ReagentUsedMetadata = reagentTypeId;
            item.SourcePlantCodeMetadata = preSeed?.SourcePlantCodeMetadata;
            item.CustomPlantName = chosenPlantName;
            item.ResolvedPlantCodeMetadata = plantCode;
            item.SourcePlantDisplayName = preSeed?.SourcePlantDisplayName;
            item.ActivePowerLabel = preSeed?.ActivePowerLabel;
            item.PassivePowerLabel = preSeed?.PassivePowerLabel;
            if (!string.IsNullOrWhiteSpace(activePowerOverride))
                item.ActivePowerLabel = activePowerOverride.Trim();
            if (!string.IsNullOrWhiteSpace(passivePowerOverride))
                item.PassivePowerLabel = passivePowerOverride.Trim();
            item.LabCareProfileMetadata = string.IsNullOrWhiteSpace(labCareProfileMetadata)
                ? null
                : labCareProfileMetadata.Trim();
            ApplyPlantMetadataFromCode(item, plantCode, onlyIfEmpty: true);
            return item;
        }

        /// <summary>
        /// Specie di riferimento per il seme in output Lab: genitore la cui famiglia coincide con <paramref name="resolvedFamilyRaw"/>,
        /// altrimenti primo genitore valido; HYBRID-WEAK → primo codice da metadata; fallback Wave 1 per famiglia.
        /// </summary>
        public static string ResolveReferencePlantCodeForLabSeed(Item preSeed, string resolvedFamilyRaw)
        {
            if (preSeed == null)
                return null;

            if (!string.IsNullOrWhiteSpace(resolvedFamilyRaw) &&
                resolvedFamilyRaw.TrimStart().StartsWith("HYBRID-WEAK", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var code in ParseParentPlantCodes(preSeed.SourcePlantCodeMetadata))
                {
                    if (ResolvePlantDataByCode(code) != null)
                        return code;
                }
                return MapFamilyToWave1PlantCode("STANDARD");
            }

            string targetFam = NormalizeFamily(resolvedFamilyRaw);
            foreach (var code in ParseParentPlantCodes(preSeed.SourcePlantCodeMetadata))
            {
                var pd = ResolvePlantDataByCode(code);
                if (pd == null) continue;
                if (NormalizeFamily(pd.Family.ToString()) == targetFam)
                    return pd.PlantCode;
            }

            foreach (var code in ParseParentPlantCodes(preSeed.SourcePlantCodeMetadata))
            {
                if (ResolvePlantDataByCode(code) != null)
                    return code.Trim();
            }

            return MapFamilyToWave1PlantCode(targetFam);
        }

        /// <summary>
        /// Incubatore Reagente X, nome libero e dominante AUTO: stima il <c>PlantCode</c> confrontando
        /// i testi potere scelti con il primo rigo attivo/passivo di ogni genitore nel pre-seed (due linee).
        /// Restituisce null se pareggio, nessun match o meno di due genitori.
        /// </summary>
        public static string TryResolveReferencePlantCodeFromPowerChoices(Item preSeed, string activePowerLine, string passivePowerLine)
        {
            if (preSeed == null) return null;
            var codes = ParseParentPlantCodes(preSeed.SourcePlantCodeMetadata);
            if (codes.Count < 2) return null;
            var scores = new int[codes.Count];
            void ScoreOne(string line)
            {
                if (string.IsNullOrWhiteSpace(line)) return;
                string ln = line.Trim();
                for (int i = 0; i < codes.Count; i++)
                {
                    var pd = ResolvePlantDataByCode(codes[i]);
                    if (pd == null) continue;
                    string fa = FirstDescriptorLineOfPower(pd.ActivePower);
                    string fp = FirstDescriptorLineOfPower(pd.PassivePower);
                    if (!string.IsNullOrEmpty(fa) && string.Equals(fa, ln, StringComparison.OrdinalIgnoreCase)) scores[i]++;
                    if (!string.IsNullOrEmpty(fp) && string.Equals(fp, ln, StringComparison.OrdinalIgnoreCase)) scores[i]++;
                }
            }
            ScoreOne(activePowerLine);
            ScoreOne(passivePowerLine);
            int best = -1;
            var winners = new List<int>();
            for (int i = 0; i < codes.Count; i++)
            {
                if (scores[i] <= 0) continue;
                if (scores[i] > best) { best = scores[i]; winners.Clear(); winners.Add(i); }
                else if (scores[i] == best) winners.Add(i);
            }
            if (best < 0 || winners.Count != 1) return null;
            return codes[winners[0]];
        }

        private static string FirstDescriptorLineOfPower(string multilineOrSingle)
        {
            if (string.IsNullOrWhiteSpace(multilineOrSingle)) return null;
            int cut = multilineOrSingle.IndexOfAny(new[] { '\r', '\n' });
            string s = cut < 0 ? multilineOrSingle.Trim() : multilineOrSingle.Substring(0, cut).Trim();
            return string.IsNullOrEmpty(s) ? null : s;
        }

        public static List<string> ParseParentPlantCodes(string meta)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(meta)) return list;
            var parts = meta.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var t = p.Trim();
                if (!string.IsNullOrEmpty(t) && !list.Exists(x => x.Equals(t, StringComparison.OrdinalIgnoreCase)))
                    list.Add(t);
            }
            return list;
        }

        private static string MapFamilyToWave1PlantCode(string normalizedFamily)
        {
            switch (NormalizeFamily(normalizedFamily))
            {
                case "PURE":
                    return "PLT-PURE-001";
                case "EVIL":
                    return "PLT-EVIL-001";
                default:
                    return "PLT-STD-001";
            }
        }

        public static string GetFruitDisplayNameByTypeId(string typeId)
        {
            if (string.IsNullOrWhiteSpace(typeId))
                return typeId;

            if (ItemDisplayNameLocalization.TryGetByTypeId(typeId, out var localized))
                return localized;

            if (_fruitDefinitionsByTypeId.TryGetValue(typeId, out var definition))
                return definition.DisplayName;

            return typeId;
        }

        public static string ResolveFruitTypeIdForPlant(string plantCode, string family = null)
        {
            if (!string.IsNullOrWhiteSpace(plantCode))
            {
                foreach (var definition in _fruitDefinitionsByTypeId.Values)
                {
                    if (string.Equals(definition.PlantCode, plantCode, StringComparison.OrdinalIgnoreCase))
                        return definition.TypeId;
                }
            }

            switch (NormalizeFamily(family))
            {
                case "PURE":
                    return Items.FruitArcticPod;
                case "EVIL":
                    return Items.FruitGlassPod;
                default:
                    return Items.FruitFerricPod;
            }
        }

        private static void CopyPlantPowerMetadata(Item source, Item target)
        {
            if (source == null || target == null)
                return;

            target.SourcePlantDisplayName = source.SourcePlantDisplayName;
            target.ActivePowerLabel = source.ActivePowerLabel;
            target.PassivePowerLabel = source.PassivePowerLabel;
        }

        private static bool ApplyBaseFruitMetadata(Item item, string typeId)
        {
            if (item == null || string.IsNullOrWhiteSpace(typeId))
                return false;

            if (!_fruitDefinitionsByTypeId.TryGetValue(typeId, out var definition))
                return false;

            if (string.IsNullOrWhiteSpace(item.SourcePlantCodeMetadata))
                item.SourcePlantCodeMetadata = definition.PlantCode;
            if (item.PlantLevelMetadata <= 0)
                item.PlantLevelMetadata = 1;

            ApplyPlantMetadataFromCode(item, definition.PlantCode, onlyIfEmpty: true);
            if (string.IsNullOrWhiteSpace(item.SourcePlantDisplayName))
            {
                var pdFallback = ResolvePlantDataByCode(definition.PlantCode);
                item.SourcePlantDisplayName = pdFallback != null
                    ? PlantSpeciesDisplayNames.FromPlantData(pdFallback)
                    : (PlantSpeciesDisplayNames.FromPlantCode(definition.PlantCode) ?? definition.PlantCode);
            }
            if (string.IsNullOrWhiteSpace(item.PassivePowerLabel))
                item.PassivePowerLabel = definition.PassivePowerLabel;
            if (!item.GeneticTypeValue.HasValue)
                item.GeneticTypeValue = GeneticType.Stable;
            return true;
        }

        private static void ApplyPlantMetadataFromCode(Item item, string plantCode, bool onlyIfEmpty)
        {
            if (item == null || string.IsNullOrWhiteSpace(plantCode))
                return;

            var plantData = ResolvePlantDataByCode(plantCode);
            if (plantData == null)
                return;

            if (!onlyIfEmpty || string.IsNullOrWhiteSpace(item.FamilyMetadata))
                item.FamilyMetadata = NormalizeFamily(plantData.Family.ToString());
            if (!onlyIfEmpty || string.IsNullOrWhiteSpace(item.SourcePlantCodeMetadata))
                item.SourcePlantCodeMetadata = plantData.PlantCode;
            if (!onlyIfEmpty || string.IsNullOrWhiteSpace(item.SourcePlantDisplayName))
                item.SourcePlantDisplayName = PlantSpeciesDisplayNames.FromPlantData(plantData);
            if (!onlyIfEmpty || string.IsNullOrWhiteSpace(item.ActivePowerLabel))
                item.ActivePowerLabel = plantData.ActivePower;
            if (!item.GeneticTypeValue.HasValue)
                item.GeneticTypeValue = plantData.DefaultGeneticType;

            if (!onlyIfEmpty || string.IsNullOrWhiteSpace(item.PassivePowerLabel))
                item.PassivePowerLabel = ResolvePassivePowerLabel(plantData.PlantCode);
        }

        private static PlantData ResolvePlantDataByCode(string plantCode)
        {
            if (string.IsNullOrWhiteSpace(plantCode))
                return null;

            var trimmed = plantCode.Trim();
            if (PlantDatabase.Instance != null)
            {
                var fromDb = PlantDatabase.Instance.GetPlantDataByCode(trimmed);
                if (fromDb != null)
                    return fromDb;
            }

            return Resources.Load<PlantData>("Plants/" + trimmed);
        }

        /// <summary>
        /// Sostituisce <see cref="Item.SourcePlantDisplayName"/> quando è vuoto o coincide col codice pianta.
        /// Non usa <see cref="PlantData.name"/> (spesso uguale al codice asset).
        /// </summary>
        private static void EnsureSourcePlantDisplayIsHumanReadable(Item item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.SourcePlantCodeMetadata))
                return;

            var code = item.SourcePlantCodeMetadata.Trim();
            var disp = item.SourcePlantDisplayName?.Trim();
            if (!string.IsNullOrWhiteSpace(disp) &&
                !string.Equals(disp, code, StringComparison.OrdinalIgnoreCase))
                return;

            var label = ResolveSpeciesUiNameFromPlantCode(code);
            if (!string.IsNullOrWhiteSpace(label))
                item.SourcePlantDisplayName = label;
        }

        /// <summary>
        /// Nome comune specie da codice pianta (mappa + <see cref="PlantSpeciesDisplayNames.FromPlantData"/>).
        /// </summary>
        private static string ResolveSpeciesUiNameFromPlantCode(string plantCode)
        {
            if (string.IsNullOrWhiteSpace(plantCode))
                return null;
            var trimmed = plantCode.Trim();
            var fromMap = PlantSpeciesDisplayNames.FromPlantCode(trimmed);
            if (!string.IsNullOrWhiteSpace(fromMap))
                return fromMap;
            var pd = ResolvePlantDataByCode(trimmed);
            return pd != null ? PlantSpeciesDisplayNames.FromPlantData(pd) : trimmed;
        }

        /// <summary>
        /// Nome pianta sorgente per UI (toast COLLECTION, meta righe).
        /// </summary>
        public static string ResolveSourcePlantDisplayNameForUi(Item item)
        {
            if (item == null)
                return null;

            var code = item.SourcePlantCodeMetadata?.Trim();
            var disp = item.SourcePlantDisplayName?.Trim();

            if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(disp) &&
                !string.Equals(disp, code, StringComparison.OrdinalIgnoreCase))
                return disp;

            if (!string.IsNullOrEmpty(code))
                return ResolveSpeciesUiNameFromPlantCode(code) ?? code;

            return string.IsNullOrEmpty(disp) ? null : disp;
        }

        private static string ResolvePassivePowerLabel(string plantCode)
        {
            if (string.IsNullOrWhiteSpace(plantCode))
                return null;

            switch (plantCode.Trim().ToUpperInvariant())
            {
                case "PLT-PURE-001":
                    return "Permafrost Core: sostiene il recupero del pH e la purezza ambientale.";
                case "PLT-EVIL-001":
                    return "Nebbia Sporale: alza la pressione mutagena e la volatilita biologica.";
                case "PLT-STD-001":
                    return "Filtro Ferrico: stabilizza il bioma e attenua il rischio muffe residuo.";
                default:
                    return null;
            }
        }

        private static string CombineDistinctValues(params string[] values)
        {
            var distinct = values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return distinct.Count == 0 ? null : string.Join(" | ", distinct);
        }

        public static string NormalizeFamily(string family)
        {
            if (string.IsNullOrWhiteSpace(family)) return "STANDARD";
            string norm = family.Trim().ToUpperInvariant();
            if (norm.Contains("PURE")) return "PURE";
            if (norm.Contains("EVIL")) return "EVIL";
            if (norm.Contains("IPNO")) return "IPNOTICHE";
            return "STANDARD";
        }

        public static string BuildCandidateTraitsCsv(string familyA, string familyB)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var trait in GetDefaultTraitsByFamily(familyA))
                set.Add(trait);
            foreach (var trait in GetDefaultTraitsByFamily(familyB))
                set.Add(trait);
            return string.Join(",", set);
        }

        public static List<string> ParseTraits(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return new List<string>();
            return csv
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IEnumerable<string> GetDefaultTraitsByFamily(string family)
        {
            switch (NormalizeFamily(family))
            {
                case "PURE":
                    return new[] { "GrowthSpeed", "PurityAura", "YieldBoost" };
                case "EVIL":
                    return new[] { "ToxinAffinity", "AggressiveSpread", "DarkResilience" };
                case "IPNOTICHE":
                    return new[] { "MindBloom", "PheromonePulse", "NeuralEcho" };
                default:
                    return new[] { "BalancedGrowth", "NeutralYield", "Resilience" };
            }
        }

        private static bool PairIs(string a, string b, string x, string y)
        {
            return (string.Equals(a, x, StringComparison.OrdinalIgnoreCase) && string.Equals(b, y, StringComparison.OrdinalIgnoreCase))
                || (string.Equals(a, y, StringComparison.OrdinalIgnoreCase) && string.Equals(b, x, StringComparison.OrdinalIgnoreCase));
        }

        private static readonly HashSet<string> GameplayTraitTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "GROWTH", "YIELD", "RESILIENCE", "LED_ADAPT", "PH_STABILITY", "VERSATILE"
        };

        /// <summary>
        /// Incubatore Reagente X: deriva tag gameplay (Task 6) da testi potere attivo/passivo scelti dal giocatore.
        /// </summary>
        public static string BuildSelectedTraitsCsvFromPowerChoices(string activeLabel, string passiveLabel)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in MapPowerDescriptionToGameplayTags(activeLabel))
                set.Add(g);
            foreach (var g in MapPowerDescriptionToGameplayTags(passiveLabel))
                set.Add(g);
            if (set.Count == 0)
                set.Add("VERSATILE");
            return string.Join(",", set.OrderBy(s => s, StringComparer.Ordinal));
        }

        /// <summary>
        /// Normalizza una riga tratti (legacy Lab o già tag) al CSV di tag gameplay usato da <see cref="Sporae.Dome.PotSystem.Growth.LabHybridGameplayModifiers"/>.
        /// </summary>
        public static string NormalizeTraitsRowToGameplayTagCsv(string csvOrSingle)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in ParseTraits(csvOrSingle ?? string.Empty))
            {
                string t = raw.Trim();
                if (string.IsNullOrEmpty(t)) continue;
                if (GameplayTraitTags.Contains(t))
                    set.Add(t.ToUpperInvariant());
                else
                    set.Add(MapLegacyTraitTokenToGameplayTag(t));
            }
            if (set.Count == 0)
                set.Add("VERSATILE");
            return string.Join(",", set.OrderBy(s => s, StringComparer.Ordinal));
        }

        static string MapLegacyTraitTokenToGameplayTag(string legacy)
        {
            switch ((legacy ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "YIELDBOOST":
                case "NEUTRALYIELD":
                    return "YIELD";
                case "GROWTHSPEED":
                case "BALANCEDGROWTH":
                    return "GROWTH";
                case "DARKRESILIENCE":
                case "RESILIENCE":
                case "TOXINAFFINITY":
                case "AGGRESSIVESPREAD":
                    return "RESILIENCE";
                case "PURITYAURA":
                case "MINDBLOOM":
                    return "PH_STABILITY";
                case "PHEROMONEPULSE":
                case "NEURALECHO":
                    return "LED_ADAPT";
                default:
                    return "VERSATILE";
            }
        }

        static IEnumerable<string> MapPowerDescriptionToGameplayTags(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) yield break;
            string s = label.Trim();
            if (s.StartsWith("—", StringComparison.Ordinal)) yield break;
            string lower = s.ToLowerInvariant();
            bool any = false;
            if (lower.Contains("resa") || lower.Contains("yield") || lower.Contains("raccolto") || lower.Contains("frutt"))
            {
                any = true;
                yield return "YIELD";
            }
            if (lower.Contains("cresci") || lower.Contains("growth") || lower.Contains("veloce"))
            {
                any = true;
                yield return "GROWTH";
            }
            if (lower.Contains("resilien") || lower.Contains("tossin") || lower.Contains("spread") || lower.Contains("nebbia spor"))
            {
                any = true;
                yield return "RESILIENCE";
            }
            if (lower.Contains("led") || lower.Contains("luce") || lower.Contains("foto") || lower.Contains("ipnot") || lower.Contains("pulse"))
            {
                any = true;
                yield return "LED_ADAPT";
            }
            if (lower.Contains("ph") || lower.Contains("filtro") || lower.Contains("stabil") || lower.Contains("purity") || lower.Contains("equilibr"))
            {
                any = true;
                yield return "PH_STABILITY";
            }
            if (!any)
                yield return "VERSATILE";
        }

        /// <summary>
        /// Seme con metadata come da Lab/Incubatore (traits CSV, genetica, livello sul seme). QA Task 4 senza flusso lab.
        /// </summary>
        /// <param name="geneticTypeOverride">Se valorizzato, sostituisce <see cref="PlantData.DefaultGeneticType"/> sul seme (debug / Pot console).</param>
        public static Item CreateDebugSeedWithLabLikeMetadata(string plantCode, int seedPlantLevelMetadata = 3, int traitPowerPercent = 100, GeneticType? geneticTypeOverride = null)
        {
            if (string.IsNullOrWhiteSpace(plantCode))
                return null;
            plantCode = plantCode.Trim();
            var plantData = ResolvePlantDataByCode(plantCode);
            if (plantData?.SeedItemConfig == null)
            {
                SporiumLogger.LogError(LogCategory.Inventory,
                    $"CreateDebugSeedWithLabLikeMetadata: PlantData o SeedItemConfig mancante per '{plantCode}'");
                return null;
            }

            string typeId = plantData.SeedItemConfig.TypeId;
            var config = Resources.Load<ItemConfig>("Items/" + typeId);
            if (config == null)
            {
                SporiumLogger.LogError(LogCategory.Inventory,
                    $"CreateDebugSeedWithLabLikeMetadata: ItemConfig mancante per '{typeId}'");
                return null;
            }

            var item = new Item(config, _uniqueId++);
            item.SourcePlantCodeMetadata = plantCode;
            item.ResolvedPlantCodeMetadata = plantCode;
            item.PlantLevelMetadata = Mathf.Max(1, seedPlantLevelMetadata);
            item.GeneticTypeValue = geneticTypeOverride ?? plantData.DefaultGeneticType;
            item.FamilyMetadata = NormalizeFamily(plantData.Family.ToString());
            item.TraitPowerPercent = Mathf.Clamp(traitPowerPercent, 1, 999);
            string famNorm = item.FamilyMetadata;
            string traitCsv = BuildCandidateTraitsCsv(famNorm, famNorm);
            item.CandidateTraitsCsv = traitCsv;
            item.SelectedTraitsCsv = NormalizeTraitsRowToGameplayTagCsv(traitCsv);
            item.ReagentUsedMetadata = "DEBUG-LAB-SKIP";
            ApplyPlantMetadataFromCode(item, plantCode, onlyIfEmpty: true);
            return item;
        }
    }
}