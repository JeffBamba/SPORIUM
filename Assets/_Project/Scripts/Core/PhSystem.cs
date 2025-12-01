using System;
using UnityEngine;

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
            
            public PlantContribution(string plantCode, string potId, float dailyDrift)
            {
                PlantCode = plantCode ?? "Unknown";
                PotId = potId ?? "Unknown";
                DailyDrift = dailyDrift;
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
            
            OnPhChanged?.Invoke(CurrentPh, delta);
        }

        /// <summary>
        /// Registra un drift giornaliero che verrà applicato al cambio giorno
        /// </summary>
        public void RegisterDailyDrift(float drift)
        {
            ApplyInstantDelta(drift, "DailyDrift");
        }
        
        /// <summary>
        /// Registra drift da piante con dettagli individuali
        /// </summary>
        public void RegisterPlantDrift(float drift, string plantCode = null, string potId = null)
        {
            // Traccia pianta individuale per tooltip dettagliato
            if (Mathf.Abs(drift) > 0.01f && !string.IsNullOrEmpty(plantCode))
            {
                // Aggiorna contributo esistente se già presente (stessa pianta stesso pot)
                bool found = false;
                for (int i = 0; i < _plantContributions.Count; i++)
                {
                    var plant = _plantContributions[i];
                    if (plant.PlantCode == plantCode && plant.PotId == potId)
                    {
                        // Aggiorna drift (sostituisce invece di sommare per mostrare drift corrente)
                        _plantContributions[i] = new PlantContribution(plantCode, potId, drift);
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    _plantContributions.Add(new PlantContribution(plantCode, potId, drift));
                    
                    // Limita a 20 piante per evitare accumulo eccessivo
                    if (_plantContributions.Count > 20)
                    {
                        _plantContributions.RemoveAt(0);
                    }
                }
            }
            
            ApplyInstantDelta(drift, "Plants");
        }
        
        /// <summary>
        /// Registra drift da azioni con dettagli pot
        /// </summary>
        public void RegisterActionDrift(float drift, string actionName = "", string potId = null)
        {
            // Traccia azione individuale per tooltip dettagliato
            if (Mathf.Abs(drift) > 0.01f && !string.IsNullOrEmpty(actionName))
            {
                _actionContributions.Add(new ActionContribution(actionName, potId, drift));
                
                // Limita a 20 azioni per evitare accumulo eccessivo
                if (_actionContributions.Count > 20)
        {
                    _actionContributions.RemoveAt(0);
                }
            }
            
            ApplyInstantDelta(drift, $"Action_{actionName}");
        }
        
        /// <summary>
        /// Registra drift da eventi
        /// </summary>
        public void RegisterEventDrift(float drift, string eventName = "")
        {
            ApplyInstantDelta(drift, $"Event_{eventName}");
        }

        /// <summary>
        /// Determina la banda pH corrente (usa CurrentPh che include oscillazione)
        /// </summary>
        public PhBand EvaluateState()
        {
            float ph = CurrentPh;
            if (ph <= -80f) return PhBand.UltraAcid;
            if (ph <= -30f) return PhBand.StableAcid;
            if (ph <= 29f) return PhBand.Neutral;
            if (ph <= 79f) return PhBand.StableBasic;
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
                        breakdown += $"<color=#FFFF00>Pianta{plantInfo}:</color> {plantValue}/giorno{potInfo}\n";
                        hasContributions = true;
                    }
                }
            }
            // Fallback: mostra totale piante se non ci sono dettagli individuali
            else if (Mathf.Abs(contrib.PlantsDrift) > 0.01f)
            {
                // Formato con 1 decimale: mostra 2 come "2,0" e -2 come "-2,0"
                string plantsValue = contrib.PlantsDrift.ToString("+#0.0;-#0.0;0", culture);
                breakdown += $"<color=#FFFF00>Piante:</color> {plantsValue}\n";
                hasContributions = true;
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
            // Fallback: mostra totale azioni se non ci sono dettagli individuali
            else if (Mathf.Abs(contrib.ActionsDrift) > 0.01f)
            {
                string actionsValue = contrib.ActionsDrift.ToString("+#0,0;-#0,0;0", culture);
                breakdown += $"<color=#00FFFF>Azioni:</color> {actionsValue}\n";
                hasContributions = true;
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
            
            breakdown += $"<b>Total: {CurrentPh.ToString("F1", culture)}</b>";
            
            return breakdown;
        }
        
        /// <summary>
        /// Converte nome azione tecnico in nome display italiano
        /// </summary>
        private string GetActionDisplayName(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
                return "Azione";
            
            actionName = actionName.ToLower();
            
            if (actionName.Contains("overwatering") || actionName.Contains("over"))
                return "Overwatering";
            if (actionName.Contains("blueled") || actionName.Contains("blue"))
                return "LED Blu";
            if (actionName.Contains("redled") || actionName.Contains("red"))
                return "LED Rosso";
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

