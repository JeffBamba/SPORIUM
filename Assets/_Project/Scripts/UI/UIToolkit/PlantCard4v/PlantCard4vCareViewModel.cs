using System.Globalization;
using _Project;
using _Project.Sporae.Core;
using Sporae.Dome.PotSystem.Condition;
using Sporae.Dome.PotSystem.Growth;
using UnityEngine;

namespace Sporae.UI.UIToolkit.PlantCard4v
{
    public enum PlantCard4vActionKind
    {
        None,
        Water,
        LightBlue,
        LightRed,
        LightOff,
        Additive,
        Prune,
        Fertilize,
        Observe,
        TerminalPlant,
        TerminalHarvest,
        TerminalUproot
    }

    public sealed class PlantCard4vCareViewModel
    {
        private static readonly CultureInfo ItCulture = CultureInfo.GetCultureInfo("it-IT");

        public string PotId { get; private set; }
        public string ShortPotId { get; private set; }
        public string PlantName { get; private set; }
        public string PlantSubtitle { get; private set; }
        public string SpeciesLine { get; private set; }
        public string LifeState { get; private set; }
        public string StageDetail { get; private set; }
        public string MainNeed { get; private set; }
        public string MainNeedSubtitle { get; private set; }
        public string MainRisk { get; private set; }
        public string RiskCause { get; private set; }
        public string RiskLevelText { get; private set; }
        public bool HasSecondaryRisk { get; private set; }
        public string SecondaryRiskTitle { get; private set; }
        public string SecondaryRiskCause { get; private set; }
        public string HydrationText { get; private set; }
        public int HydrationPercent { get; private set; }
        public int LightStressPercent { get; private set; }
        /// <summary>Drift giornaliero accodato della cupola (PhSystem).</summary>
        public string PhDomeDriftText { get; private set; }
        /// <summary>Banda pH ambiente (Acido/Neutro/Basico) da EvaluateState.</summary>
        public string PhDomeBandShort { get; private set; }
        /// <summary>Preferenza chimica della pianta (range ottimale) — Acido/Basico/Neutro.</summary>
        public string PlantPhPreferenceLabel { get; private set; }
        /// <summary>Stress luce cumulativo per la card "Condizione generale".</summary>
        public string LightStressPercentLine { get; private set; }
        /// <summary>Testo livello muffa: "Lvl 0" …</summary>
        public string MoldLevelLine { get; private set; }
        /// <summary>LED richiesto in questa fase, se noto.</summary>
        public string PreferredLightLine { get; private set; }
        /// <summary>True quando mostrare blocco rischi dettagliato (non stato "tutto stabile").</summary>
        public bool ShowRiskDetailPanel { get; private set; }
        public string FertilizerText { get; private set; }
        public string ConditionText { get; private set; }
        public string MoldText { get; private set; }
        public string VoHintLine { get; private set; }
        public string VoHintId { get; private set; }
        public string FooterStateLine { get; private set; }
        public PlantCard4vActionKind PrimaryAction { get; private set; }
        public PlantCard4vActionKind SecondaryAction { get; private set; }
        public int RiskSegments { get; private set; }
        public bool IsWateringActive { get; private set; }
        public LedSystemState LedState { get; private set; }
        public bool IsEmpty { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsHarvestReady { get; private set; }

        public static PlantCard4vCareViewModel Build(
            PotSlot pot,
            PotStateModel state,
            PlantData plantData,
            PotSystemConfig config,
            PhSystem phSystem)
        {
            var model = new PlantCard4vCareViewModel();
            model.BuildInternal(pot, state, plantData, config, phSystem);
            return model;
        }

        private void BuildInternal(PotSlot pot, PotStateModel state, PlantData plantData, PotSystemConfig config, PhSystem phSystem)
        {
            PotId = pot != null && !string.IsNullOrWhiteSpace(pot.PotId) ? pot.PotId : (state != null ? state.PotId : "POT-???");
            ShortPotId = BuildShortPotId(PotId);
            IsEmpty = state == null || state.IsEmpty || !state.HasPlant;
            IsDead = state != null && (PlantCondition)state.ConditionLabel == PlantCondition.Morta;
            IsWateringActive = state != null && state.WateringSystemOn;
            LedState = state != null ? state.LedSystemState : LedSystemState.Off;

            if (IsEmpty)
            {
                BuildEmpty(phSystem);
                return;
            }

            PlantName = ResolvePlantName(state, plantData);
            PlantSubtitle = ResolvePlantSubtitle(state, plantData);
            SpeciesLine = ResolveSpeciesLine(state, plantData);
            LifeState = FormatLifeState((PlantStage)state.Stage, IsDead);
            StageDetail = $"FASE {Mathf.Max(0, state.Stage)} - {FormatStageDetail((PlantStage)state.Stage)}";
            IsHarvestReady = state.Stage == (int)PlantStage.HarvestReady && state.AmountFruits > 0f;

            int maxHydration = config != null ? Mathf.Max(1, config.MaxHydration) : 10;
            HydrationPercent = Mathf.Clamp(Mathf.RoundToInt((float)state.Hydration / maxHydration * 100f), 0, 100);
            HydrationText = $"IDRATAZIONE {HydrationPercent}%";
            LightStressPercent = CalculateLightStressPercent(state, config);
            LightStressPercentLine = $"{LightStressPercent.ToString(ItCulture)}%";

            StageRequirements stageReq = plantData != null ? plantData.GetStageRequirements((PlantStage)state.Stage) : null;
            float currentPh = phSystem != null ? phSystem.CurrentPh : 0f;

            ResolveDomePhRow(phSystem);
            PlantPhPreferenceLabel = ResolvePlantPhPreferenceLabel(plantData);
            PreferredLightLine = ResolvePreferredLightLine(stageReq);
            MoldLevelLine = $"Lvl {Mathf.Clamp(state.MoldRiskLevel, 0, 99)}";
            FertilizerText = ResolveFertilizerText(state, stageReq);
            ConditionText = ResolveConditionText(state);
            MoldText = ResolveMoldText(state);

            ResolveNeedRiskAndActions(state, plantData, stageReq, config, phSystem, currentPh);
            FooterStateLine = $"{FormatLifeState((PlantStage)state.Stage, IsDead)} - {BuildFooterForStage((PlantStage)state.Stage, IsDead)}";
        }

        private void BuildEmpty(PhSystem phSystem)
        {
            PlantName = "VASO VUOTO";
            PlantSubtitle = "PROCEDURA PLANT ASSENTE";
            SpeciesLine = "Specie: nessuna";
            LifeState = "VUOTO";
            StageDetail = "NESSUNA ATTIVITA' RILEVATA";
            MainNeed = "Attende un seme";
            MainNeedSubtitle = "Serve una procedura d'impianto al Terminale POT per iniziare un ciclo vitale.";
            MainRisk = "Nessuna vita da proteggere";
            RiskCause = "Procedura PLANT richiesta dal Terminale POT";
            RiskLevelText = "RISCHIO N/D";
            HasSecondaryRisk = false;
            SecondaryRiskTitle = string.Empty;
            SecondaryRiskCause = string.Empty;
            HydrationText = "IDRATAZIONE 0%";
            HydrationPercent = 0;
            LightStressPercent = 0;
            LightStressPercentLine = "0%";
            ResolveDomePhRow(phSystem);
            PlantPhPreferenceLabel = "---";
            PreferredLightLine = "---";
            MoldLevelLine = "Lvl 0";
            FertilizerText = "---";
            ConditionText = "---";
            MoldText = "---";
            VoHintLine = "Solo polvere e promesse. Prima serve una procedura di impianto.";
            VoHintId = "empty";
            FooterStateLine = "VUOTO - Tracce di vita assenti.";
            PrimaryAction = PlantCard4vActionKind.TerminalPlant;
            SecondaryAction = PlantCard4vActionKind.None;
            RiskSegments = 0;
            ShowRiskDetailPanel = true;
        }

        private void ResolveDomePhRow(PhSystem phSystem)
        {
            if (phSystem == null)
            {
                PhDomeDriftText = "---";
                PhDomeBandShort = "---";
                return;
            }

            float drift = phSystem.GetTotalDailyDrift();
            PhDomeDriftText = drift.ToString("+0.0;-0.0;0.0", ItCulture);
            PhDomeBandShort = FormatPhBandShort(phSystem.EvaluateState());
        }

        private static string FormatPhBandShort(PhSystem.PhBand band)
        {
            return band switch
            {
                PhSystem.PhBand.UltraAcid => "Acido",
                PhSystem.PhBand.StableAcid => "Acido",
                PhSystem.PhBand.Neutral => "Neutro",
                PhSystem.PhBand.StableBasic => "Basico",
                PhSystem.PhBand.UltraBasic => "Basico",
                _ => "---"
            };
        }

        private static string ResolvePlantPhPreferenceLabel(PlantData plantData)
        {
            if (plantData == null)
                return "---";

            float mid = (plantData.OptimalPhMin + plantData.OptimalPhMax) * 0.5f;
            if (mid < -8f)
                return "Acido";
            if (mid > 8f)
                return "Basico";
            return "Neutro";
        }

        private static string ResolvePreferredLightLine(StageRequirements stageReq)
        {
            LedType? req = stageReq != null ? stageReq.GetRequiredLed() : null;
            if (!req.HasValue)
                return "NESSUNA";

            return req.Value == LedType.Blue ? "BLU" : "ROSSA";
        }

        private void ResolveNeedRiskAndActions(
            PotStateModel state,
            PlantData plantData,
            StageRequirements stageReq,
            PotSystemConfig config,
            PhSystem phSystem,
            float currentPh)
        {
            PrimaryAction = PlantCard4vActionKind.None;
            SecondaryAction = PlantCard4vActionKind.None;
            HasSecondaryRisk = false;
            SecondaryRiskTitle = string.Empty;
            SecondaryRiskCause = string.Empty;
            RiskSegments = 1;

            if (IsDead)
            {
                MainNeed = "Non risponde";
                MainNeedSubtitle = "La biomassa non e' piu' recuperabile: serve la procedura di estirpazione al Terminale POT.";
                MainRisk = "La vita non e' infinita";
                RiskCause = "Pianta morta - rimozione via Terminale POT";
                RiskLevelText = "STATO FINALE";
                VoHintLine = "Non risponde piu'. Anche il contenimento, a volte, arriva tardi.";
                VoHintId = "dead";
                PrimaryAction = PlantCard4vActionKind.TerminalUproot;
                SecondaryAction = PlantCard4vActionKind.None;
                RiskSegments = 8;
                ShowRiskDetailPanel = true;
                return;
            }

            if (state.IsInfested)
            {
                MainNeed = "Va ripulita";
                MainNeedSubtitle = "Muffa attiva nel substrato: servono potatura e stabilizzazione chimica.";
                MainRisk = "Infestazione attiva";
                RiskCause = "Muffa materializzata nel Pot";
                RiskLevelText = "RISCHIO CRITICO";
                VoHintLine = "La superficie e' viva nel modo sbagliato. Il contenimento sta cedendo.";
                VoHintId = "infested";
                PrimaryAction = PlantCard4vActionKind.Prune;
                SecondaryAction = PlantCard4vActionKind.Additive;
                RiskSegments = 8;
                ShowRiskDetailPanel = true;
                return;
            }

            if (state.MoldRiskLevel >= 3)
            {
                MainNeed = "Va stabilizzata";
                MainNeedSubtitle = "Umidita' cronicamente alta: il rischio muffa richiede intervento immediato.";
                MainRisk = "Muffa critica";
                RiskCause = "Overwatering prolungato";
                RiskLevelText = "RISCHIO ALTO";
                VoHintLine = "Troppa acqua. Il substrato sta diventando una seconda coltura.";
                VoHintId = "mold_critical";
                PrimaryAction = PlantCard4vActionKind.Prune;
                SecondaryAction = PlantCard4vActionKind.Additive;
                RiskSegments = 7;
                ShowRiskDetailPanel = true;
                return;
            }

            if (IsHarvestReady)
            {
                MainNeed = "Ha completato il ciclo";
                MainNeedSubtitle = "La coltura e' pronta per la raccolta meccanica autorizzata al Terminale POT.";
                MainRisk = "Frutto maturo in attesa";
                RiskCause = "Raccolta manuale non autorizzata";
                RiskLevelText = "PROCEDURA TERMINALE";
                VoHintLine = "Ha dato tutto. Ora serve la macchina, non la mano.";
                VoHintId = "harvest_ready";
                PrimaryAction = PlantCard4vActionKind.TerminalHarvest;
                SecondaryAction = PlantCard4vActionKind.None;
                RiskSegments = 3;
                ShowRiskDetailPanel = true;
                return;
            }

            if (stageReq != null && HydrationPercent < stageReq.hydrationMin)
            {
                if (state.WateringSystemOn)
                {
                    MainNeed = "Irrigazione in corso";
                    MainNeedSubtitle = "Il sistema a goccia e' attivo: attendi che l'idratazione raggiunga il minimo previsto per questa fase.";
                    MainRisk = "Assorbimento in attesa";
                    RiskCause = "Sistema a goccia attivo";
                    RiskLevelText = "PROCEDURA ATTIVA";
                    VoHintLine = "L'acqua e' in viaggio. Il resto dei parametri dira' se basta.";
                    VoHintId = "water_active";
                    PrimaryAction = ResolveNextCareAction(state, plantData, stageReq, phSystem, currentPh);
                    SecondaryAction = PlantCard4vActionKind.None;
                    RiskSegments = 2;
                    ShowRiskDetailPanel = true;
                    return;
                }

                MainNeed = "Sta cercando acqua";
                MainNeedSubtitle = $"Substrato sotto soglia ({stageReq.hydrationMin}%): priorizza reintegro idrico controllato.";
                MainRisk = "Disidratazione lenta";
                RiskCause = $"Idratazione sotto range ({stageReq.hydrationMin}%)";
                RiskLevelText = "STRESS MEDIO";
                VoHintLine = "Il substrato e' troppo secco. Non sta morendo, ma sta aspettando.";
                VoHintId = "water_low";
                PrimaryAction = PlantCard4vActionKind.Water;
                SecondaryAction = ResolveLightSecondary(state, plantData, stageReq);
                SetSecondaryLightRiskIfActive(state, plantData);
                RiskSegments = 4;
                ShowRiskDetailPanel = true;
                return;
            }

            if (stageReq != null && HydrationPercent > stageReq.hydrationMax)
            {
                MainNeed = "Ha troppa acqua";
                MainNeedSubtitle = $"Idratazione sopra il massimo ({stageReq.hydrationMax}%): rischio anaerobiosi e muffa in aumento.";
                MainRisk = "Muffa in preparazione";
                RiskCause = $"Idratazione sopra range ({stageReq.hydrationMax}%)";
                RiskLevelText = "STRESS MEDIO";
                VoHintLine = "Troppa acqua. La superficie comincia a diventare viva nel modo sbagliato.";
                VoHintId = "water_high";
                PrimaryAction = state.WateringSystemOn ? PlantCard4vActionKind.Water : PlantCard4vActionKind.Prune;
                SecondaryAction = state.WateringSystemOn ? PlantCard4vActionKind.Prune : PlantCard4vActionKind.Additive;
                RiskSegments = 4;
                ShowRiskDetailPanel = true;
                return;
            }

            var lightAction = ResolveLightSecondary(state, plantData, stageReq);
            if (lightAction != PlantCard4vActionKind.None)
            {
                MainNeed = "Sta cercando orientamento";
                if (TryBuildActiveLightRisk(state, plantData, out string lightRisk, out string lightCause, out int lightRiskSegments))
                {
                    MainRisk = lightRisk;
                    RiskCause = lightCause;
                    RiskLevelText = "RISCHIO LUCE";
                    RiskSegments = lightRiskSegments;
                    MainNeedSubtitle = lightCause;
                    VoHintLine = "La luce sta lasciando tracce. Il Pot non e' ancora in zona burn, ma ci si sta avvicinando.";
                }
                else
                {
                    MainRisk = "Nessuna anomalia critica";
                    RiskCause = $"Stress luce {LightStressPercent}%";
                    RiskLevelText = "NESSUN RISCHIO LUCE";
                    RiskSegments = 0;
                    MainNeedSubtitle = "Spettro o durata LED non allineati alla fase: regola l'illuminazione o spegni per ridurre stress.";
                    VoHintLine = "Non e' stress. Sta solo cercando orientamento.";
                }
                VoHintId = "light_need";
                PrimaryAction = lightAction;
                SecondaryAction = PlantCard4vActionKind.None;
                ShowRiskDetailPanel = true;
                return;
            }

            if (plantData != null && phSystem != null && !plantData.IsPhInOptimalRange(currentPh))
            {
                bool tooLow = currentPh < plantData.OptimalPhMin;
                MainNeed = "Vuole un ambiente diverso";
                MainNeedSubtitle = tooLow
                    ? $"pH cupola sotto l'ottimale della specie ({plantData.OptimalPhMin:0}-{plantData.OptimalPhMax:0}). Usa additivo per rialzo."
                    : $"pH cupola sopra l'ottimale della specie ({plantData.OptimalPhMin:0}-{plantData.OptimalPhMax:0}). Usa additivo per correzione.";
                MainRisk = tooLow ? "pH troppo acido" : "pH troppo basico";
                RiskCause = $"Affinita' {plantData.OptimalPhMin:0}-{plantData.OptimalPhMax:0}";
                RiskLevelText = "SQUILIBRIO pH";
                VoHintLine = tooLow
                    ? "L'ambiente la sta tirando verso il basso. Il pH e' fuori tono."
                    : "L'ambiente e' troppo duro. Il pH e' fuori tono.";
                VoHintId = tooLow ? "ph_low" : "ph_high";
                PrimaryAction = PlantCard4vActionKind.Additive;
                SecondaryAction = PlantCard4vActionKind.None;
                RiskSegments = 5;
                ShowRiskDetailPanel = true;
                return;
            }

            if (stageReq != null && state.Stage > (int)PlantStage.Sprout && state.FertilizerLevel < stageReq.fertilizerMin)
            {
                MainNeed = "Ha bisogno di nutrienti";
                MainNeedSubtitle = $"Fertilizzazione sotto il minimo ({stageReq.fertilizerMin}%): la fase corrente richiede piu' input nutritivo.";
                MainRisk = "Crescita rallentata";
                RiskCause = $"Fertilizzante sotto range ({stageReq.fertilizerMin}%)";
                RiskLevelText = "RISCHIO BASSO";
                VoHintLine = "Sta provando a costruire tessuto nuovo con troppo poco materiale.";
                VoHintId = "fert_low";
                PrimaryAction = PlantCard4vActionKind.Fertilize;
                SecondaryAction = PlantCard4vActionKind.None;
                RiskSegments = 3;
                ShowRiskDetailPanel = true;
                return;
            }

            MainNeed = "Parametri stabili";
            MainNeedSubtitle = "Idratazione, luce, pH e nutrimento risultano coerenti con i requisiti della fase: nessuna misura urgente.";
            MainRisk = "Nessuna anomalia critica";
            RiskCause = "Contenimento nei limiti";
            RiskLevelText = "RISCHIO BASSO";
            VoHintLine = "Per ora tiene. Non tutto cio' che vive chiede di essere toccato.";
            VoHintId = "stable";
            PrimaryAction = PlantCard4vActionKind.None;
            SecondaryAction = PlantCard4vActionKind.None;
            RiskSegments = 0;
            ShowRiskDetailPanel = false;
        }

        private PlantCard4vActionKind ResolveNextCareAction(
            PotStateModel state,
            PlantData plantData,
            StageRequirements stageReq,
            PhSystem phSystem,
            float currentPh)
        {
            var lightAction = ResolveLightSecondary(state, plantData, stageReq);
            if (lightAction != PlantCard4vActionKind.None)
                return lightAction;

            if (plantData != null && phSystem != null && !plantData.IsPhInOptimalRange(currentPh))
                return PlantCard4vActionKind.Additive;

            if (stageReq != null && state.Stage > (int)PlantStage.Sprout && state.FertilizerLevel < stageReq.fertilizerMin)
                return PlantCard4vActionKind.Fertilize;

            return PlantCard4vActionKind.None;
        }

        private void SetSecondaryLightRiskIfActive(PotStateModel state, PlantData plantData)
        {
            if (!TryBuildActiveLightRisk(state, plantData, out string title, out string cause, out _))
                return;

            HasSecondaryRisk = true;
            SecondaryRiskTitle = title;
            SecondaryRiskCause = cause;
        }

        private bool TryBuildActiveLightRisk(PotStateModel state, PlantData plantData, out string title, out string cause, out int segments)
        {
            title = string.Empty;
            cause = string.Empty;
            segments = 0;

            if (state == null)
                return false;

            if (plantData != null && state.LedSystemState != LedSystemState.Off)
            {
                var compat = LedCompatibilityHelper.GetCompatibleLedTypes(plantData.Family);
                if (!LedCompatibilityHelper.IsLedCompatible(state.LedSystemState, compat))
                {
                    title = "LED incompatibile";
                    cause = state.LedSystemState == LedSystemState.Red ? "Spettro rosso non adatto." : "Spettro blu non adatto.";
                    segments = Mathf.Clamp(Mathf.CeilToInt(LightStressPercent / 12.5f), 1, 8);
                    return true;
                }
            }

            if (LightStressPercent >= 80)
            {
                title = "Stress da luce";
                cause = $"Esposizione accumulata {LightStressPercent}%";
                segments = Mathf.Clamp(Mathf.CeilToInt(LightStressPercent / 12.5f), 6, 8);
                return true;
            }

            return false;
        }

        private static int CalculateLightStressPercent(PotStateModel state, PotSystemConfig config)
        {
            if (state == null)
                return 0;

            int maxDays = config != null ? config.MaxDaysForFullStress : 5;
            if (maxDays <= 0)
                maxDays = 5;

            return Mathf.Clamp(Mathf.RoundToInt((float)state.GetConsecutiveLedDays() / maxDays * 100f), 0, 100);
        }

        private PlantCard4vActionKind ResolveLightSecondary(PotStateModel state, PlantData plantData, StageRequirements stageReq)
        {
            if (state == null || stageReq == null)
                return PlantCard4vActionKind.None;

            LedType? requiredLed = stageReq.GetRequiredLed();
            if (requiredLed.HasValue)
            {
                LedSystemState required = requiredLed.Value == LedType.Blue ? LedSystemState.Blue : LedSystemState.Red;
                if (state.LedSystemState != required)
                    return required == LedSystemState.Blue ? PlantCard4vActionKind.LightBlue : PlantCard4vActionKind.LightRed;
            }

            if (plantData != null && state.LedSystemState != LedSystemState.Off)
            {
                var compat = LedCompatibilityHelper.GetCompatibleLedTypes(plantData.Family);
                if (!LedCompatibilityHelper.IsLedCompatible(state.LedSystemState, compat))
                    return PlantCard4vActionKind.LightOff;
            }

            int stress = state.GetConsecutiveLedDays();
            if (stress >= 4 && state.LedSystemState != LedSystemState.Off)
                return PlantCard4vActionKind.LightOff;

            return PlantCard4vActionKind.None;
        }

        private static string ResolvePlantName(PotStateModel state, PlantData plantData)
        {
            if (state == null)
                return "---";

            if (!string.IsNullOrWhiteSpace(state.CustomPlantName))
                return state.CustomPlantName.ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(state.SourcePlantDisplayName))
                return state.SourcePlantDisplayName.ToUpperInvariant();

            if (plantData != null && !string.IsNullOrWhiteSpace(plantData.name))
                return plantData.name.ToUpperInvariant();

            return string.IsNullOrWhiteSpace(state.PlantCode) ? "SPECIE NON IDENTIFICATA" : state.PlantCode;
        }

