using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Sporae.UI.UIToolkit.HUD
{
    /// <summary>
    /// Componente riutilizzabile per barre segmentate (es. ACTIONS bar con 4 segmenti).
    /// </summary>
    public class SegmentedBarUI
    {
        private VisualElement _barContainer;
        private List<VisualElement> _segments;
        private Color _fillColor;
        private Color _emptyColor;
        private Color _borderColor;
        private int _segmentCount;
        
        public int Value { get; private set; }
        public int MaxValue { get; private set; }
        
        public SegmentedBarUI(VisualElement barContainer, int segmentCount, Color fillColor, Color emptyColor, Color borderColor)
        {
            _barContainer = barContainer;
            _segmentCount = segmentCount;
            _fillColor = fillColor;
            _emptyColor = emptyColor;
            _borderColor = borderColor;
            _segments = new List<VisualElement>();
            
            // Trova o crea i segmenti
            InitializeSegments();
        }
        
        private void InitializeSegments()
        {
            for (int i = 0; i < _segmentCount; i++)
            {
                var segment = _barContainer.Q<VisualElement>($"segment-{i}");
                if (segment == null)
                {
                    // Crea segmento se non esiste
                    segment = new VisualElement();
                    segment.name = $"segment-{i}";
                    segment.AddToClassList("bar-segment");
                    _barContainer.Add(segment);
                }
                _segments.Add(segment);
            }
        }
        
        /// <summary>
        /// Aggiorna il valore della barra segmentata.
        /// </summary>
        public void UpdateValue(int current, int max)
        {
            Value = current;
            MaxValue = max;
            
            int filledCount = Mathf.CeilToInt((current / (float)max) * _segmentCount);
            filledCount = Mathf.Clamp(filledCount, 0, _segmentCount);
            
            for (int i = 0; i < _segments.Count; i++)
            {
                var segment = _segments[i];
                if (i < filledCount)
                {
                    // Segmento riempito
                    segment.style.backgroundColor = new StyleColor(_fillColor);
                    segment.style.borderTopColor = new StyleColor(_borderColor);
                    segment.style.borderRightColor = new StyleColor(_borderColor);
                    segment.style.borderBottomColor = new StyleColor(_borderColor);
                    segment.style.borderLeftColor = new StyleColor(_borderColor);
                }
                else
                {
                    // Segmento vuoto
                    segment.style.backgroundColor = new StyleColor(_emptyColor);
                    segment.style.borderTopColor = new StyleColor(_borderColor);
                    segment.style.borderRightColor = new StyleColor(_borderColor);
                    segment.style.borderBottomColor = new StyleColor(_borderColor);
                    segment.style.borderLeftColor = new StyleColor(_borderColor);
                }
            }
        }
        
        /// <summary>
        /// Imposta i colori della barra dinamicamente.
        /// </summary>
        public void SetColors(Color fillColor, Color emptyColor, Color borderColor)
        {
            _fillColor = fillColor;
            _emptyColor = emptyColor;
            _borderColor = borderColor;
            UpdateValue(Value, MaxValue); // Riapplica i colori
        }
    }
}

