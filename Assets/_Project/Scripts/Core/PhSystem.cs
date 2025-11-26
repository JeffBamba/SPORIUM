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
        
        // Oscillazione idle (variazione organica continua)
        private float _idleOscillation = 0f;

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
        /// Registra drift da piante
        /// </summary>
        public void RegisterPlantDrift(float drift)
        {
            ApplyInstantDelta(drift, "Plants");
        }
        
        /// <summary>
        /// Registra drift da azioni
        /// </summary>
        public void RegisterActionDrift(float drift, string actionName = "")
        {
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
        }
        
        /// <summary>
        /// Genera stringa di calcolo per tooltip (formato italiano, stile HUD esistente)
        /// Evidenzia gli elementi che contribuiscono al valore
        /// </summary>
        public string GetCalculationBreakdown()
        {
            var contrib = GetContributions();
            var culture = System.Globalization.CultureInfo.GetCultureInfo("it-IT");
            
            string breakdown = "<b>pH Calculation:</b>\n";
            
            // Formato italiano con virgole, stile "pBase: 0,0"
            breakdown += $"pBase: {contrib.BasePh.ToString("F1", culture)}\n";
            
            // Evidenzia solo gli elementi che contribuiscono significativamente
            bool hasContributions = false;
            
            if (Mathf.Abs(contrib.PlantsDrift) > 0.01f)
            {
                string plantsValue = contrib.PlantsDrift.ToString("+#0,0;-#0,0;0", culture);
                breakdown += $"<color=yellow>Piante:</color> {plantsValue}\n";
                hasContributions = true;
            }
            
            if (Mathf.Abs(contrib.ActionsDrift) > 0.01f)
            {
                string actionsValue = contrib.ActionsDrift.ToString("+#0,0;-#0,0;0", culture);
                breakdown += $"<color=cyan>Azioni:</color> {actionsValue}\n";
                hasContributions = true;
            }
            
            if (Mathf.Abs(contrib.EventsDrift) > 0.01f)
            {
                string eventsValue = contrib.EventsDrift.ToString("+#0,0;-#0,0;0", culture);
                breakdown += $"<color=magenta>Eventi:</color> {eventsValue}\n";
                hasContributions = true;
            }
            
            if (Mathf.Abs(contrib.DailyDrift) > 0.01f)
            {
                string dailyValue = contrib.DailyDrift.ToString("+#0,0;-#0,0;0", culture);
                breakdown += $"<color=orange>Drift:</color> {dailyValue}\n";
                hasContributions = true;
            }
            
            // Mostra oscillazione idle se presente
            if (Mathf.Abs(_idleOscillation) > 0.01f)
            {
                string oscillationValue = _idleOscillation.ToString("+#0,0;-#0,0;0", culture);
                breakdown += $"<color=gray>Oscillazione:</color> {oscillationValue}\n";
                hasContributions = true;
            }
            
            if (!hasContributions)
            {
                breakdown += "<color=gray>(Nessun contributo attivo)</color>\n";
            }
            
            breakdown += $"<b>Total: {CurrentPh.ToString("F1", culture)}</b>";
            
            return breakdown;
        }
    }
}