        private static string ResolvePlantSubtitle(PotStateModel state, PlantData plantData)
        {
            if (state == null)
                return "---";

            if (plantData != null && !string.IsNullOrWhiteSpace(plantData.Description))
                return plantData.Description;

            string family = plantData != null ? plantData.Family.ToString() : state.PlantFamilyMetadata;
            string code = !string.IsNullOrWhiteSpace(state.PlantCode) ? state.PlantCode : "CODICE NON IDENTIFICATO";
            return string.IsNullOrWhiteSpace(family) ? code : $"{family.ToUpperInvariant()} / {code}";
        }

        private static string ResolveSpeciesLine(PotStateModel state, PlantData plantData)
        {
            if (state == null)
                return "Specie: ---";

            string species = PlantSpeciesDisplayNames.FromPlantData(plantData);
            if (string.IsNullOrWhiteSpace(species) && !string.IsNullOrWhiteSpace(state.SourcePlantDisplayName))
                species = state.SourcePlantDisplayName;
            if (string.IsNullOrWhiteSpace(species))
                species = string.IsNullOrWhiteSpace(state.PlantCode) ? "non identificata" : state.PlantCode;

            return $"Specie: {species}";
        }

        private static string ResolveFertilizerText(PotStateModel state, StageRequirements stageReq)
        {
            if (state == null)
                return "---";
            if (stageReq == null)
                return $"{state.FertilizerLevel}%";
            if (state.FertilizerLevel < stageReq.fertilizerMin)
                return "BASSO";
            if (state.FertilizerLevel > stageReq.fertilizerMax)
                return "ALTO";
            return "ADEGUATO";
        }

