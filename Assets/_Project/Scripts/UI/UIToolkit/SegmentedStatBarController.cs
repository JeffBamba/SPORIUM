using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

namespace Sporae.UI.UIToolkit
{
    /// <summary>
    /// Controller per barre segmentate (10 segmenti) come la barra Hydration nella reference.
    /// </summary>
    public class SegmentedStatBarController
    {
        private VisualElement _barContainer;
        private Label _valueLabel;
        private string _barType;
        private List<VisualElement> _segments;
        private StatBarThresholds _thresholds;
        private MonoBehaviour _coroutineOwner;
        
        private float _currentValue;
        private float _maxValue;
        
        public float CurrentValue => _currentValue;
        public float MaxValue => _maxValue;
        public float Percentage => _maxValue > 0 ? (_currentValue / _maxValue) * 100f : 0f;
        
        public SegmentedStatBarController(VisualElement barContainer, Label valueLabel, string barType, StatBarThresholds thresholds, MonoBehaviour coroutineOwner)
        {
            _barContainer = barContainer;
            _valueLabel = valueLabel;
            _barType = barType;
            _thresholds = thresholds;
            _coroutineOwner = coroutineOwner;
            _segments = new List<VisualElement>();
            
            // Trova tutti i segmenti (hydration-segment-0 a hydration-segment-9)
            for (int i = 0; i < 10; i++)
            {
                var segment = _barContainer.Q<VisualElement>($"hydration-segment-{i}");
                if (segment != null)
                {
                    _segments.Add(segment);
                }
            }
        }
        
        public void UpdateValues(float current, float max)
        {
            _currentValue = current;
            _maxValue = max;
            
            // Calcola percentuale
            float percentage = max > 0 ? (current / max) * 100f : 0f;
            
            // Aggiorna label con percentuale (come nel prompt Figma)
            _valueLabel.text = $"{Mathf.RoundToInt(percentage)}%";
            
            // Calcola quanti segmenti devono essere riempiti (usando CeilToInt come nel prompt)
            float fillAmount = max > 0 ? (current / max) : 0f;
            int filledSegments = Mathf.CeilToInt(fillAmount * 10f);
            
            // Determina se deve blinkare (range 20-40%)
            bool shouldBlink = (percentage >= 20f && percentage <= 40f);
            
            // Aggiorna segmenti
            for (int i = 0; i < _segments.Count; i++)
            {
                var segment = _segments[i];
                bool isFilled = (i < filledSegments);
                bool isLastFilled = (i == filledSegments - 1);
                
                if (isFilled)
                {
                    segment.AddToClassList("filled");
                    UpdateSegmentColor(segment, i, filledSegments);
                    
                    // Blink solo sull'ultimo segmento riempito quando in range 20-40%
                    if (shouldBlink && isLastFilled)
                    {
                        segment.AddToClassList("segment-blink");
                        if (_coroutineOwner != null && _coroutineOwner.gameObject.activeInHierarchy)
                        {
                            _coroutineOwner.StartCoroutine(BlinkSegment(segment));
                        }
                    }
                    else
                    {
                        segment.RemoveFromClassList("segment-blink");
                    }
                }
                else
                {
                    segment.RemoveFromClassList("filled");
                    segment.RemoveFromClassList("segment-blink");
                    // Background color come nel prompt: Color(0.118f, 0.157f, 0.165f)
                    segment.style.backgroundColor = new StyleColor(new Color(0.118f, 0.157f, 0.165f, 1f));
                    segment.style.borderTopColor = new StyleColor(new Color(0.118f, 0.157f, 0.165f, 0.8f));
                    segment.style.borderRightColor = new StyleColor(new Color(0.118f, 0.157f, 0.165f, 0.8f));
                    segment.style.borderBottomColor = new StyleColor(new Color(0.118f, 0.157f, 0.165f, 0.8f));
                    segment.style.borderLeftColor = new StyleColor(new Color(0.118f, 0.157f, 0.165f, 0.8f));
                }
            }
        }
        
        private void UpdateSegmentColor(VisualElement segment, int segmentIndex, int totalFilled)
        {
            float percentage = Percentage;
            Color fillColor;
            Color borderColor;
            
            switch (_barType)
            {
                case "hydration":
                    // Colori come nel prompt Figma:
                    // Blue per normale (>=40%), Yellow <40%, Red <26%
                    if (percentage >= 40f)
                    {
                        fillColor = new Color(0.365f, 0.714f, 0.890f, 1f); // Blue
                        borderColor = fillColor;
                    }
                    else if (percentage >= 26f)
                    {
                        fillColor = new Color(0.902f, 0.788f, 0.435f, 1f); // Yellow
                        borderColor = fillColor;
                    }
                    else
                    {
                        fillColor = new Color(0.827f, 0.373f, 0.373f, 1f); // Red
                        borderColor = fillColor;
                    }
                    break;
                default:
                    fillColor = new Color(0.365f, 0.714f, 0.890f, 1f); // Blue default
                    borderColor = fillColor;
                    break;
            }
            
            segment.style.backgroundColor = new StyleColor(fillColor);
            segment.style.borderTopColor = new StyleColor(borderColor);
            segment.style.borderRightColor = new StyleColor(borderColor);
            segment.style.borderBottomColor = new StyleColor(borderColor);
            segment.style.borderLeftColor = new StyleColor(borderColor);
        }
        
        /// <summary>
        /// Coroutine per animazione blink dell'ultimo segmento riempito (range 20-40%).
        /// </summary>
        private IEnumerator BlinkSegment(VisualElement segment)
        {
            float elapsed = 0f;
            float duration = 0.8f; // Durata ciclo blink
            
            while (segment != null && segment.ClassListContains("segment-blink"))
            {
                elapsed += Time.deltaTime;
                float t = Mathf.PingPong(elapsed / duration, 1f);
                float opacity = Mathf.Lerp(0.4f, 1.0f, t);
                
                var currentColor = segment.resolvedStyle.backgroundColor;
                segment.style.backgroundColor = new StyleColor(new Color(currentColor.r, currentColor.g, currentColor.b, opacity));
                
                yield return null;
            }
        }
    }
}

