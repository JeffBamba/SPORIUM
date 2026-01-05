using UnityEngine;
using UnityEngine.UIElements;
using System;
using Sporae.Dome.PotSystem.Growth;
using Sporae.UI.UIToolkit.PlantCard;
using Sporae.UI.UIToolkit.PlantCard.Helpers;

namespace Sporae.UI.UIToolkit.PlantCard.Components
{
    /// <summary>
    /// Componente riutilizzabile per rotary knobs (Irrigation 2-position, Illuminazione 3-position).
    /// Gestisce rotazione, LED center, indicator line, click areas e animazioni.
    /// </summary>
    public class RotaryKnobUI
    {
        public enum KnobType
        {
            Irrigation,    // 2-position: OFF (-45deg) / ON (45deg)
            Illuminazione  // 3-position: BLUE (-45deg) / OFF (0deg) / RED (45deg)
        }
        
        private VisualElement _knobElement;
        private VisualElement _indicatorLine;
        private VisualElement _innerLed;
        private Label _statusLabel;
        private KnobType _type;
        private PlantCardV2Config _config;
        
        // Stato corrente
        private bool _irrigationState = false;  // Per Irrigation
        private LedSystemState _ledState = LedSystemState.Off;  // Per Illuminazione
        
        // Eventi
        public event Action<bool> OnIrrigationStateChanged;
        public event Action<LedSystemState> OnLedStateChanged;
        
        // Click areas
        private VisualElement _clickAreaLeft;
        private VisualElement _clickAreaRight;
        private VisualElement _clickAreaTop;  // Solo per Illuminazione
        
        public RotaryKnobUI(VisualElement knobElement, KnobType type, PlantCardV2Config config)
        {
            _knobElement = knobElement;
            _type = type;
            _config = config;
            
            InitializeElements();
            SetupClickAreas();
        }
        
        private void InitializeElements()
        {
            // Trova elementi interni
            _indicatorLine = _knobElement.Q<VisualElement>("indicator-line");
            _innerLed = _knobElement.Q<VisualElement>("inner-led");
            
            // Trova status label (parent container)
            var container = _knobElement.parent;
            while (container != null && container.name != "irrigation-container" && container.name != "illuminazione-container")
            {
                container = container.parent;
            }
            if (container != null)
            {
                _statusLabel = container.Q<Label>("irrigation-status") ?? container.Q<Label>("illuminazione-status");
            }
        }
        
        private void SetupClickAreas()
        {
            // Trova click areas
            // BUG1 FIX: Per Irrigation, click-area-left si chiama "click-area-off" nell'UXML
            // Per Illuminazione, click-area-left si chiama "click-area-blue"
            if (_type == KnobType.Irrigation)
            {
                // Irrigation: click-area-off (left) e click-area-on (right)
                _clickAreaLeft = _knobElement.Q<VisualElement>("click-area-off");
                _clickAreaRight = _knobElement.Q<VisualElement>("click-area-on");
                _clickAreaTop = null; // Irrigation non ha click-area-top
            }
            else
            {
                // Illuminazione: click-area-blue (left), click-area-off (top), click-area-red (right)
                _clickAreaLeft = _knobElement.Q<VisualElement>("click-area-blue");
                _clickAreaRight = _knobElement.Q<VisualElement>("click-area-red");
                _clickAreaTop = _knobElement.Q<VisualElement>("click-area-off");
            }
            
            // Fallback per compatibilità (cerca anche per nome generico)
            if (_clickAreaLeft == null)
            {
                _clickAreaLeft = _knobElement.Q<VisualElement>("click-area-left");
            }
            if (_clickAreaRight == null)
            {
                _clickAreaRight = _knobElement.Q<VisualElement>("click-area-right");
            }
            if (_clickAreaTop == null && _type == KnobType.Illuminazione)
            {
                _clickAreaTop = _knobElement.Q<VisualElement>("click-area-top");
            }
            
            // BUG1 FIX: Assicura che le click areas possano ricevere click
            if (_clickAreaLeft != null)
            {
                _clickAreaLeft.pickingMode = PickingMode.Position;
                _clickAreaLeft.RegisterCallback<ClickEvent>(OnClickLeft);
            }
            
            if (_clickAreaRight != null)
            {
                _clickAreaRight.pickingMode = PickingMode.Position;
                _clickAreaRight.RegisterCallback<ClickEvent>(OnClickRight);
            }
            
            if (_clickAreaTop != null && _type == KnobType.Illuminazione)
            {
                _clickAreaTop.pickingMode = PickingMode.Position;
                _clickAreaTop.RegisterCallback<ClickEvent>(OnClickTop);
            }
            
        }
        
        private void OnClickLeft(ClickEvent evt)
        {
            evt.StopPropagation();
            
            if (_type == KnobType.Irrigation)
            {
                // Irrigation: Click left = OFF
                SetIrrigationState(false);
            }
            else if (_type == KnobType.Illuminazione)
            {
                // Illuminazione: Click left = BLUE
                SetLedState(LedSystemState.Blue);
            }
        }
        
