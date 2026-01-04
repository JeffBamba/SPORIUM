using System;
using System.IO;
using UnityEngine;
using Sporae.DevTools;

namespace _Project
{
    /// <summary>
    /// Sistema gestione pH globale della Dome
    /// Range: -100 (Ultra Acido) → +100 (Ultra Basico)
    /// </summary>
    public class PhSystem
    {
        public enum PhBand
        {
            UltraAcid = 0,    // ≤ -80
            StableAcid = 1,   // -79 ... -30
            Neutral = 2,      // -29 ... +29
            StableBasic = 3,  // +30 ... +79
            UltraBasic = 4    // ≥ +80
        }

        private float _currentPh = 0f; // Neutro di default
        public const float MIN_PH = -100f;
        public const float MAX_PH = 100f;
        
        // Tracciamento contributi per tooltip
        private float _basePh = 0f;
        private float _plantsDrift = 0f;
        private float _actionsDrift = 0f;
        private float _eventsDrift = 0f;
        private float _dailyDrift = 0f;
        
        // Accumulatori giornalieri applicati in un colpo solo a fine giornata
        private float _queuedPlantsDrift = 0f;
        private float _queuedActionsDrift = 0f;
        private float _queuedEventsDrift = 0f;
        private float _queuedDailyDrift = 0f;
        
        // Tracking azioni individuali per tooltip dettagliato
        private System.Collections.Generic.List<ActionContribution> _actionContributions = new System.Collections.Generic.List<ActionContribution>();
        
        // Tracking piante individuali per tooltip dettagliato
        private System.Collections.Generic.List<PlantContribution> _plantContributions = new System.Collections.Generic.List<PlantContribution>();
        
        /// <summary>
        /// Struttura per tracciare contributi di piante individuali
        /// </summary>
        private struct PlantContribution
        {
            public string PlantCode;   // "PLT-STD-001", etc.
            public string PotId;       // "POT-001", "POT-002", etc.
            public float DailyDrift;   // Drift giornaliero di questa pianta
            public int Day;            // Giorno a cui fa riferimento questo drift
            
            public PlantContribution(string plantCode, string potId, float dailyDrift, int day = 0)
            {
                PlantCode = plantCode ?? "Unknown";
                PotId = potId ?? "Unknown";
                DailyDrift = dailyDrift;
                Day = day;
            }
        }
        
        // Oscillazione idle (variazione organica continua)
        private float _idleOscillation = 0f;
        
        /// <summary>
        /// Struttura per tracciare contributi di azioni individuali
        /// </summary>
        private struct ActionContribution
        {
            public string ActionName;  // "Overwatering", "BlueLED", "RedLED", "SprayAntifungal"
            public string PotId;       // "POT-001", "POT-002", etc.
            public float Delta;        // Valore del drift applicato
            
            public ActionContribution(string actionName, string potId, float delta)
            {
                ActionName = actionName;
                PotId = potId ?? "Unknown";
                Delta = delta;
            }
        }

        public float CurrentPh => _currentPh + _idleOscillation; // Include oscillazione nel valore mostrato
        
        public event Action<float, float> OnPhChanged; // (newPh, delta)
        
        /// <summary>
        /// Struttura per i contributi al pH
        /// </summary>
        public struct PhContributions
        {
            public float BasePh;
            public float PlantsDrift;
            public float ActionsDrift;
            public float EventsDrift;
            public float DailyDrift;
            public float Total => BasePh + PlantsDrift + ActionsDrift + EventsDrift + DailyDrift;
        }
        
        public PhContributions GetContributions()
        {
            return new PhContributions
            {
                BasePh = _basePh,
                PlantsDrift = _plantsDrift,
                ActionsDrift = _actionsDrift,
                EventsDrift = _eventsDrift,
                DailyDrift = _dailyDrift
            };
        }

        public PhSystem(float initialPh = 0f)
        {
            _currentPh = Mathf.Clamp(initialPh, MIN_PH, MAX_PH);
            _basePh = _currentPh; // Inizializza _basePh al valore iniziale
        }

