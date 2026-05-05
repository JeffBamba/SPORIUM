using _Project;
using Sporae.Dome.PotSystem.Condition;
using Sporae.Dome.PotSystem.Growth;
using UnityEngine;
using _Project.Sporae.Core;

namespace Sporae.UI.UIToolkit.PlantCard4v
{
    /// <summary>Stato pianta/vaso prima di un'azione, per giudizio VO post-intervento.</summary>
    public struct PlantCard4vCareSnapshot
    {
        public bool HasPlant;
        public int Hydration;
        public int MoldRiskLevel;
        public bool IsInfested;
        public bool WateringSystemOn;
        public LedSystemState LedState;
        public int FertilizerLevel;
        public int ConditionLabel;
        public int Stage;
        public float DomePh;

        public static PlantCard4vCareSnapshot Capture(PotStateModel state, PotSystemConfig config, PhSystem phSystem)
        {
            if (state == null || !state.HasPlant)
                return default;

            return new PlantCard4vCareSnapshot
            {
                HasPlant = true,
                Hydration = state.Hydration,
                MoldRiskLevel = state.MoldRiskLevel,
                IsInfested = state.IsInfested,
                WateringSystemOn = state.WateringSystemOn,
                LedState = state.LedSystemState,
                FertilizerLevel = state.FertilizerLevel,
                ConditionLabel = state.ConditionLabel,
                Stage = state.Stage,
                DomePh = phSystem != null ? phSystem.CurrentPh : 0f,
            };
        }

        public int GetHydrationPercent(PotSystemConfig config)
        {
            int maxH = config != null ? Mathf.Max(1, config.MaxHydration) : 10;
            return Mathf.Clamp(Mathf.RoundToInt((float)Hydration / maxH * 100f), 0, 100);
        }
    }

    /// <summary>Richiesta di riga VO reattiva dopo <see cref="PotEvents.EmitAction"/> da PlantCard4v.</summary>
    public sealed class PlantCard4vVoReactionRequest
    {
        public PotEvents.PotActionType Action { get; }
        public PlantCard4vCareSnapshot Before { get; }
        public string Detail { get; }
        public string Stamp { get; }

        public PlantCard4vVoReactionRequest(PotEvents.PotActionType action, PlantCard4vCareSnapshot before, string detail, string stamp)
        {
            Action = action;
            Before = before;
            Detail = detail ?? string.Empty;
            Stamp = stamp ?? string.Empty;
        }
    }

    /// <summary>Righe VO satiriche / black humor in risposta a interventi sul Pot (coscienza del biologo).</summary>
    public static class PlantCard4vBiologistReactionVo
    {
        public static bool TryBuildLine(
            PlantCard4vVoReactionRequest req,
            PotStateModel after,
            PlantData plantData,
            StageRequirements stageReq,
            PotSystemConfig config,
            PhSystem phSystem,
            float currentPh,
            out string line,
            out string voHintId)
        {
            line = null;
            voHintId = null;
            if (req == null || after == null || !after.HasPlant || !req.Before.HasPlant)
                return false;

            string built = BuildLine(req, after, plantData, stageReq, config, phSystem, currentPh);
            if (string.IsNullOrWhiteSpace(built))
                return false;

            line = built.Trim();
            voHintId = $"react|{(int)req.Action}|{req.Stamp}";
            return true;
        }

        private static string BuildLine(
            PlantCard4vVoReactionRequest req,
            PotStateModel after,
            PlantData plantData,
            StageRequirements stageReq,
            PotSystemConfig config,
            PhSystem phSystem,
            float currentPh)
        {
            switch (req.Action)
            {
                case PotEvents.PotActionType.Water:
                    return BuildWaterLine(req, after, stageReq, config);
                case PotEvents.PotActionType.Light:
                    return BuildLightLine(req, after, plantData, stageReq);
                case PotEvents.PotActionType.Pruning:
                    return BuildPruningLine(req, after);
                case PotEvents.PotActionType.Spray:
                    return BuildSprayLine(req, after, plantData, phSystem);
                case PotEvents.PotActionType.Fertilize:
                    return BuildFertilizeLine(req, after, plantData, stageReq);
                default:
                    return null;
            }
        }

        private static string BuildWaterLine(
            PlantCard4vVoReactionRequest req,
            PotStateModel after,
            StageRequirements stageReq,
            PotSystemConfig config)
        {
            bool nowOn = after.WateringSystemOn;
            bool wasOn = req.Before.WateringSystemOn;
            if (nowOn == wasOn)
                return "Toggle registrato. Se nulla e' cambiato, forse il problema sei tu, non il rubinetto.";

            int bp = req.Before.GetHydrationPercent(config);

            if (nowOn && !wasOn)
            {
                if (stageReq != null && bp < stageReq.hydrationMin)
                    return "Irrigazione accesa: il substrato aveva sete cartacea. Ora tocca all'acqua verificare l'ego del protocollo.";
                if (stageReq != null && bp > stageReq.hydrationMax)
                    return "Hai acceso le gocce su un acquitrino autorizzato. Il referto chiama *entusiasmo idrico*; io traduco.";
                return "Flusso attivo. Non e' miracolo: e' idraulica con firma e contabile d'umore nero.";
            }

            return "Flusso spento. Meno acqua, meno alibi per muffe pigre e filosofie anaerobiche.";
        }