        private void OnClickRight(ClickEvent evt)
        {
            evt.StopPropagation();
            
            if (_type == KnobType.Irrigation)
            {
                // Irrigation: Click right = ON
                SetIrrigationState(true);
            }
            else if (_type == KnobType.Illuminazione)
            {
                // Illuminazione: Click right = RED
                SetLedState(LedSystemState.Red);
            }
        }
        
        private void OnClickTop(ClickEvent evt)
        {
            evt.StopPropagation();
            
            if (_type == KnobType.Illuminazione)
            {
                // Illuminazione: Click top = OFF
                SetLedState(LedSystemState.Off);
            }
        }
        
        /// <summary>
        /// Imposta stato irrigazione (per Irrigation knob)
        /// </summary>
        public void SetIrrigationState(bool isOn)
        {
            if (_type != KnobType.Irrigation)
            {
                Debug.LogWarning("RotaryKnobUI: SetIrrigationState chiamato su knob non Irrigation");
                return;
            }
            
            _irrigationState = isOn;
            UpdateVisuals();
            OnIrrigationStateChanged?.Invoke(isOn);
        }
        
        /// <summary>
        /// Ottiene stato irrigazione corrente
        /// </summary>
        public bool GetIrrigationState()
        {
            return _irrigationState;
        }
        
        /// <summary>
        /// Imposta stato LED (per Illuminazione knob)
        /// </summary>
        public void SetLedState(LedSystemState state)
        {
            if (_type != KnobType.Illuminazione)
            {
                Debug.LogWarning("RotaryKnobUI: SetLedState chiamato su knob non Illuminazione");
                return;
            }
            
            _ledState = state;
            UpdateVisuals();
            OnLedStateChanged?.Invoke(state);
        }
        
        /// <summary>
        /// Ottiene stato LED corrente
        /// </summary>
        public LedSystemState GetLedState()
        {
            return _ledState;
        }
        
        /// <summary>
        /// Aggiorna visuali del knob (rotazione, LED, status label)
        /// </summary>
        private void UpdateVisuals()
        {
            if (_knobElement == null) return;
            
            if (_type == KnobType.Irrigation)
            {
                // Rotazione: OFF = -45deg, ON = 45deg
                float rotation = _irrigationState ? 45f : -45f;
                _knobElement.style.rotate = new Rotate(rotation);
                
                // LED: OFF = blue, ON = green
                if (_innerLed != null && _config != null)
                {
                    Color ledColor = _irrigationState ? _config.GreenLed : _config.BlueInfo;
                    _innerLed.style.backgroundColor = ledColor;
                }
                
                // Indicator line: OFF = blue, ON = green
                if (_indicatorLine != null && _config != null)
                {
                    Color lineColor = _irrigationState ? _config.GreenLed : _config.BlueInfo;
                    _indicatorLine.style.backgroundColor = lineColor;
                }
                
                // Status label
                if (_statusLabel != null)
                {
                    _statusLabel.text = PlantCardFormatters.FormatIrrigationStatus(_irrigationState);
                    if (_config != null)
                    {
                        Color statusColor = PlantCardColorCalculator.GetIrrigationColor(_irrigationState, _config);
                        _statusLabel.style.color = statusColor;
                    }
                }
            }
            else if (_type == KnobType.Illuminazione)
            {
                // Rotazione: BLUE = -45deg, OFF = 0deg, RED = 45deg
                float rotation = _ledState switch
                {
                    LedSystemState.Blue => -45f,
                    LedSystemState.Red => 45f,
                    _ => 0f
                };
                _knobElement.style.rotate = new Rotate(rotation);
                
                // LED: BLUE = blue, OFF = gray, RED = red
                if (_innerLed != null && _config != null)
                {
                    Color ledColor = PlantCardColorCalculator.GetLedColor(_ledState, _config);
                    _innerLed.style.backgroundColor = ledColor;
                    
                    // Glow solo se attivo
                    if (_ledState != LedSystemState.Off)
                    {
                        // Glow effect via box-shadow (se supportato)
                        // Alternativa: usare opacity animation
                    }
                }
                
                // Indicator line: sempre verde (non cambia)
                if (_indicatorLine != null && _config != null)
                {
                    _indicatorLine.style.backgroundColor = _config.GreenLed;
                }
                
                // Status label
                if (_statusLabel != null)
                {
                    _statusLabel.text = PlantCardFormatters.FormatLedStatus(_ledState);
                    if (_config != null)
                    {
                        Color statusColor = PlantCardColorCalculator.GetLedColor(_ledState, _config);
                        _statusLabel.style.color = statusColor;
                    }
                }
            }
        }
        
        /// <summary>
        /// Abilita/disabilita il knob
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            if (_knobElement != null)
            {
                _knobElement.SetEnabled(enabled);
                _knobElement.style.opacity = enabled ? 1f : 0.5f;
            }
        }
    }
}

