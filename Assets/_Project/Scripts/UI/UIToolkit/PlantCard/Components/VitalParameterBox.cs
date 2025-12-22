using UnityEngine;
using UnityEngine.UIElements;
using Sporae.UI.UIToolkit.PlantCard;
using Sporae.UI.UIToolkit.PlantCard.Helpers;

namespace Sporae.UI.UIToolkit.PlantCard.Components
{
    /// <summary>
    /// Componente riutilizzabile per parametri vitali in PlantCard V2.0.
    /// Gestisce header, value grande, segmented bar, range info, e colore dinamico.
    /// </summary>
    public class VitalParameterBox
    {
        public enum ParameterType
        {
            Hydration,      // Celeste fisso
            Fertilizer,     // Dinamico (verde/giallo/rosso)
            LightStress,    // Giallo fisso
            Condizione,     // Dinamico (verde/giallo/rosso)
            MoldRisk,       // Giallo fisso
            PhAffinity      // Giallo fisso
        }
        
        private VisualElement _container;
        private Label _valueLabel;
        private VisualElement _segmentedBar;
        private Label _rangeInfoLabel;
        private ParameterType _type;
        private PlantCardV2Config _config;
        
        // Segmenti della barra
        private VisualElement[] _segments;
        private const int SEGMENT_COUNT = 10;
        
        public VitalParameterBox(VisualElement container, ParameterType type, PlantCardV2Config config)
        {
            _container = container;
            _type = type;
            _config = config;
            
            // CRITICO: NON impostare MAI border-color o background-color sul container
            // Gli stili devono essere gestiti SOLO come inline nel UXML o modificati in UI Builder
            // Questo garantisce che le modifiche in UI Builder abbiano priorità assoluta
            
            InitializeElements();
        }
        
        private void InitializeElements()
        {
            // Trova elementi
            _valueLabel = _container.Q<Label>(className: "vital-value-large") ?? 
                         _container.Q<Label>(className: "vital-value-medium");
            _segmentedBar = _container.Q<VisualElement>(className: "segmented-bar");
            _rangeInfoLabel = _container.Q<Label>(className: "range-info");
            
            // Inizializza segmenti
            if (_segmentedBar != null)
            {
                _segments = new VisualElement[SEGMENT_COUNT];
                for (int i = 0; i < SEGMENT_COUNT; i++)
                {
                    string segmentName = $"segment-{i}";
                    var segment = _segmentedBar.Q<VisualElement>(segmentName);
                    if (segment == null)
                    {
                        // Crea segmento se non esiste
                        segment = new VisualElement();
                        segment.name = segmentName;
                        segment.AddToClassList("bar-segment");
                        _segmentedBar.Add(segment);
                    }
                    _segments[i] = segment;
                }
            }
        }
        
        /// <summary>
        /// Aggiorna il valore del parametro
        /// </summary>
        public void UpdateValue(int value, int max = 100)
        {
            // CRITICO: NON modificare MAI background-color o border-color del container qui
            // Questi devono essere gestiti SOLO come inline nel UXML o modificati in UI Builder
            
            // Aggiorna label valore
            if (_valueLabel != null)
            {
                string formattedValue = _type switch
                {
                    ParameterType.PhAffinity => $"{value}",  // Mostra range pH
                    ParameterType.MoldRisk => $"{value}",  // Mostra level (0-3), non percentuale
                    _ => PlantCardFormatters.FormatPercentage(value, max)  // Mostra percentuale
                };
                _valueLabel.text = formattedValue;
                
                // Applica colore dinamico se necessario
                Color valueColor = GetValueColor(value, max);
                _valueLabel.style.color = valueColor;
                
                // Text shadow glow
                // Nota: text-shadow non supportato direttamente in USS, usare outline o shader
            }
            
            // Aggiorna segmented bar
            UpdateSegmentedBar(value, max);
        }
        
        /// <summary>
        /// Aggiorna range info (es. "45%-55%-65%")
        /// </summary>
        public void UpdateRangeInfo(string rangeText)
        {
            if (_rangeInfoLabel != null)
            {
                _rangeInfoLabel.text = rangeText;
            }
        }
        
        /// <summary>
        /// Aggiorna segmented bar
        /// </summary>
        private void UpdateSegmentedBar(int value, int max)
        {
            if (_segments == null || _segments.Length == 0) return;
            
            int filledCount = PlantCardCalculators.CalculateSegmentedBarFilled(value, max, SEGMENT_COUNT);
            Color barColor = GetValueColor(value, max);
            
            for (int i = 0; i < _segments.Length; i++)
            {
                if (_segments[i] == null) continue;
                
                if (i < filledCount)
                {
                    // Segmento filled
                    _segments[i].style.backgroundColor = barColor;
                    _segments[i].AddToClassList("filled");
                }
                else
                {
                    // Segmento empty
                    Color emptyColor = new Color(barColor.r, barColor.g, barColor.b, 0.2f);
                    _segments[i].style.backgroundColor = emptyColor;
                    _segments[i].RemoveFromClassList("filled");
                }
            }
        }
        
        /// <summary>
        /// Ottiene il colore per il valore basato sul tipo e thresholds
        /// </summary>
        private Color GetValueColor(int value, int max)
        {
            if (_config == null)
                return Color.white;
            
            int percent = max > 0 ? Mathf.RoundToInt((float)value / max * 100f) : 0;
            
            return _type switch
            {
                ParameterType.Hydration => _config.BlueInfo,  // Celeste fisso
                ParameterType.Fertilizer => _config.GetFertilizerColor(percent),  // Dinamico
                ParameterType.LightStress => _config.YellowStandard,  // Giallo fisso
                ParameterType.Condizione => _config.GetConditionColor(percent),  // Dinamico
                ParameterType.MoldRisk => _config.GetMoldRiskColor(value),  // Dinamico basato su level
                ParameterType.PhAffinity => _config.YellowStandard,  // Giallo fisso
                _ => Color.white
            };
        }
        
        /// <summary>
        /// Aggiorna colore del container border (opzionale)
        /// NON chiamare questo metodo se vuoi che gli stili di UI Builder abbiano priorità.
        /// Questo metodo è disabilitato per rispettare gli stili impostati in UI Builder.
        /// </summary>
        public void UpdateBorderColor(Color color)
        {
            // DISABILITATO: Non sovrascrivere border color per rispettare gli stili di UI Builder
            // Gli stili impostati in UI Builder hanno priorità e non devono essere sovrascritti dal codice
            // if (_container != null)
            // {
            //     _container.style.borderTopColor = color;
            //     _container.style.borderRightColor = color;
            //     _container.style.borderBottomColor = color;
            //     _container.style.borderLeftColor = color;
            // }
        }
    }
}