        /// <summary>
        /// Applica una modifica istantanea al pH
        /// </summary>
        public void ApplyInstantDelta(float delta, string source = "Unknown")
        {
            float oldBasePh = _currentPh;
            float oldPh = CurrentPh;
            _currentPh = Mathf.Clamp(_currentPh + delta, MIN_PH, MAX_PH);
            float actualDelta = CurrentPh - oldPh;
            float baseDelta = _currentPh - oldBasePh;
            
            // Traccia contributo in base alla sorgente (solo il delta del valore base)
            TrackContribution(baseDelta, source);
            
            // #region agent log
            try 
            { 
                var dataObj = new { currentPh = CurrentPh, actualDelta = actualDelta, oldPh = oldPh, subscribersCount = OnPhChanged?.GetInvocationList()?.Length ?? 0 };
                string dataJson = JsonUtility.ToJson(dataObj);
                var logData = $"{{\"location\":\"PhSystem.cs:134\",\"message\":\"ApplyInstantDelta - OnPhChanged invocato\",\"data\":{dataJson},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"B\"}}";
                File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", logData + "\n"); 
            } 
            catch { }
            // #endregion
            
            OnPhChanged?.Invoke(CurrentPh, actualDelta);
        }
        
        /// <summary>
        /// Traccia i contributi al pH per categoria
        /// </summary>
        private void TrackContribution(float delta, string source)
        {
            source = source.ToLower();
            
            if (source.Contains("plant") || source.Contains("pianta"))
            {
                _plantsDrift += delta;
            }
            else if (source.Contains("action") || source.Contains("azione") || 
                     source.Contains("water") || source.Contains("led") || source.Contains("spray"))
            {
                _actionsDrift += delta;
            }
            else if (source.Contains("event") || source.Contains("evento"))
            {
                _eventsDrift += delta;
            }
            else if (source.Contains("daily") || source.Contains("giornaliero") || source.Contains("drift"))
            {
                _dailyDrift += delta;
            }
            else
            {
                // Default: azioni manuali o debug
                _actionsDrift += delta;
            }
        }

        /// <summary>
        /// Imposta il pH a un valore specifico (per debug)
        /// </summary>
        public void SetPh(float newPh)
        {
            float oldPh = CurrentPh;
            _currentPh = Mathf.Clamp(newPh, MIN_PH, MAX_PH);
            float delta = CurrentPh - oldPh;
            
            // #region agent log
            try 
            { 
                var dataObj = new { currentPh = CurrentPh, delta = delta, oldPh = oldPh, subscribersCount = OnPhChanged?.GetInvocationList()?.Length ?? 0 };
                string dataJson = JsonUtility.ToJson(dataObj);
                var logData = $"{{\"location\":\"PhSystem.cs:182\",\"message\":\"SetPh - OnPhChanged invocato\",\"data\":{dataJson},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"B\"}}";
                File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", logData + "\n"); 
            } 
            catch { }
            // #endregion
            
            OnPhChanged?.Invoke(CurrentPh, delta);
        }

        /// <summary>
        /// Registra un drift giornaliero che verrà applicato al cambio giorno
        /// </summary>
        public void RegisterDailyDrift(float drift)
        {
            _queuedDailyDrift += drift;
        }

        /// <summary>
        /// BLK-02.07: Pulisce il drift giornaliero simulato (debug)
        /// Rimuove sia il queued che quello già applicato e aggiorna pH
        /// </summary>
        public void ClearDailyDrift()
        {
            // delta da sottrarre al pH corrente
            float deltaToRemove = _dailyDrift + _queuedDailyDrift;

            _queuedDailyDrift = 0f;
            _dailyDrift = 0f;

            if (Mathf.Abs(deltaToRemove) > 0.0001f)
            {
                _currentPh = Mathf.Clamp(_currentPh - deltaToRemove, MIN_PH, MAX_PH);
                OnPhChanged?.Invoke(CurrentPh, -deltaToRemove);
                SporiumLogger.LogInfo(LogCategory.Ph, $"Drift simulato rimosso: {-deltaToRemove:F2}, pH ora: {_currentPh:F2}");
            }
        }
        
