using System.Collections.Generic;
using System.Globalization;
using _Project;
using _Project.Sporae.Core;
using UnityEngine;
using Sporae.Dome.PotSystem.Condition;
using Sporae.Dome.PotSystem.Growth;

namespace Sporae.UI.UIToolkit.PlantCard4v
{
    /// <summary>Colore semantico titolo riga Bisogni principali (verde / giallo / rosso).</summary>
    public enum PlantCard4vNeedSignal
    {
        Ok,
        Attention,
        Warning
    }

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

        /// <summary>Macro-area usata per evitare due frasi VO sullo stesso parametro.</summary>
        private enum VoParamTopic
        {
            None,
            Water,
            Light,
            Ph,
            Fertilizer,
            Condition,
        }

        public string PotId { get; private set; }
        public string ShortPotId { get; private set; }
        public string PlantName { get; private set; }
        public string PlantSubtitle { get; private set; }
        public string SpeciesLine { get; private set; }
        public string LifeState { get; private set; }
        public string StageDetail { get; private set; }
        public string ConditionLine { get; private set; }
        public PlantCard4vNeedSignal ConditionStatusSignal { get; private set; }
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
        /// <summary>Livello fertilizzante 0–100 (substrato).</summary>
        public int FertilizerPercent { get; private set; }
        /// <summary>Etichetta barra fertilizzante (stile idratazione).</summary>
        public string FertilizerMeterLabel { get; private set; }
        public int LightStressPercent { get; private set; }
        /// <summary>Drift giornaliero accodato della cupola (PhSystem), per tooltip/dettaglio.</summary>
        public string PhDomeDriftText { get; private set; }
        /// <summary>pH cupola corrente (-100…+100), stessa scala della TopBar DRIFT pH.</summary>
        public string PhDomeAmbientValueText { get; private set; }
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
        /// <summary>Stato LED per footer (testo sintetico).</summary>
        public string FooterLightStatusText { get; private set; }
        /// <summary>Stato irrigazione per footer.</summary>
        public string FooterIrrigationStatusText { get; private set; }

        public PlantCard4vNeedSignal HydrationNeedSignal { get; private set; }
        public PlantCard4vNeedSignal PhNeedSignal { get; private set; }
        public PlantCard4vNeedSignal FertilizerNeedSignal { get; private set; }
        public PlantCard4vNeedSignal ConditionNeedSignal { get; private set; }

