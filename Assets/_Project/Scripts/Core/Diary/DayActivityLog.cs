using System;
using System.Collections.Generic;
using _Project.Sporae.Core;

namespace _Project
{
    /// <summary>
    /// Registro eventi del giorno corrente per il resoconto End of Day.
    /// Si resetta su DayCycleSystem.OnDayChanged.
    /// </summary>
    public class DayActivityLog
    {
        public struct HarvestEntry
        {
            public string PotId;
            public string PlantCode;
            public int Level;
            public int Amount;
        }

        /// <summary>Voce Dome per testo descrittivo nel diario (es. "Hai piantato un seme di X nel POT Y").</summary>
        public struct DomeActivityEntry
        {
            public string PotId;
            /// <summary>Plant, Water, Light, Fertilize, Pruning, Started (azione avviata ma non completata).</summary>
            public string ActionKind;
            public string PlantCode;
            public string PlantDisplayName;
        }

        /// <summary>Voce Lab per testo descrittivo (es. "Hai estratto N spore da frutto").</summary>
        public struct LabActivityEntry
        {
            public string LabType;
            public string InputDescription;
            public int SporeOut;
            public int Cell001Out;
            public int Cell002Out;
            public int Cell003Out;
        }

        private readonly List<string> _potIdsWateringTurnedOn = new();
        private readonly List<string> _potIdsWateringTurnedOff = new();
        private readonly List<string> _potIdsWithDomeActionStarted = new();
        private readonly List<DomeActivityEntry> _domeEntries = new();
        private readonly List<HarvestEntry> _harvests = new();
        private readonly List<string> _labActionTypes = new();
        private readonly List<LabActivityEntry> _labEntries = new();

        private readonly DayCycleSystem _dayCycleSystem;

        public DayActivityLog()
        {
            _dayCycleSystem = ServiceContainer.Instance.Get<DayCycleSystem>();
            _dayCycleSystem.OnDayChanged += Clear;
        }

        ~DayActivityLog()
        {
            if (_dayCycleSystem != null)
                _dayCycleSystem.OnDayChanged -= Clear;
        }

        public void Clear(int _)
        {
            _potIdsWateringTurnedOn.Clear();
            _potIdsWateringTurnedOff.Clear();
            _potIdsWithDomeActionStarted.Clear();
            _domeEntries.Clear();
            _harvests.Clear();
            _labActionTypes.Clear();
            _labEntries.Clear();
        }

        /// <summary>
        /// Registra una voce Dome strutturata per il testo descrittivo nel diario.
        /// </summary>
        public void RecordDomeAction(DomeActivityEntry entry)
        {
            if (string.IsNullOrEmpty(entry.PotId)) return;
            _domeEntries.Add(entry);
            if (!_potIdsWithDomeActionStarted.Contains(entry.PotId))
                _potIdsWithDomeActionStarted.Add(entry.PotId);
        }

        /// <summary>
        /// Registra che su questo vaso è stata avviata un'azione (es. Water/Plant/Light).
        /// Aggiunge una voce con ActionKind "Started" per il diario descrittivo.
        /// </summary>
        public void RecordDomeActionStarted(string potId)
        {
            if (string.IsNullOrEmpty(potId)) return;
            if (!_potIdsWithDomeActionStarted.Contains(potId))
                _potIdsWithDomeActionStarted.Add(potId);
            _domeEntries.Add(new DomeActivityEntry { PotId = potId, ActionKind = "Started", PlantCode = null, PlantDisplayName = null });
        }

        public void RecordWateringToggle(string potId, bool isNowOn)
        {
            // #region agent log
            try
            {
                long ts = (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalMilliseconds;
                string safePotId = string.IsNullOrEmpty(potId) ? "" : potId.Replace("\\", "\\\\").Replace("\"", "\\\"");
                string logLine = "{\"location\":\"DayActivityLog.RecordWateringToggle\",\"message\":\"Dome watering recorded\",\"data\":{\"potId\":\"" + safePotId + "\",\"isNowOn\":" + (isNowOn ? "true" : "false") + "},\"timestamp\":" + ts + ",\"hypothesisId\":\"H1\"}\n";
                System.IO.File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", logLine);
            }
            catch { }
            // #endregion
            if (string.IsNullOrEmpty(potId)) return;
            if (isNowOn)
            {
                _potIdsWateringTurnedOn.Add(potId);
                _domeEntries.Add(new DomeActivityEntry { PotId = potId, ActionKind = "Water", PlantCode = null, PlantDisplayName = null });
            }
            else
                _potIdsWateringTurnedOff.Add(potId);
        }

        public void RecordHarvest(string potId, string plantCode, int level, int amount)
        {
            if (string.IsNullOrEmpty(potId)) return;
            _harvests.Add(new HarvestEntry
            {
                PotId = potId,
                PlantCode = plantCode ?? "",
                Level = Math.Max(0, level),
                Amount = Math.Max(0, amount)
            });
        }

        public void RecordLabAction(string labType)
        {
            if (string.IsNullOrEmpty(labType)) return;
            _labActionTypes.Add(labType);
            _labEntries.Add(new LabActivityEntry { LabType = labType, InputDescription = "", SporeOut = 0, Cell001Out = 0, Cell002Out = 0, Cell003Out = 0 });
        }

        /// <summary>
        /// Registra una voce Lab strutturata (es. Extractor con spore/cellule e tipo input).
        /// </summary>
        public void RecordLabAction(LabActivityEntry entry)
        {
            if (string.IsNullOrEmpty(entry.LabType)) return;
            _labActionTypes.Add(entry.LabType);
            _labEntries.Add(entry);
        }

        public IReadOnlyList<string> PotIdsWateringTurnedOnThisDay => _potIdsWateringTurnedOn;
        public IReadOnlyList<string> PotIdsWateringTurnedOffThisDay => _potIdsWateringTurnedOff;
        public IReadOnlyList<string> PotIdsWithDomeActionStartedThisDay => _potIdsWithDomeActionStarted;
        public IReadOnlyList<DomeActivityEntry> DomeEntriesThisDay => _domeEntries;
        public IReadOnlyList<HarvestEntry> HarvestsThisDay => _harvests;
        public IReadOnlyList<string> LabActionTypesThisDay => _labActionTypes;
        public IReadOnlyList<LabActivityEntry> LabEntriesThisDay => _labEntries;
    }
}
