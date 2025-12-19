using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace Sporae.UI.UIToolkit
{
    /// <summary>
    /// Componente riutilizzabile per gestire una barra di stat dinamica con colori e animazioni.
    /// </summary>
    public class StatBarController
    {
        private VisualElement _barFill;
        private Label _valueLabel;
        private string _barType; // "health", "energy", "hydration"
        
        private float _currentValue;
        private float _maxValue;
        private float _displayedValue; // Valore visualizzato (per smooth lerp)
        
        private Coroutine _lerpCoroutine;
        private MonoBehaviour _coroutineOwner;
        
        // Thresholds per cambio colore
        private readonly StatBarThresholds _thresholds;
        
        public float CurrentValue => _currentValue;
        public float MaxValue => _maxValue;
        public float Percentage => _maxValue > 0 ? (_currentValue / _maxValue) * 100f : 0f;
        
        public StatBarController(VisualElement barFill, Label valueLabel, string barType, StatBarThresholds thresholds, MonoBehaviour coroutineOwner)
        {
            _barFill = barFill;
            _valueLabel = valueLabel;
            _barType = barType;
            _thresholds = thresholds;
            _coroutineOwner = coroutineOwner;
            _displayedValue = 0f;
        }
        
        /// <summary>
        /// Aggiorna i valori della barra con smooth lerp.
        /// </summary>
        public void UpdateValues(float current, float max)
        {
            _currentValue = current;
            _maxValue = max;
            
            // Aggiorna label immediatamente
            _valueLabel.text = $"{Mathf.RoundToInt(current)}/{Mathf.RoundToInt(max)}";
            
            // Avvia smooth lerp per il fill
            if (_coroutineOwner != null && _coroutineOwner.gameObject.activeInHierarchy)
            {
                if (_lerpCoroutine != null)
                {
                    _coroutineOwner.StopCoroutine(_lerpCoroutine);
                }
                _lerpCoroutine = _coroutineOwner.StartCoroutine(LerpFillValue());
            }
            else
            {
                // Fallback se coroutine non disponibile
                _displayedValue = current;
                UpdateFillVisual();
            }
            
            // Aggiorna colore basato su thresholds
            UpdateColor();
        }
        
        private IEnumerator LerpFillValue()
        {
            float startValue = _displayedValue;
            float targetValue = _currentValue;
            float elapsed = 0f;
            float duration = 0.3f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _displayedValue = Mathf.Lerp(startValue, targetValue, t);
                UpdateFillVisual();
                yield return null;
            }
            
            _displayedValue = targetValue;
            UpdateFillVisual();
        }
        
        private void UpdateFillVisual()
        {
            if (_barFill == null || _maxValue <= 0) return;
            
            float fillAmount = _displayedValue / _maxValue;
            _barFill.style.width = new StyleLength(new Length(fillAmount * 100f, LengthUnit.Percent));
        }
        
        private void UpdateColor()
        {
            if (_barFill == null) return;
            
            float percentage = Percentage;
            string colorClass = GetColorClass(percentage);
            
            // Rimuovi tutte le classi colore esistenti
            _barFill.RemoveFromClassList("health-fill-green");
            _barFill.RemoveFromClassList("health-fill-yellow");
            _barFill.RemoveFromClassList("health-fill-red");
            _barFill.RemoveFromClassList("energy-fill-blue");
            _barFill.RemoveFromClassList("energy-fill-yellow");
            _barFill.RemoveFromClassList("energy-fill-red");
            _barFill.RemoveFromClassList("hydration-fill-green-normal");
            _barFill.RemoveFromClassList("hydration-fill-green-enhanced");
            _barFill.RemoveFromClassList("hydration-fill-yellow");
            _barFill.RemoveFromClassList("hydration-fill-red");
            
            // Aggiungi la classe corretta
            if (!string.IsNullOrEmpty(colorClass))
            {
                _barFill.AddToClassList(colorClass);
            }
            
            // Gestione glow dinamico (via C# style properties)
            UpdateGlowEffect(percentage);
        }
        
        private string GetColorClass(float percentage)
        {
            switch (_barType)
            {
                case "health":
                    if (percentage >= _thresholds.HealthGreenMin) return "health-fill-green";
                    if (percentage >= _thresholds.HealthYellowMin) return "health-fill-yellow";
                    return "health-fill-red";
                    
                case "energy":
                    if (percentage >= _thresholds.EnergyBlueMin) return "energy-fill-blue";
                    if (percentage >= _thresholds.EnergyYellowMin) return "energy-fill-yellow";
                    return "energy-fill-red";
                    
                case "hydration":
                    if (percentage >= _thresholds.HydrationGreenEnhancedMin) return "hydration-fill-green-enhanced";
                    if (percentage >= _thresholds.HydrationGreenNormalMin) return "hydration-fill-green-normal";
                    if (percentage >= _thresholds.HydrationYellowMin) return "hydration-fill-yellow";
                    return "hydration-fill-red";
                    
                default:
                    return "health-fill-green";
            }
        }
        
        private void UpdateGlowEffect(float percentage)
        {
            if (_barFill == null) return;
            
            Color glowColor = GetGlowColor(percentage);
            float glowIntensity = GetGlowIntensity(percentage);
            
            // UI Toolkit non supporta direttamente box-shadow, quindi usiamo un workaround
            // Impostiamo il colore del bordo per tutti e 4 i lati (UI Toolkit richiede lati separati)
            _barFill.style.borderTopColor = new StyleColor(glowColor);
            _barFill.style.borderRightColor = new StyleColor(glowColor);
            _barFill.style.borderBottomColor = new StyleColor(glowColor);
            _barFill.style.borderLeftColor = new StyleColor(glowColor);
            
            // Per glow più intenso, possiamo aggiungere un elemento overlay in futuro
        }
        
        private Color GetGlowColor(float percentage)
        {
            switch (_barType)
            {
                case "health":
                    if (percentage >= _thresholds.HealthGreenMin) return new Color(0.498f, 1f, 0.478f, 0.4f); // #7FFF7A
                    if (percentage >= _thresholds.HealthYellowMin) return new Color(0.902f, 0.788f, 0.435f, 0.4f); // #E6C96F
                    return new Color(0.827f, 0.373f, 0.373f, 0.4f); // #D35F5F
                    
                case "energy":
                    if (percentage >= _thresholds.EnergyBlueMin) return new Color(0.365f, 0.714f, 0.890f, 0.4f); // #5DB6E3
                    if (percentage >= _thresholds.EnergyYellowMin) return new Color(0.902f, 0.788f, 0.435f, 0.4f); // #E6C96F
                    return new Color(0.827f, 0.373f, 0.373f, 0.4f); // #D35F5F
                    
                case "hydration":
                    if (percentage >= _thresholds.HydrationGreenEnhancedMin) return new Color(0.498f, 1f, 0.478f, 0.6f); // Enhanced glow
                    if (percentage >= _thresholds.HydrationGreenNormalMin) return new Color(0.498f, 1f, 0.478f, 0.4f); // Normal glow
                    if (percentage >= _thresholds.HydrationYellowMin) return new Color(0.902f, 0.788f, 0.435f, 0.4f); // #E6C96F
                    return new Color(0.827f, 0.373f, 0.373f, 0.4f); // #D35F5F
                    
                default:
                    return new Color(0.498f, 1f, 0.478f, 0.4f);
            }
        }
        
        private float GetGlowIntensity(float percentage)
        {
            // Enhanced glow per hydration well-hydrated
            if (_barType == "hydration" && percentage >= _thresholds.HydrationGreenEnhancedMin)
            {
                return 1.5f; // 50% più intenso
            }
            return 1f;
        }
        
        /// <summary>
        /// Abilita/disabilita animazione pulsing per stato critico.
        /// </summary>
        public void SetCriticalPulsing(bool enabled)
        {
            if (_barFill == null) return;
            
            if (enabled)
            {
                _barFill.AddToClassList("hydration-critical");
            }
            else
            {
                _barFill.RemoveFromClassList("hydration-critical");
            }
        }
    }
    
    /// <summary>
    /// Soglie configurabili per cambio colore delle barre.
    /// </summary>
    [System.Serializable]
    public class StatBarThresholds
    {
        // Health thresholds
        public float HealthGreenMin = 70f;
        public float HealthYellowMin = 40f;
        
        // Energy thresholds
        public float EnergyBlueMin = 60f;
        public float EnergyYellowMin = 30f;
        
        // Hydration thresholds
        public float HydrationGreenEnhancedMin = 76f;
        public float HydrationGreenNormalMin = 51f;
        public float HydrationYellowMin = 26f;
    }
}