        public string HydrationRowTooltip { get; private set; }
        /// <summary>Tooltip riga riepilogo bisogni (somma delle aree).</summary>
        public string SummaryRowTooltip { get; private set; }
        public string PhRowTooltip { get; private set; }
        public string FertilizerRowTooltip { get; private set; }
        public string ConditionRowTooltip { get; private set; }

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
            PhSystem phSystem,
            PlantCard4vVoReactionRequest reactionRequest = null,
            int phraseVariantSalt = 0)
        {
            var model = new PlantCard4vCareViewModel();
            model.BuildInternal(pot, state, plantData, config, phSystem, reactionRequest, phraseVariantSalt);
            return model;
        }

        private int _phraseVariantSalt;

        private void BuildInternal(
            PotSlot pot,
            PotStateModel state,
            PlantData plantData,
            PotSystemConfig config,
            PhSystem phSystem,
            PlantCard4vVoReactionRequest reactionRequest = null,
            int phraseVariantSalt = 0)
        {
            _phraseVariantSalt = phraseVariantSalt;
            PotId = pot != null && !string.IsNullOrWhiteSpace(pot.PotId) ? pot.PotId : (state != null ? state.PotId : "POT-???");
            ShortPotId = BuildShortPotId(PotId);
            IsEmpty = state == null || state.IsEmpty || !state.HasPlant;
            IsDead = state != null && (PlantCondition)state.ConditionLabel == PlantCondition.Morta;
            IsWateringActive = state != null && state.WateringSystemOn;
            LedState = state != null ? state.LedSystemState : LedSystemState.Off;

            if (IsEmpty)
            {
                BuildEmpty(state, phSystem);
                return;
            }

            PlantName = ResolvePlantName(state, plantData);
            PlantSubtitle = ResolvePlantSubtitle(state, plantData);
            SpeciesLine = ResolveSpeciesLine(state, plantData);
            LifeState = FormatLifeState((PlantStage)state.Stage, IsDead);
            StageDetail = $"FASE {Mathf.Max(0, state.Stage)} - {FormatStageDetail((PlantStage)state.Stage, _phraseVariantSalt)}";
            ConditionLine = $"Condizione: {FormatConditionDisplayName((PlantCondition)state.ConditionLabel, state.ConditionScore)}";
            ConditionStatusSignal = ResolveConditionStatusSignal((PlantCondition)state.ConditionLabel, state.ConditionScore);
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
            FertilizerPercent = Mathf.Clamp(state.FertilizerLevel, 0, 100);
            FertilizerMeterLabel = $"FERTILIZZANTE {FertilizerPercent}%";
            ConditionText = ResolveConditionText(state);
            MoldText = ResolveMoldText(state);

            ResolveNeedRiskAndActions(state, plantData, stageReq, config, phSystem, currentPh);
            ResolveNeedRowSignalsAndTooltips(state, plantData, stageReq, phSystem, currentPh);
            ResolveFooterHardwareStatus(state);
            FooterStateLine = $"{FormatLifeState((PlantStage)state.Stage, IsDead)} - {BuildFooterForStage((PlantStage)state.Stage, IsDead, _phraseVariantSalt)}";

            if (reactionRequest != null && state != null && !IsEmpty
                && PlantCard4vBiologistReactionVo.TryBuildLine(
                    reactionRequest,
                    state,
                    plantData,
                    stageReq,
                    config,
                    phSystem,
                    currentPh,
                    out string reactLine,
                    out string reactHintId))
            {
                VoHintLine = reactLine;
                VoHintId = reactHintId;
            }
        }

        private void BuildEmpty(PotStateModel state, PhSystem phSystem)
        {
            PlantName = "VASO VUOTO";
            PlantSubtitle = "PROCEDURA PLANT ASSENTE";
            SpeciesLine = "Specie: nessuna";
            LifeState = "VUOTO";
            StageDetail = "NESSUNA ATTIVITA' RILEVATA";
            ConditionLine = "Condizione: N/D";
            ConditionStatusSignal = PlantCard4vNeedSignal.Ok;
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
            FertilizerPercent = 0;
            FertilizerMeterLabel = "FERTILIZZANTE 0%";
            ConditionText = "---";
            MoldText = "---";
            SetPlantCardVo(
                "Solo polvere e promesse. Prima serve una procedura di impianto.",
                VoParamTopic.None,
                "empty",
                state,
                null,
                null,
                phSystem,
                0f);
            FooterStateLine = "VUOTO - Tracce di vita assenti.";
            PrimaryAction = PlantCard4vActionKind.TerminalPlant;
            SecondaryAction = PlantCard4vActionKind.None;
            RiskSegments = 0;
            ShowRiskDetailPanel = true;
            HydrationNeedSignal = PlantCard4vNeedSignal.Warning;
            PhNeedSignal = PlantCard4vNeedSignal.Ok;
            FertilizerNeedSignal = PlantCard4vNeedSignal.Ok;
            ConditionNeedSignal = PlantCard4vNeedSignal.Ok;
            HydrationRowTooltip = "Vaso vuoto: nessuna idratazione da monitorare.";
            SummaryRowTooltip = $"{MainNeed}\n{MainNeedSubtitle}";
            PhRowTooltip = "Nessun dato: vaso senza coltura attiva.";
            FertilizerRowTooltip = "Nessun dato: vaso senza coltura attiva.";
            ConditionRowTooltip = "Nessun dato: vaso senza coltura attiva.";
            FooterLightStatusText = "---";
            FooterIrrigationStatusText = "---";
        }

        private void ResolveFooterHardwareStatus(PotStateModel state)
        {
            if (state == null)
            {
                FooterLightStatusText = "---";
                FooterIrrigationStatusText = "---";
                return;
            }

            FooterLightStatusText = state.LedSystemState switch
            {
                LedSystemState.Blue => "LED blu ON",
                LedSystemState.Red => "LED rossa ON",
                _ => "Luce spenta"
            };
            FooterIrrigationStatusText = state.WateringSystemOn
                ? "Irrigazione ON"
                : "Irrigazione OFF";
        }

        private void ResolveNeedRowSignalsAndTooltips(
            PotStateModel state,
            PlantData plantData,
            StageRequirements stageReq,
            PhSystem phSystem,
            float currentPh)
        {
            if (stageReq == null)
            {
                HydrationNeedSignal = PlantCard4vNeedSignal.Ok;
            }
            else if (HydrationPercent < stageReq.hydrationMin)
            {
                HydrationNeedSignal = IsWateringActive ? PlantCard4vNeedSignal.Attention : PlantCard4vNeedSignal.Warning;
            }
            else if (HydrationPercent > stageReq.hydrationMax)
            {
                HydrationNeedSignal = PlantCard4vNeedSignal.Warning;
            }
            else
            {
                HydrationNeedSignal = PlantCard4vNeedSignal.Ok;
            }

            HydrationRowTooltip = BuildHydrationRowTooltip(state, stageReq, HydrationPercent, IsWateringActive);

            if (plantData != null && phSystem != null && !plantData.IsPhInOptimalRange(currentPh))
            {
                PhNeedSignal = PlantCard4vNeedSignal.Warning;
                bool low = currentPh < plantData.OptimalPhMin;
                PhRowTooltip = low
                    ? $"pH cupola fuori dall'intervallo ottimale della specie ({plantData.OptimalPhMin:0} / {plantData.OptimalPhMax:0}): valore troppo basso. Drift accodato {PhDomeDriftText}, banda ambiente {PhDomeBandShort}. Preferenza chimica pianta: {PlantPhPreferenceLabel}."
                    : $"pH cupola fuori dall'intervallo ottimale della specie ({plantData.OptimalPhMin:0} / {plantData.OptimalPhMax:0}): valore troppo alto. Drift accodato {PhDomeDriftText}, banda ambiente {PhDomeBandShort}. Preferenza chimica pianta: {PlantPhPreferenceLabel}.";
            }
            else if (phSystem != null && Mathf.Abs(phSystem.GetTotalDailyDrift()) >= 0.8f)
            {
                PhNeedSignal = PlantCard4vNeedSignal.Attention;
                PhRowTooltip = $"Drift giornaliero accodato {PhDomeDriftText} (si applica a fine giornata). Banda ambiente: {PhDomeBandShort}. Preferenza specie: {PlantPhPreferenceLabel}. Monitora additivi se il trend peggiora.";
            }
            else
            {
                PhNeedSignal = PlantCard4vNeedSignal.Ok;
                PhRowTooltip = $"Drift giornaliero accodato {PhDomeDriftText}. Banda ambiente {PhDomeBandShort}. Preferenza chimica specie: {PlantPhPreferenceLabel}. Parametri entro tolleranza operativa.";
            }

            switch (FertilizerText)
            {
                case "BASSO":
                    FertilizerNeedSignal = PlantCard4vNeedSignal.Warning;
                    break;
                case "ALTO":
                    FertilizerNeedSignal = PlantCard4vNeedSignal.Attention;
                    break;
                default:
                    FertilizerNeedSignal = PlantCard4vNeedSignal.Ok;
                    break;
            }

            FertilizerRowTooltip = BuildFertilizerRowTooltip(state, stageReq);
            if (!string.IsNullOrWhiteSpace(FertilizerText) && FertilizerText != "---")
                FertilizerRowTooltip += $" Indicatore: {FertilizerText}.";

            if (LightStressPercent >= 80)
            {
                ConditionNeedSignal = PlantCard4vNeedSignal.Warning;
            }
            else if (LightStressPercent >= 40)
            {
                ConditionNeedSignal = PlantCard4vNeedSignal.Attention;
            }
            else
            {
                ConditionNeedSignal = PlantCard4vNeedSignal.Ok;
            }

            ConditionRowTooltip = $"Stress da luce {LightStressPercent}%. LED preferito: {PreferredLightLine}.";

            if (ShouldPreserveNarrativeMainNeed(state))
            {
                SummaryRowTooltip = string.IsNullOrWhiteSpace(MainNeedSubtitle)
                    ? MainNeed
                    : $"{MainNeed}\n{MainNeedSubtitle}";
            }
            else
                ComposeAggregateSummaryNeedFields();
        }

        private static bool ShouldPreserveNarrativeMainNeed(PotStateModel state)
        {
            if (state == null)
                return true;
            if ((PlantCondition)state.ConditionLabel == PlantCondition.Morta)
                return true;
            if (state.IsInfested)
                return true;
            if (state.Stage == (int)PlantStage.HarvestReady && state.AmountFruits > 0f)
                return true;
            return false;
        }

        private static string BuildHydrationRowTooltip(
            PotStateModel state,
            StageRequirements stageReq,
            int hydrationPercent,
            bool wateringActive)
        {
            if (state == null)
                return "Idratazione: stato non disponibile.";
            if (stageReq == null)
                return $"Idratazione substrato {hydrationPercent}%.";

            if (hydrationPercent < stageReq.hydrationMin)
            {
                return wateringActive
                    ? $"Idratazione {hydrationPercent}% sotto il minimo di fase ({stageReq.hydrationMin}%): irrigazione attiva, attendi stabilizzazione."
                    : $"Idratazione {hydrationPercent}% sotto il minimo ({stageReq.hydrationMin}%): serve reintegro idrico controllato.";
            }

            if (hydrationPercent > stageReq.hydrationMax)
                return $"Idratazione {hydrationPercent}% sopra il massimo ({stageReq.hydrationMax}%): rischio stress da eccesso d'acqua.";

            return $"Idratazione {hydrationPercent}% nel range di fase ({stageReq.hydrationMin}–{stageReq.hydrationMax}%): regime idrico coerente.";
        }

        /// <summary>
        /// Sovrascrive MainNeed/MainNeedSubtitle con un riepilogo sintetico delle quattro aree (non ripete il dettaglio idrico).
        /// </summary>
        private void ComposeAggregateSummaryNeedFields()
        {
            var urgent = new List<string>();
            var watch = new List<string>();

            void Add(PlantCard4vNeedSignal signal, string label)
            {
                if (signal == PlantCard4vNeedSignal.Warning)
                    urgent.Add(label);
                else if (signal == PlantCard4vNeedSignal.Attention)
                    watch.Add(label);
            }

            Add(HydrationNeedSignal, "idratazione");
            Add(PhNeedSignal, "pH cupola");
            Add(FertilizerNeedSignal, "nutrimento");
            Add(ConditionNeedSignal, "luce");

            if (urgent.Count == 0 && watch.Count == 0)
            {
                MainNeed = "Quadro generale: parametri coerenti";
                MainNeedSubtitle = "Le richieste della fase risultano soddisfatte nelle quattro aree monitorate.";
            }
            else if (urgent.Count > 0)
            {
                MainNeed = "Priorità operative urgenti";
                MainNeedSubtitle = string.Join(", ", urgent)
                    + (watch.Count > 0 ? $". Da monitorare: {string.Join(", ", watch)}." : ".");
            }
            else
            {
                MainNeed = "Richieste da monitorare";
                MainNeedSubtitle = string.Join(", ", watch) + ".";
            }

            SummaryRowTooltip = string.IsNullOrWhiteSpace(MainNeedSubtitle)
                ? MainNeed
                : $"{MainNeed}\n{MainNeedSubtitle}";
        }

        private static string BuildFertilizerRowTooltip(PotStateModel state, StageRequirements stageReq)
        {
            if (state == null)
                return "---";

            bool dead = (PlantCondition)state.ConditionLabel == PlantCondition.Morta;
            string stageName = FormatLifeState((PlantStage)state.Stage, dead);

            if (stageReq == null)
                return $"Fase {stageName}: livello fertilizzante nel substrato {state.FertilizerLevel}%.";

            int min = stageReq.fertilizerMin;
            int max = stageReq.fertilizerMax;
            return $"Fase {stageName}: nutrimento nel substrato {state.FertilizerLevel}%. " +
                   $"Per questa fase il range consigliato è {min}–{max}%. " +
                   "Sotto il minimo la crescita può rallentare o bloccarsi; oltre il massimo il surplus è sprecato e può generare stress. " +
                   "Scegli un fertilizzante genetico compatibile o attendi l'assorbimento.";
        }

        private void ResolveDomePhRow(PhSystem phSystem)
        {
            if (phSystem == null)
            {
                PhDomeDriftText = "---";
                PhDomeBandShort = "---";
                PhDomeAmbientValueText = "---";
                return;
            }

            PhDomeAmbientValueText = phSystem.CurrentPh.ToString("F1", ItCulture);
            float drift = phSystem.GetTotalDailyDrift();
            PhDomeDriftText = drift.ToString("+0.0;-0.0;0.0", ItCulture);
            PhDomeBandShort = FormatPhBandShort(phSystem.EvaluateState());
        }

        public static string FormatPhBandShort(PhSystem.PhBand band)
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
                SetPlantCardVo(
                    "Non risponde piu'. Anche il contenimento, a volte, arriva tardi.",
                    VoParamTopic.Condition,
                    "dead",
                    state,
                    plantData,
                    stageReq,
                    phSystem,
                    currentPh);
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
                SetPlantCardVo(
                    "La superficie e' viva nel modo sbagliato. Il contenimento sta cedendo.",
                    VoParamTopic.Condition,
                    "infested",
                    state,
                    plantData,
                    stageReq,
                    phSystem,
                    currentPh);
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
                SetPlantCardVo(
                    "Troppa acqua. Il substrato sta diventando una seconda coltura.",
                    VoParamTopic.Condition,
                    "mold_critical",
                    state,
                    plantData,
                    stageReq,
                    phSystem,
                    currentPh);
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
                SetPlantCardVo(
                    "Ha dato tutto. Ora serve la macchina, non la mano.",
                    VoParamTopic.Condition,
                    "harvest_ready",
                    state,
                    plantData,
                    stageReq,
                    phSystem,
                    currentPh);
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
                    SetPlantCardVo(
                        "L'acqua e' in viaggio. Il resto dei parametri dira' se basta.",
                        VoParamTopic.Water,
                        "water_active",
                        state,
                        plantData,
                        stageReq,
                        phSystem,
                        currentPh);
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
                SetPlantCardVo(
                    "Il substrato e' troppo secco. Non sta morendo, ma sta aspettando.",
                    VoParamTopic.Water,
                    "water_low",
                    state,
                    plantData,
                    stageReq,
                    phSystem,
                    currentPh);
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
                SetPlantCardVo(
                    "Troppa acqua. La superficie comincia a diventare viva nel modo sbagliato.",
                    VoParamTopic.Water,
                    "water_high",
                    state,
                    plantData,
                    stageReq,
                    phSystem,
                    currentPh);
                PrimaryAction = state.WateringSystemOn ? PlantCard4vActionKind.Water : PlantCard4vActionKind.Prune;
                SecondaryAction = state.WateringSystemOn ? PlantCard4vActionKind.Prune : PlantCard4vActionKind.Additive;
                RiskSegments = 4;
                ShowRiskDetailPanel = true;
                return;
            }

            var lightAction = ResolveLightSecondary(state, plantData, stageReq);
            if (lightAction != PlantCard4vActionKind.None)
            {
                int rot = PhraseRot(_phraseVariantSalt);
                MainNeed = PickLightGuidanceMainNeed(rot);
                if (TryBuildActiveLightRisk(state, plantData, out string lightRisk, out string lightCause, out int lightRiskSegments))
                {
                    MainRisk = lightRisk;
                    RiskCause = lightCause;
                    RiskLevelText = "RISCHIO LUCE";
                    RiskSegments = lightRiskSegments;
                    MainNeedSubtitle = lightCause;
                    SetPlantCardVo(
                        PickLightStressVoPrimary(rot),
                        VoParamTopic.Light,
                        "light_need",
                        state,
                        plantData,
                        stageReq,
                        phSystem,
                        currentPh);
                }
                else
                {
                    MainRisk = "Nessuna anomalia critica";
                    RiskCause = $"Stress luce {LightStressPercent}%";
                    RiskLevelText = "NESSUN RISCHIO LUCE";
                    RiskSegments = 0;
                    MainNeedSubtitle = PickLightGuidanceCalmSubtitle(rot);
                    SetPlantCardVo(
                        PickLightGuidanceCalmVoPrimary(rot),
                        VoParamTopic.Light,
                        "light_need",
                        state,
                        plantData,
                        stageReq,
                        phSystem,
                        currentPh);
                }
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
                SetPlantCardVo(
                    tooLow
                        ? "L'ambiente la sta tirando verso il basso. Il pH e' fuori tono."
                        : "L'ambiente e' troppo duro. Il pH e' fuori tono.",
                    VoParamTopic.Ph,
                    tooLow ? "ph_low" : "ph_high",
                    state,
                    plantData,
                    stageReq,
                    phSystem,
                    currentPh);
                PrimaryAction = PlantCard4vActionKind.Additive;
                SecondaryAction = PlantCard4vActionKind.None;
                RiskSegments = 5;
                ShowRiskDetailPanel = true;
                return;
            }

            if (stageReq != null && !FertilizerCarePolicy.ShouldTreatFertilizerAsOptional((PlantStage)state.Stage, stageReq) && state.FertilizerLevel < stageReq.fertilizerMin)
            {
                MainNeed = "Ha bisogno di nutrienti";
                MainNeedSubtitle = $"Fertilizzazione sotto il minimo ({stageReq.fertilizerMin}%): la fase corrente richiede piu' input nutritivo.";
                MainRisk = "Crescita rallentata";
                RiskCause = $"Fertilizzante sotto range ({stageReq.fertilizerMin}%)";
                RiskLevelText = "RISCHIO BASSO";
                SetPlantCardVo(
                    "Sta provando a costruire tessuto nuovo con troppo poco materiale.",
                    VoParamTopic.Fertilizer,
                    "fert_low",
                    state,
                    plantData,
                    stageReq,
                    phSystem,
                    currentPh);
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
            SetPlantCardVo(
                "Per ora tiene. Non tutto cio' che vive chiede di essere toccato.",
                VoParamTopic.None,
                "stable",
                state,
                plantData,
                stageReq,
                phSystem,
                currentPh);
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

            if (stageReq != null && !FertilizerCarePolicy.ShouldTreatFertilizerAsOptional((PlantStage)state.Stage, stageReq) && state.FertilizerLevel < stageReq.fertilizerMin)
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

        private static string FormatConditionDisplayName(PlantCondition condition, int conditionScore)
        {
            return condition switch
            {
                PlantCondition.Rigogliosa => "Rigogliosa",
                PlantCondition.Sana => "Sana",
                PlantCondition.Stressata => "Stressata",
                PlantCondition.Appassita => "Appassita",
                PlantCondition.Critica => "Critica",
                PlantCondition.Morta => "Morta",
                _ => conditionScore >= 70 ? "Sana" : conditionScore >= 40 ? "Stressata" : "Critica"
            };
        }

        private static PlantCard4vNeedSignal ResolveConditionStatusSignal(PlantCondition condition, int conditionScore)
        {
            return condition switch
            {
                PlantCondition.Rigogliosa => PlantCard4vNeedSignal.Ok,
                PlantCondition.Sana => PlantCard4vNeedSignal.Ok,
                PlantCondition.Stressata => PlantCard4vNeedSignal.Attention,
                PlantCondition.Appassita => PlantCard4vNeedSignal.Attention,
                PlantCondition.Critica => PlantCard4vNeedSignal.Warning,
                PlantCondition.Morta => PlantCard4vNeedSignal.Warning,
                _ => conditionScore >= 70
                    ? PlantCard4vNeedSignal.Ok
                    : conditionScore >= 40
                        ? PlantCard4vNeedSignal.Attention
                        : PlantCard4vNeedSignal.Warning
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

        private static int PhraseRot(int salt) => Mathf.Abs(salt) % 1009;

        private static string PickLightGuidanceMainNeed(int rot)
        {
            return (rot % 3) switch
            {
                0 => "Luce da ottimizzare",
                1 => "Calibra lo spettro LED",
                _ => "Aggiusta fotoperiodo e colore",
            };
        }

        private static string PickLightGuidanceCalmSubtitle(int rot)
        {
            return (rot % 3) switch
            {
                0 => "Idratazione in range: regola LED per avvicinare lo stress luce alla fascia 20–80%.",
                1 => "Il substrato è in equilibrio: il segnale principale è la luce da affinare per questa fase.",
                _ => "Nessuna emergenza idrica: concentrati su durata e colore dell’illuminazione.",
            };
        }

        private static string PickLightGuidanceCalmVoPrimary(int rot)
        {
            return (rot % 3) switch
            {
                0 => "Nessun allarme. Il profilo luce si può rifinire senza fretta.",
                1 => "Stato idrico buono: resta da scegliere meglio la luce per questa fase.",
                _ => "Tutto stabile sul bagnato; la leva utile adesso è solo la luce.",
            };
        }

        private static string PickLightStressVoPrimary(int rot)
        {
            return (rot % 3) switch
            {
                0 => "La luce sta lasciando tracce. Il Pot non e' ancora in zona burn, ma ci si sta avvicinando.",
                1 => "Lo spettro attuale spinge lo stress: serve un intervento sulla luce prima che salga oltre soglia.",
                _ => "LED e fotoperiodo chiedono una correzione: il trend stress non è neutro.",
            };
        }

        private static string FormatStageDetail(PlantStage stage, int salt)
        {
            int r = PhraseRot(salt);
            return stage switch
            {
                PlantStage.Seed => "IMPIANTO",
                PlantStage.Sprout => (r % 3) switch
                {
                    0 => "RADICAMENTO",
                    1 => "ASSETTAZIONE LUCI",
                    _ => "PROTOCOLLO FOTOPERIODO",
                },
                PlantStage.Growth => "CRESCITA ATTIVA",
                PlantStage.Flowering => "RIPRODUZIONE",
                PlantStage.HarvestReady => "RACCOLTA TERMINALE",
                PlantStage.Resting => "METABOLISMO RIDOTTO",
                _ => "NESSUNA ATTIVITA'"
            };
        }

        private static string BuildFooterForStage(PlantStage stage, bool dead, int salt)
        {
            if (dead)
                return "Non risponde piu'.";

            int r = PhraseRot(salt);
            return stage switch
            {
                PlantStage.Seed => "E' piccolo, ma non dorme.",
                PlantStage.Sprout => (r % 3) switch
                {
                    0 => "Sta fissando radici e tolleranze.",
                    1 => "Piccoli aggiustamenti ora evitano stress dopo.",
                    _ => "Segnali buoni: resta coerente con luce e acqua.",
                },
                PlantStage.Growth => "Ogni giorno un po' piu' forte.",
                PlantStage.Flowering => "Un linguaggio che solo tu capisci.",
                PlantStage.HarvestReady => "Ha completato il suo ciclo.",
                PlantStage.Resting => "Non disturbarlo.",
                _ => "Tracce di vita assenti."
            };
        }

        private void SetPlantCardVo(
            string primarySentence,
            VoParamTopic primaryTopic,
            string voIdBase,
            PotStateModel state,
            PlantData plantData,
            StageRequirements stageReq,
            PhSystem phSystem,
            float currentPh)
        {
            string secondary = PickSecondaryVoSentence(primaryTopic, state, plantData, stageReq, phSystem, currentPh, out VoParamTopic secTopic);
            string trend = FormatVoTrendSentence(state);
            VoHintLine = JoinVoSentences(primarySentence, secondary, trend);
            int fd = state != null ? state.ForecastDirection : 1;
            VoHintId = $"{voIdBase}|{(int)secTopic}|{fd}";
        }

        private string PickSecondaryVoSentence(
            VoParamTopic primaryTopic,
            PotStateModel state,
            PlantData plantData,
            StageRequirements stageReq,
            PhSystem phSystem,
            float currentPh,
            out VoParamTopic pickedTopic)
        {
            VoParamTopic[] order =
            {
                VoParamTopic.Light,
                VoParamTopic.Water,
                VoParamTopic.Ph,
                VoParamTopic.Fertilizer,
                VoParamTopic.Condition,
            };

            foreach (VoParamTopic topic in order)
            {
                if (primaryTopic != VoParamTopic.None && topic == primaryTopic)
                    continue;
                string line = BuildVoSentenceForTopic(topic, state, plantData, stageReq, phSystem, currentPh);
                if (!string.IsNullOrWhiteSpace(line))
                {
                    pickedTopic = topic;
                    return line;
                }
            }

            pickedTopic = VoParamTopic.Condition;
            return "Il resto della suite sensoriale non aggiunge urgenze oltre al punto principale.";
        }

        private string BuildVoSentenceForTopic(
            VoParamTopic topic,
            PotStateModel state,
            PlantData plantData,
            StageRequirements stageReq,
            PhSystem phSystem,
            float currentPh)
        {
            switch (topic)
            {
                case VoParamTopic.Water:
                    if (state == null) return null;
                    if (stageReq != null)
                    {
                        if (HydrationPercent < stageReq.hydrationMin)
                        {
                            return state.WateringSystemOn
                                ? "Sul fronte acqua il substrato e' ancora sotto soglia ma l'irrigazione e' gia' attiva."
                                : "Sul fronte acqua il substrato resta sotto il minimo di fase: serve reintegro controllato.";
                        }
                        if (HydrationPercent > stageReq.hydrationMax)
                            return "Sul fronte acqua sei sopra il massimo di fase: vigila su ristagni e miceti.";
                        return $"Sul fronte acqua sei al {HydrationPercent}%, nel range {stageReq.hydrationMin}-{stageReq.hydrationMax}% previsto.";
                    }
                    return $"Sul fronte acqua la lettura e' {HydrationPercent}%: segui il protocollo POT.";

                case VoParamTopic.Light:
                    if (state == null) return null;
                    string led = state.LedSystemState == LedSystemState.Off
                        ? "spento"
                        : (state.LedSystemState == LedSystemState.Blue ? "blu" : "rosso");
                    if (LightStressPercent > 80)
                        return $"Sul fronte luce il LED e' {led} e lo stress e' al {LightStressPercent}%, vicino alla saturazione.";
                    if (LightStressPercent < 20)
                        return $"Sul fronte luce il LED e' {led} e lo stress e' al {LightStressPercent}%, sotto la soglia di beneficio.";
                    return $"Sul fronte luce il LED e' {led} e lo stress e' al {LightStressPercent}%, nella fascia operativa 20-80%.";

                case VoParamTopic.Ph:
                    if (plantData == null || phSystem == null) return null;
                    if (!plantData.IsPhInOptimalRange(currentPh))
                        return $"Sul fronte chimico il pH cupola ({currentPh.ToString("0.0", ItCulture)}) esce dalla finestra {plantData.OptimalPhMin:0}-{plantData.OptimalPhMax:0} della specie.";
                    return $"Sul fronte chimico il pH ({currentPh.ToString("0.0", ItCulture)}) resta nella tolleranza {plantData.OptimalPhMin:0}-{plantData.OptimalPhMax:0}.";

                case VoParamTopic.Fertilizer:
                    if (state == null || stageReq == null) return null;
                    if (FertilizerCarePolicy.ShouldTreatFertilizerAsOptional((PlantStage)state.Stage, stageReq))
                        return null;
                    if (state.FertilizerLevel < stageReq.fertilizerMin)
                        return $"Sul fronte nutrimento sei al {state.FertilizerLevel}%, sotto il minimo {stageReq.fertilizerMin}% di questa fase.";
                    if (state.FertilizerLevel > stageReq.fertilizerMax)
                        return $"Sul fronte nutrimento sei al {state.FertilizerLevel}%, sopra il tetto {stageReq.fertilizerMax}%.";
                    return $"Sul fronte nutrimento sei al {state.FertilizerLevel}%, nel target {stageReq.fertilizerMin}-{stageReq.fertilizerMax}%.";

                case VoParamTopic.Condition:
                    if (state == null) return null;
                    return $"Sul fronte tessuti la condizione segnala {ConditionText} con muffa {MoldText}.";

                default:
                    return null;
            }
        }

        private static string FormatVoTrendSentence(PotStateModel state)
        {
            if (state == null || state.IsEmpty || !state.HasPlant)
                return "Chi legge il trend oggi non ha biomassa registrata: occorre una coltura attiva.";

            var d = (ForecastDirection)state.ForecastDirection;
            return d switch
            {
                ForecastDirection.Up => "Chi legge il trend la vede verso il miglioramento rispetto a ieri.",
                ForecastDirection.Down => "Chi legge il trend la vede verso il peggioramento rispetto a ieri.",
                _ => "Chi legge il trend la vede stabile rispetto a ieri.",
            };
        }

        private static string JoinVoSentences(string primary, string secondary, string trend)
        {
            static string Norm(string s)
            {
                s = (s ?? string.Empty).Trim();
                if (s.Length == 0) return string.Empty;
                char last = s[s.Length - 1];
                if (last != '.' && last != '?' && last != '!' && last != ';')
                    s += ".";
                return s;
            }

            return $"{Norm(primary)}\n{Norm(secondary)}\n{Norm(trend)}".Trim();
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