        private static string ResolveConditionText(PotStateModel state)
        {
            if (state == null)
                return "---";

            PlantCondition condition = (PlantCondition)state.ConditionLabel;
            return condition switch
            {
                PlantCondition.Rigogliosa => "RIGOGLIOSA",
                PlantCondition.Sana => "STABILE",
                PlantCondition.Stressata => "STRESSATA",
                PlantCondition.Appassita => "DEBOLE",
                PlantCondition.Critica => "CRITICA",
                PlantCondition.Morta => "MORTA",
                _ => state.ConditionScore >= 70 ? "STABILE" : state.ConditionScore >= 40 ? "STRESSATA" : "CRITICA"
            };
        }

        private static string ResolveMoldText(PotStateModel state)
        {
            if (state == null)
                return "---";
            if (state.IsInfested)
                return "INFESTATA";
            return state.MoldRiskLevel switch
            {
                0 => "BASSO",
                1 => "BASSO",
                2 => "MEDIO",
                _ => "ALTO"
            };
        }

        private static string FormatLifeState(PlantStage stage, bool dead)
        {
            if (dead)
                return "MORTO";

            return stage switch
            {
                PlantStage.Empty => "VUOTO",
                PlantStage.Seed => "SEME",
                PlantStage.Sprout => "GERMOGLIO",
                PlantStage.Growth => "CRESCITA",
                PlantStage.Flowering => "FIORITURA",
                PlantStage.HarvestReady => "HARVEST READY",
                PlantStage.Resting => "RIPOSO",
                _ => stage.ToString().ToUpperInvariant()
            };
        }