        private static string BuildLightLine(
            PlantCard4vVoReactionRequest req,
            PotStateModel after,
            PlantData plantData,
            StageRequirements stageReq)
        {
            LedSystemState b = req.Before.LedState;
            LedSystemState a = after.LedSystemState;
            if (b == a)
                return "Comando LED: eseguito. Se lo spettro e' identico, e' arte concettuale, non agronomia.";

            if (a == LedSystemState.Off)
                return "Buio di gabinetto. La fotosintesi non applaude; il burn, si', sospira di sollievo.";

            if (plantData != null)
            {
                LedCompatibility compat = LedCompatibilityHelper.GetCompatibleLedTypes(plantData.Family);
                if (!LedCompatibilityHelper.IsLedCompatible(a, compat))
                {
                    return a == LedSystemState.Blue
                        ? "Blu su genetica che lo disprezza: firma elegante su uno stress gia' spiegabile."
                        : "Rosso dove serviva tatto: contrasto bello, botanica meno.";
                }
            }

            LedType? need = stageReq?.GetRequiredLed();
            if (need.HasValue)
            {
                LedSystemState want = need.Value == LedType.Blue ? LedSystemState.Blue : LedSystemState.Red;
                if (a == want)
                    return "Spettro in linea con la fase. Non e' talento: e' aver letto la tabella senza farsi suggestionare.";
                return "LED acceso, ma non e' quello che la fase pretendeva. Io: neutrale. La pianta: meno.";
            }

            return "Luce accesa senza clausole di fase. Chiamalo *mood irradiato* e spera nella statistica.";
        }

        private static string BuildPruningLine(PlantCard4vVoReactionRequest req, PotStateModel after)
        {
            bool hadPressure = req.Before.IsInfested || req.Before.MoldRiskLevel >= 2;
            bool relieved = after.MoldRiskLevel < req.Before.MoldRiskLevel
                || (req.Before.IsInfested && !after.IsInfested);

            if (relieved && hadPressure)
                return "Taglio utile: meno muffa, meno teatro da incubatrice. Accetto l'atto, non la tua biografia su Instagram.";

            if (!hadPressure)
                return "Forbici su tessuto quasi innocente: piu' vanity clinic che medicina. Firmato: coscienza che non applaude.";

            return "Potatura registrata. Se il substrato migliora, sara' un bel silenzio; se no, il gossip resta chimico.";
        }

        private static string BuildSprayLine(
            PlantCard4vVoReactionRequest req,
            PotStateModel after,
            PlantData plantData,
            PhSystem phSystem)
        {
            bool basic = req.Detail == Items.AdditiveBasic;
            bool acid = req.Detail == Items.AdditiveAcid;

            if (acid)
            {
                if (after.MoldRiskLevel > req.Before.MoldRiskLevel)
                    return "Acido versato: il miceto riceve un invito stampato su carta pesante. RSVP: probabile.";
                return "Acido aggiunto. Se il pH voleva scendere, ok; se no, carino uguale — in senso morboso.";
            }

            if (basic)
            {
                if (req.Before.MoldRiskLevel >= 2 || req.Before.IsInfested)
                    return "Basico spruzzato: meno rischio micotico, piu' silenzio. Non e' magia: e' disciplina con odore di clinica.";
                if (plantData != null && phSystem != null
                    && !plantData.IsPhInOptimalRange(req.Before.DomePh)
                    && req.Before.DomePh < plantData.OptimalPhMin)
                    return "Correzione verso l'alto: il cupolone smussa l'acidita'. Non ringraziare: fattura arrivera' ugualmente.";
                return "Spray quando non ardeva: caro, poetico, e sospetto come un complimento in laboratorio.";
            }

            return "Bottiglia svuotata; spiegazioni parziali. Il biologo preferisce i numeri, tu preferisci il gesto.";
        }

        private static string BuildFertilizeLine(
            PlantCard4vVoReactionRequest req,
            PotStateModel after,
            PlantData plantData,
            StageRequirements stageReq)
        {
            bool wasAlive = req.Before.ConditionLabel != (int)PlantCondition.Morta;
            bool nowDead = after.ConditionLabel == (int)PlantCondition.Morta;

            if (nowDead && wasAlive)
                return "Nutrienti *interpretati* male. La pianta non protesta per mancanza di voce. Tu resti con la coscienza e una finestra sporca.";

            if (!wasAlive)
                return null;

            bool optionalFert = stageReq == null
                || FertilizerCarePolicy.ShouldTreatFertilizerAsOptional((PlantStage)after.Stage, stageReq);

            int beforeF = req.Before.FertilizerLevel;
            int afterF = after.FertilizerLevel;

            bool wasLow = stageReq != null && !optionalFert && beforeF < stageReq.fertilizerMin;
            bool nowInRange = stageReq != null && stageReq.IsFertilizerInRange(afterF);

            if (wasLow && nowInRange)
                return "Reintegro accettato: la fame smette di essere un'opinione del piano nutrizionale.";

            if (stageReq != null && !optionalFert && beforeF >= stageReq.fertilizerMin)
                return "Il substrato non digiunava: ora e' sazio e gia' sospettoso. Dosaggio: plausibile; necessita': in discussione.";

            if (optionalFert)
                return "Concime su fase che non lo pretendeva: hobby caro, effetto da oracolo — interpretabile.";

            return "Dosaggio archiviato. Se cresce, applausi sterili; se no, non incolpare solo oggi.";
        }
    }
}