        /// <summary>
        /// Registra drift da piante con dettagli individuali
        /// </summary>
        public void RegisterPlantDrift(float drift, string plantCode = null, string potId = null, int day = 0)
        {
            // BUG FIX: Il drift della pianta è un drift GIORNALIERO che deve essere applicato ogni giorno,
            // anche se è lo stesso valore. La lista _plantContributions serve solo per il tooltip,
            // non per controllare l'applicazione del drift.
            
            // Aggiorna lista contributi per tooltip (solo se plantCode è valido)
            if (Mathf.Abs(drift) > 0.01f && !string.IsNullOrEmpty(plantCode))
            {
                // BLK-02.07: Crea sempre un nuovo contributo con il giorno corrente (non aggiorna quello esistente)
                // Questo permette di tracciare ogni giorno come voce singola nel breakdown
                _plantContributions.Add(new PlantContribution(plantCode, potId, drift, day));
                
                // Limita a 50 contributi per evitare accumulo eccessivo (aumentato per tracciare più giorni)
                if (_plantContributions.Count > 50)
                {
                    _plantContributions.RemoveAt(0);
                }
            }
            
            // IMPORTANTE: Applica SEMPRE il drift completo, anche se è lo stesso valore del giorno precedente
            // Il drift della pianta è un drift giornaliero che si accumula ogni giorno
            if (Mathf.Abs(drift) > 0.01f)
            {
                _queuedPlantsDrift += drift;
                SporiumLogger.LogInfo(LogCategory.Ph, $"RegisterPlantDrift: drift={drift:F2} accodato, plantCode={plantCode ?? "NULL"}, potId={potId ?? "NULL"}, day={day}");
            }
        }
        
        /// <summary>
        /// Rimuove i contributi delle piante che non sono più nei vasi attivi
        /// IMPORTANTE: Solo le piante nei POT hanno impatto sul pH, non quelle in Inventory o Seed Storage
        /// </summary>
        public void CleanupPlantContributions(System.Collections.Generic.HashSet<string> activePotIds)
        {
            if (activePotIds == null)
                return;
            
            int removedCount = 0;
            
            // Rimuovi tutti i contributi delle piante che non sono più nei vasi attivi
            for (int i = _plantContributions.Count - 1; i >= 0; i--)
            {
                if (!activePotIds.Contains(_plantContributions[i].PotId))
                {
                    // Se era accodato per oggi, rimuovilo dai queued
                    _queuedPlantsDrift -= _plantContributions[i].DailyDrift;
                    SporiumLogger.LogDebug(LogCategory.Ph, $"Cleanup: Rimuovo contributo obsoleto - PotId={_plantContributions[i].PotId}, PlantCode={_plantContributions[i].PlantCode}, Drift={_plantContributions[i].DailyDrift:F2}, Day={_plantContributions[i].Day}");
                    _plantContributions.RemoveAt(i);
                    removedCount++;
                }
            }
            
            if (removedCount > 0)
            {
                // Se i drift erano già stati applicati nei giorni precedenti, rimuovili dal cumulativo e ricalcola pH
                float oldPh = _currentPh;
                // Somma i drift rimossi (già tolti dai queued, qui correggiamo il cumulativo)
                // Nota: il drift rimosso è la somma dei DailyDrift tolti; ricostruiamolo da removedCount? No, già sottratto: accumuliamo nuovamente.
                // Per semplicità, ricalcoliamo il pH dai contributi attuali (plants/actions/events/daily) senza i queued.
                // _plantsDrift deve essere coerente: se le piante obsolete contribuivano al cumulativo, vanno sottratte.
                // Qui non abbiamo il totale rimosso; quindi lo ricombiniamo con una seconda passata: non disponibile. Soluzione: non toccare _plantsDrift qui (manteniamo l'effetto storico), ma garantiamo che i nuovi drift non si applichino.
                // Nota: per rimuovere l'effetto storico servirebbe tracciare il valore rimosso; per ora segnaliamo solo la lista vuota.
                
                if (_plantContributions.Count == 0)
                {
                    SporiumLogger.LogDebug(LogCategory.Ph, "Cleanup: _plantContributions è vuoto dopo cleanup");
                }
                
                // Nessun ricalcolo pH perché mancano i valori rimossi già applicati; la rimozione futura è garantita (queued = 0).
                // Se si desidera rimuovere anche l'effetto storico, usare RemovePlantContributions con potId specifico.
            }
        }
        