        private static string FormatStageDetail(PlantStage stage)
        {
            return stage switch
            {
                PlantStage.Seed => "IMPIANTO",
                PlantStage.Sprout => "ORIENTAMENTO",
                PlantStage.Growth => "CRESCITA ATTIVA",
                PlantStage.Flowering => "RIPRODUZIONE",
                PlantStage.HarvestReady => "RACCOLTA TERMINALE",
                PlantStage.Resting => "METABOLISMO RIDOTTO",
                _ => "NESSUNA ATTIVITA'"
            };
        }

        private static string BuildFooterForStage(PlantStage stage, bool dead)
        {
            if (dead)
                return "Non risponde piu'.";

            return stage switch
            {
                PlantStage.Seed => "E' piccolo, ma non dorme.",
                PlantStage.Sprout => "Sta prendendo orientamento.",
                PlantStage.Growth => "Ogni giorno un po' piu' forte.",
                PlantStage.Flowering => "Un linguaggio che solo tu capisci.",
                PlantStage.HarvestReady => "Ha completato il suo ciclo.",
                PlantStage.Resting => "Non disturbarlo.",
                _ => "Tracce di vita assenti."
            };
        }

        private static string BuildShortPotId(string potId)
        {
            if (string.IsNullOrWhiteSpace(potId))
                return "--";

            string digits = potId.Replace("POT-", string.Empty);
            return digits.TrimStart('0') switch
            {
                "" => "00-A",
                "1" => "01-A",
                "2" => "02-B",
                "3" => "03-C",
                "4" => "04-D",
                _ => digits
            };
        }
    }
}