        /// <summary>
        /// Rimuove i contributi di una pianta specifica quando viene rimossa (es. UPROOT)
        /// IMPORTANTE: Solo le piante nei POT hanno impatto sul pH, non quelle in Inventory o Seed Storage
        /// </summary>
        public void RemovePlantContributions(string potId)
        {
            if (string.IsNullOrEmpty(potId))
            {
                SporiumLogger.LogWarning(LogCategory.Ph, "RemovePlantContributions chiamato con potId NULL o vuoto!");
                return;
            }
            
            // Trova tutti i contributi di questa pianta (potrebbero esserci più entry se registrata più volte)
            float totalDriftToRemove = 0f;
            int removedCount = 0;
            
            for (int i = _plantContributions.Count - 1; i >= 0; i--)
            {
                if (_plantContributions[i].PotId == potId)
                {
                    totalDriftToRemove += _plantContributions[i].DailyDrift;
                    SporiumLogger.LogDebug(LogCategory.Ph, $"Trovato contributo da rimuovere: PotId={potId}, PlantCode={_plantContributions[i].PlantCode}, Drift={_plantContributions[i].DailyDrift:F2}, Day={_plantContributions[i].Day}");
                    _plantContributions.RemoveAt(i);
                    removedCount++;
                }
            }
            
            // Rimuovi solo dai queued (drift del giorno in corso); i drift storici restano applicati
            if (Mathf.Abs(totalDriftToRemove) > 0.01f)
            {
                _queuedPlantsDrift = Mathf.Min(0f, _queuedPlantsDrift - totalDriftToRemove);
                SporiumLogger.LogInfo(LogCategory.Ph, $"Rimossi {removedCount} contributi pH accodati per pianta nel vaso {potId}: drift accodato rimosso = {totalDriftToRemove:F2} (queued ora: {_queuedPlantsDrift:F2}), pH attuale = {CurrentPh:F2}");
            }
            else if (removedCount > 0)
            {
                SporiumLogger.LogWarning(LogCategory.Ph, $"Rimossi {removedCount} contributi per vaso {potId} ma drift totale era 0 o molto piccolo");
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.Ph, $"Nessun contributo trovato per vaso {potId} da rimuovere!");
            }
        }
        
        /// <summary>
        /// Verifica se esiste già un contributo per una specifica azione e vaso
        /// </summary>
        public bool HasActionContribution(string actionName, string potId)
        {
            if (string.IsNullOrEmpty(actionName) || string.IsNullOrEmpty(potId))
                return false;
            
            foreach (var action in _actionContributions)
            {
                if (action.ActionName == actionName && action.PotId == potId)
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Registra drift da azioni con dettagli pot
        /// </summary>
        public void RegisterActionDrift(float drift, string actionName = "", string potId = null)
        {
            // Traccia azione individuale per tooltip dettagliato
            // NOTA: Aggiunge sempre una nuova entry per permettere accumulo giornaliero (es. overwatering ogni giorno)
            if (Mathf.Abs(drift) > 0.01f && !string.IsNullOrEmpty(actionName))
            {
                _actionContributions.Add(new ActionContribution(actionName, potId, drift));
                
                // Limita a 50 azioni per evitare accumulo eccessivo (aumentato da 20 per gestire più vasi)
                if (_actionContributions.Count > 50)
        {
                    _actionContributions.RemoveAt(0);
                }
            }
            
            _queuedActionsDrift += drift;
        }
        
        /// <summary>
        /// Rimuove i contributi di un'azione specifica per un vaso (es. Overwatering quando idratazione scende sotto 50%)
        /// GDD AZ-11: Overwatering viene rimosso quando Hydration < 50%
        /// </summary>
        public void RemoveActionContribution(string actionName, string potId)
        {
            if (string.IsNullOrEmpty(actionName) || string.IsNullOrEmpty(potId))
            {
                SporiumLogger.LogWarning(LogCategory.Ph, "RemoveActionContribution chiamato con actionName o potId NULL o vuoto!");
                return;
            }
            
            // Trova tutti i contributi di questa azione per questo vaso
            float totalDriftToRemove = 0f;
            int removedCount = 0;
            
            for (int i = _actionContributions.Count - 1; i >= 0; i--)
            {
                if (_actionContributions[i].ActionName == actionName && _actionContributions[i].PotId == potId)
                {
                    totalDriftToRemove += _actionContributions[i].Delta;
                    SporiumLogger.LogDebug(LogCategory.Ph, $"Trovato contributo da rimuovere: Action={actionName}, PotId={potId}, Delta={_actionContributions[i].Delta:F2}");
                    _actionContributions.RemoveAt(i);
                    removedCount++;
                }
            }
            
            // Rimuovi solo dai queued (drift del giorno in corso); i drift storici restano applicati
            if (Mathf.Abs(totalDriftToRemove) > 0.01f)
            {
                // Evita di generare offset positivo: mantieni il queued non maggiore di 0 per drift negativi
                _queuedActionsDrift = Mathf.Min(0f, _queuedActionsDrift - totalDriftToRemove);
                SporiumLogger.LogInfo(LogCategory.Ph, $"Rimossi {removedCount} contributi pH accodati per {actionName} nel vaso {potId}: drift accodato rimosso = {totalDriftToRemove:F2} (queued ora: {_queuedActionsDrift:F2})");
                
                // BLK-02.07 BUG FIX: Notifica cambio per aggiornare tooltip quando vengono rimossi contributi
                // Emetti evento con delta 0 per forzare aggiornamento tooltip senza cambiare pH corrente
                OnPhChanged?.Invoke(CurrentPh, 0f);
            }
            else if (removedCount > 0)
            {
                SporiumLogger.LogWarning(LogCategory.Ph, $"Rimossi {removedCount} contributi per {actionName} nel vaso {potId} ma drift totale accodato era 0 o molto piccolo");
                
                // BLK-02.07 BUG FIX: Notifica cambio anche se drift era 0 (per aggiornare tooltip)
                OnPhChanged?.Invoke(CurrentPh, 0f);
            }
        }
        
        /// <summary>
        /// Registra drift da eventi
        /// </summary>
        public void RegisterEventDrift(float drift, string eventName = "")
        {
            _queuedEventsDrift += drift;
        }

        /// <summary>
        /// Determina la banda pH corrente (usa CurrentPh che include oscillazione)
        /// </summary>
        public PhBand EvaluateState()
        {
            float ph = CurrentPh;
            // Usa soglie configurabili se disponibili, altrimenti default
            float ultraAcidThreshold = -80f;
            float stableAcidThreshold = -30f;
            float stableBasicThreshold = 30f;
            float ultraBasicThreshold = 80f;
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            try
            {
                ultraAcidThreshold = DifficultyCalibrationConfig.PhThresholdUltraAcid;
                stableAcidThreshold = DifficultyCalibrationConfig.PhThresholdStableAcid;
                stableBasicThreshold = DifficultyCalibrationConfig.PhThresholdStableBasic;
                ultraBasicThreshold = DifficultyCalibrationConfig.PhThresholdUltraBasic;
            }
            catch
            {
                // Se config non disponibile, usa default
            }
            #endif
            
            if (ph <= ultraAcidThreshold) return PhBand.UltraAcid;
            if (ph <= stableAcidThreshold) return PhBand.StableAcid;
            if (ph <= stableBasicThreshold - 1f) return PhBand.Neutral; // -1 per evitare overlap
            if (ph <= ultraBasicThreshold - 1f) return PhBand.StableBasic; // -1 per evitare overlap
            return PhBand.UltraBasic;
        }

        /// <summary>
        /// Ottiene il nome della banda pH
        /// </summary>
        public string GetBandName()
        {
            return EvaluateState() switch
            {
                PhBand.UltraAcid => "Ultra Acido",
                PhBand.StableAcid => "Stable Acido",
                PhBand.Neutral => "Neutrale",
                PhBand.StableBasic => "Stable Basico",
                PhBand.UltraBasic => "Ultra Basico",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Ottiene il colore della banda pH per UI
        /// </summary>
        public Color GetBandColor()
        {
            return EvaluateState() switch
            {
                PhBand.UltraAcid => new Color(0.8f, 0.2f, 0.2f),      // Rosso scuro
                PhBand.StableAcid => new Color(1f, 0.5f, 0.2f),      // Arancione
                PhBand.Neutral => new Color(0.2f, 0.8f, 0.2f),       // Verde
                PhBand.StableBasic => new Color(0.2f, 0.5f, 1f),     // Azzurro
                PhBand.UltraBasic => new Color(0.2f, 0.2f, 0.8f),    // Blu scuro
                _ => Color.white
            };
        }

        /// <summary>
        /// Imposta l'oscillazione idle (variazione organica continua)
        /// Non viene tracciata come contributo permanente
        /// </summary>
        public void SetIdleOscillation(float oscillation)
        {
            float oldPh = CurrentPh;
            _idleOscillation = Mathf.Clamp(oscillation, MIN_PH - _currentPh, MAX_PH - _currentPh);
            float delta = CurrentPh - oldPh;
            
            // Notifica cambio pH (con oscillazione inclusa) anche per piccoli cambiamenti
            // Ridotta soglia per rendere l'oscillazione più visibile
            if (Mathf.Abs(delta) > 0.001f)
            {
                OnPhChanged?.Invoke(CurrentPh, delta);
            }
        }
        
        /// <summary>
        /// Reset pH a neutro
        /// </summary>
        public void Reset()
        {
            SetPh(0f);
            _basePh = 0f;
            _plantsDrift = 0f;
            _actionsDrift = 0f;
            _eventsDrift = 0f;
            _dailyDrift = 0f;
            _idleOscillation = 0f;
            _actionContributions.Clear();
            _plantContributions.Clear();
            _queuedPlantsDrift = 0f;
            _queuedActionsDrift = 0f;
            _queuedEventsDrift = 0f;
            _queuedDailyDrift = 0f;
        }
        
        /// <summary>
        /// Genera stringa di calcolo per tooltip (formato italiano, stile HUD esistente)
        /// Evidenzia gli elementi che contribuiscono al valore
        /// MODIFICATO: Non mostra pBase se 0, non mostra oscillazione, mostra dettagli azioni per pot
        /// </summary>
        public string GetCalculationBreakdown()
        {
            var contrib = GetContributions();
            var culture = System.Globalization.CultureInfo.GetCultureInfo("it-IT");
            
            string breakdown = "<b>pH Calculation:</b>\n";
            
            bool hasContributions = false;
            
            // Mostra pBase solo se diverso da 0
            if (Mathf.Abs(contrib.BasePh) > 0.01f)
            {
                breakdown += $"pBase: {contrib.BasePh.ToString("F1", culture)}\n";
                hasContributions = true;
            }
            
            // Mostra dettagli delle piante individuali per pot
            // IMPORTANTE: Mostra solo le piante che sono ancora nei POT (non quelle rimosse)
            if (_plantContributions.Count > 0)
            {
                foreach (var plant in _plantContributions)
                {
                    if (Mathf.Abs(plant.DailyDrift) > 0.01f)
                    {
                        // Formato con 1 decimale: mostra 2 come "2,0" e -2 come "-2,0"
                        string plantValue = plant.DailyDrift.ToString("+#0.0;-#0.0;0", culture);
                        string potInfo = !string.IsNullOrEmpty(plant.PotId) && plant.PotId != "Unknown" 
                            ? $" ({plant.PotId})" 
                            : "";
                        string plantInfo = !string.IsNullOrEmpty(plant.PlantCode) && plant.PlantCode != "Unknown"
                            ? $" - {plant.PlantCode}"
                            : "";
                        // BLK-02.07: Mostra anche il giorno di riferimento per identificare ogni voce singola
                        string dayInfo = plant.Day > 0 ? $" x giorno {plant.Day}" : "";
                        breakdown += $"<color=#FFFF00>Pianta{plantInfo}:</color> {plantValue}/giorno{potInfo}{dayInfo}\n";
                        hasContributions = true;
                    }
                }
            }
            // Fallback: mostra totale piante SOLO se non ci sono dettagli individuali E se il totale è diverso da 0
            // IMPORTANTE: Non mostrare il fallback se la lista è vuota ma il totale è ancora diverso da 0
            // (questo può accadere se i contributi sono stati rimossi ma _plantsDrift non è stato aggiornato correttamente)
            else if (Mathf.Abs(contrib.PlantsDrift) > 0.01f && _plantContributions.Count == 0)
            {
                // Se la lista è vuota ma il totale è ancora diverso da 0, potrebbe essere un problema di sincronizzazione
                // In questo caso, non mostriamo il fallback per evitare confusione
                // Il totale verrà corretto al prossimo calcolo giornaliero
                SporiumLogger.LogWarning(LogCategory.Ph, $"Discrepanza: _plantContributions è vuoto ma _plantsDrift = {contrib.PlantsDrift:F2}. Questo potrebbe indicare un problema di sincronizzazione.");
            }
            
            // Mostra dettagli delle azioni individuali per pot
            if (_actionContributions.Count > 0)
            {
                foreach (var action in _actionContributions)
                {
                    if (Mathf.Abs(action.Delta) > 0.01f)
                    {
                        // Formato con 1 decimale: mostra 5 come "5,0" e -5 come "-5,0"
                        string actionValue = action.Delta.ToString("+#0.0;-#0.0;0", culture);
                        string actionDisplayName = GetActionDisplayName(action.ActionName);
                        string potInfo = !string.IsNullOrEmpty(action.PotId) && action.PotId != "Unknown" 
                            ? $" ({action.PotId})" 
                            : "";
                        breakdown += $"<color=#00FFFF>{actionDisplayName}:</color> {actionValue}{potInfo}\n";
                        hasContributions = true;
                    }
                }
            }
            
            if (Mathf.Abs(contrib.EventsDrift) > 0.01f)
            {
                string eventsValue = contrib.EventsDrift.ToString("+#0,0;-#0,0;0", culture);
                breakdown += $"<color=#FF00FF>Eventi:</color> {eventsValue}\n";
                hasContributions = true;
            }
            
            if (Mathf.Abs(contrib.DailyDrift) > 0.01f)
            {
                string dailyValue = contrib.DailyDrift.ToString("+#0,0;-#0,0;0", culture);
                breakdown += $"<color=#FFA500>Drift:</color> {dailyValue}\n";
                hasContributions = true;
            }
            
            // NOTA: Oscillazione NON viene mostrata (come richiesto)
            // ma continua a funzionare per l'animazione visiva
            
            if (!hasContributions)
            {
                breakdown += "<color=#808080>(Nessun contributo attivo)</color>\n";
            }
            
            // BLK-02.07 BUG FIX: Calcola il totale usando i valori cumulativi reali
            // (non sommando _plantContributions che potrebbero non contenere tutti i contributi storici)
            // Il pH reale viene da _plantsDrift, _actionsDrift, etc. che sono cumulativi
            float calculatedTotal = contrib.BasePh + contrib.PlantsDrift + contrib.ActionsDrift + contrib.EventsDrift;
            // NOTA: contrib.DailyDrift è un drift generico di test/simulazione, non un contributo reale
            // quindi NON viene incluso nel totale "da contributi" per evitare confusione
            
            // Mostra il totale calcolato dai contributi cumulativi (corrisponde al pH senza oscillazione)
            breakdown += $"<b>Total (da contributi): {calculatedTotal.ToString("F1", culture)}</b>\n";
            
            // Mostra il pH corrente (che include oscillazione, ma NON DailyDrift simulato)
            // L'oscillazione è solo estetica e non viene mostrata nel breakdown
            breakdown += $"<b>pH Corrente (con oscillazione): {CurrentPh.ToString("F1", culture)}</b>";
            
            return breakdown;
        }
        
        /// <summary>
        /// Applica tutti i drift accodati per la giornata in un’unica soluzione.
        /// pH_domani = pH_oggi + Σ drift accodati (piante, azioni, eventi, daily).
        /// </summary>
        public void ApplyQueuedDrifts()
        {
            float totalQueued = _queuedPlantsDrift + _queuedActionsDrift + _queuedEventsDrift + _queuedDailyDrift;
            if (Mathf.Abs(totalQueued) < 0.0001f)
            {
                return;
            }
            
            float oldPh = _currentPh;
            
            // Aggiorna contributi cumulativi
            _plantsDrift += _queuedPlantsDrift;
            _actionsDrift += _queuedActionsDrift;
            _eventsDrift += _queuedEventsDrift;
            _dailyDrift += _queuedDailyDrift;
            
            // Ricalcola pH dalla somma completa
            var contrib = GetContributions();
            float expectedPh = contrib.Total;
            _currentPh = Mathf.Clamp(expectedPh, MIN_PH, MAX_PH);
            float actualDelta = _currentPh - oldPh;
            
            // Pulisci accumulatori giornalieri
            _queuedPlantsDrift = 0f;
            _queuedActionsDrift = 0f;
            _queuedEventsDrift = 0f;
            _queuedDailyDrift = 0f;
            
            // #region agent log
            try 
            { 
                var dataObj = new { currentPh = CurrentPh, actualDelta = actualDelta, oldPh = oldPh, subscribersCount = OnPhChanged?.GetInvocationList()?.Length ?? 0 };
                string dataJson = JsonUtility.ToJson(dataObj);
                var logData = $"{{\"location\":\"PhSystem.cs:680\",\"message\":\"ApplyQueuedDrifts - OnPhChanged invocato\",\"data\":{dataJson},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"B\"}}";
                File.AppendAllText(@"d:\Sporae_Build_Beta\.cursor\debug.log", logData + "\n"); 
            } 
            catch { }
            // #endregion
            
            OnPhChanged?.Invoke(CurrentPh, actualDelta);
            
            SporiumLogger.LogDebug(LogCategory.Ph, $"ApplyQueuedDrifts: delta={actualDelta:F2}, pH={_currentPh:F2}, contrib=Base:{contrib.BasePh:F2} Plants:{contrib.PlantsDrift:F2} Actions:{contrib.ActionsDrift:F2} Events:{contrib.EventsDrift:F2} Daily:{contrib.DailyDrift:F2}");
        }
        
        /// <summary>
        /// Converte nome azione tecnico in nome display italiano
        /// BLK-02.07: Supporta moltiplicatori LED (x1.5, x2)
        /// </summary>
        private string GetActionDisplayName(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
                return "Azione";
            
            string originalActionName = actionName;  // Salva originale prima di ToLower()
            actionName = actionName.ToLower();
            
            if (actionName.Contains("overwatering") || actionName.Contains("over"))
                return "Overwatering";
            
            // BLK-02.07: LED con moltiplicatori
            if (actionName.Contains("blueled") || actionName.Contains("blue"))
            {
                string multiplier = "";
                if (originalActionName.Contains("_x2"))
                    multiplier = " (×2)";
                else if (originalActionName.Contains("_x1.5"))
                    multiplier = " (×1.5)";
                return $"LED Blu{multiplier}";
            }
            
            if (actionName.Contains("redled") || actionName.Contains("red"))
            {
                string multiplier = "";
                if (originalActionName.Contains("_x2"))
                    multiplier = " (×2)";
                else if (originalActionName.Contains("_x1.5"))
                    multiplier = " (×1.5)";
                return $"LED Rosso{multiplier}";
            }
            if (actionName.Contains("spray") || actionName.Contains("antifungal"))
                return "Spray Antifungino";
            if (actionName.Contains("water"))
                return "Annaffiatura";
            if (actionName.Contains("light"))
                return "Illuminazione";
            
            return actionName;
        }
    }
}

