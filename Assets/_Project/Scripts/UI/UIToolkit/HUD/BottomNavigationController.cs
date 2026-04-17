using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.HUD.Components;

namespace Sporae.UI.UIToolkit.HUD
{
    /// <summary>
    /// Controller per la BottomNavigation HUD con room buttons.
    /// </summary>
    public class BottomNavigationController : MonoBehaviour
    {
        [Header("UI Toolkit References")]
        [SerializeField] private UIDocument _uiDocument;
        
        [Header("Room Configuration")]
        [SerializeField] private string _activeRoom = "dome"; // Default: DOME
        [SerializeField] private List<string> _lockedRooms = new List<string> { "restricted1", "restricted2" };
        
        [Header("Configuration")]
        [SerializeField] private bool _enableDebugLogs = false;

        [Header("UI Glow Frame")]
        [SerializeField] private Material _glowFrameMaterial;
        [SerializeField] private bool _glowFrameLiveUpdate = true;
        
        // UI Elements
        private VisualElement _root;
        private Dictionary<string, Button> _roomButtons;
        private Dictionary<string, string> _roomIds; // Button name -> Room ID mapping

        private VisualElement _glowFrame;
        private UiGlowFrameGenerator _glowFrameGenerator;
        private Material _glowFrameMaterialRuntime;
        private const string GlowShaderName = "Sporae/UI/GlowFrame";
        
        // Events
        public event Action<string> OnRoomButtonClick;
        
        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            
            // Allineato a TopBar/CompactBottomBar (200): tooltip sopra toast Foundation (150).
            if (_uiDocument != null)
                _uiDocument.sortingOrder = 200;
        }
        
        private void Start()
        {
            InitializeUI();
            InitializeRoomButtons();
            SetActiveRoom(_activeRoom);
        }
        
        private void InitializeUI()
        {
            if (_uiDocument == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "UIDocument non trovato su BottomNavigationController!");
                return;
            }
            
            _root = _uiDocument.rootVisualElement;
            if (_root == null)
            {
                SporiumLogger.LogError(LogCategory.UI, "Root VisualElement non trovato!");
                return;
            }

            _glowFrame = _root.Q<VisualElement>("glow-frame");
            SetupGlowFrame();
        }

        private void SetupGlowFrame()
        {
            if (_glowFrame == null) return;

            if (_glowFrameMaterial == null)
            {
                var shader = Shader.Find(GlowShaderName);
                if (shader != null)
                    _glowFrameMaterial = new Material(shader);
            }

            if (_glowFrameMaterial != null)
            {
                ApplyGlowDefaults(_glowFrameMaterial);
                _glowFrameMaterialRuntime = new Material(_glowFrameMaterial);
                _glowFrameGenerator = new UiGlowFrameGenerator(_glowFrame, _glowFrameMaterialRuntime);
            }

            _glowFrameLiveUpdate = true;

        }

        private static void ApplyGlowDefaults(Material mat)
        {
            // Glow frame should be transparent; bar background is handled by USS.
            var bg = new Color(0f, 0f, 0f, 0f);
            // Border: #7FFF7A @ 60%
            var border = new Color(127f / 255f, 255f / 255f, 122f / 255f, 0.60f);
            // Glow: same green, stronger alpha for bloom
            var glow = new Color(127f / 255f, 255f / 255f, 122f / 255f, 0.90f);

            mat.SetColor("_GradTop", bg);
            mat.SetColor("_GradBottom", bg);
            mat.SetFloat("_GradStrength", 0.0f);
            mat.SetColor("_BorderColor", border);
            mat.SetColor("_GlowColor", glow);
            mat.SetFloat("_BorderThickness", 4.0f);
            mat.SetFloat("_BorderSoftness", 1.0f);
            mat.SetFloat("_GlowSize", 14.0f);
            mat.SetFloat("_GlowIntensity", 1.0f);
            mat.SetFloat("_GlowFalloff", 1.25f);
        }

        private void Update()
        {
            if (_glowFrameLiveUpdate && _glowFrameGenerator != null)
            {
                if (_glowFrameMaterialRuntime != null && _glowFrameMaterial != null)
                    _glowFrameMaterialRuntime.CopyPropertiesFromMaterial(_glowFrameMaterial);
                if (_glowFrameMaterialRuntime != null)
                    _glowFrameMaterialRuntime.SetFloat("_EdgeMode", 1.0f); // top edge only
                _glowFrameGenerator.Render();
            }
        }

        private void OnDisable()
        {
            _glowFrameGenerator?.Dispose();
            _glowFrameGenerator = null;
        }
        
        private void InitializeRoomButtons()
        {
            _roomButtons = new Dictionary<string, Button>();
            _roomIds = new Dictionary<string, string>
            {
                { "btn-visitor", "visitor" },
                { "btn-storage", "storage" },
                { "btn-dome", "dome" },
                { "btn-lab", "lab" },
                { "btn-kitchen", "kitchen" },
                { "btn-dormitory", "dormitory" },
                { "btn-restricted1", "restricted1" },
                { "btn-restricted2", "restricted2" }
            };
            
            foreach (var kvp in _roomIds)
            {
                var button = _root.Q<Button>(kvp.Key);
                if (button != null)
                {
                    _roomButtons[kvp.Value] = button;
                    
                    // Setup click handler
                    string roomId = kvp.Value;
                    button.clicked += () => OnRoomButtonClicked(roomId);
                    
                    // Setup hover effects
                    button.RegisterCallback<MouseEnterEvent>(evt => OnRoomButtonHoverEnter(roomId));
                    button.RegisterCallback<MouseLeaveEvent>(evt => OnRoomButtonHoverLeave(roomId));
                    
                    // Set initial state
                    UpdateButtonState(kvp.Value, roomId);
                }
            }
        }
        
        /// <summary>
        /// Imposta la room attiva e aggiorna gli stati visuali.
        /// </summary>
        public void SetActiveRoom(string roomId)
        {
            _activeRoom = roomId;
            
            // Aggiorna tutti i button states
            foreach (var kvp in _roomIds)
            {
                UpdateButtonState(kvp.Key, kvp.Value);
            }
            
            if (_enableDebugLogs)
                SporiumLogger.LogInfo(LogCategory.UI, $"Active room set to: {roomId}");
        }
        
        /// <summary>
        /// Imposta lo stato locked/unlocked di una room.
        /// </summary>
        public void SetRoomLocked(string roomId, bool locked)
        {
            if (locked && !_lockedRooms.Contains(roomId))
            {
                _lockedRooms.Add(roomId);
            }
            else if (!locked && _lockedRooms.Contains(roomId))
            {
                _lockedRooms.Remove(roomId);
            }
            
            // Trova il button name per questa room
            string buttonName = null;
            foreach (var kvp in _roomIds)
            {
                if (kvp.Value == roomId)
                {
                    buttonName = kvp.Key;
                    break;
                }
            }
            
            if (buttonName != null)
            {
                UpdateButtonState(buttonName, roomId);
            }
        }
        
        private void UpdateButtonState(string buttonName, string roomId)
        {
            var button = _root.Q<Button>(buttonName);
            if (button == null) return;

            // Pulisci eventuali override inline (hover) che altrimenti vincono sul USS.
            // Questo garantisce che SOLO un bottone appaia "active" alla volta.
            ClearInlineVisualOverrides(button);
            
            // Rimuovi tutte le classi di stato
            button.RemoveFromClassList("room-active");
            button.RemoveFromClassList("room-available");
            button.RemoveFromClassList("room-locked");
            
            // Applica classe corretta
            if (_lockedRooms.Contains(roomId))
            {
                button.AddToClassList("room-locked");
                button.SetEnabled(false); // Disabilita interazione
            }
            else if (roomId == _activeRoom)
            {
                button.AddToClassList("room-active");
                button.SetEnabled(true);
            }
            else
            {
                button.AddToClassList("room-available");
                button.SetEnabled(true);
            }
        }

        private static void ClearInlineVisualOverrides(VisualElement el)
        {
            if (el == null) return;

            // Reset colors overridden in hover handlers
            el.style.borderTopColor = StyleKeyword.Null;
            el.style.borderRightColor = StyleKeyword.Null;
            el.style.borderBottomColor = StyleKeyword.Null;
            el.style.borderLeftColor = StyleKeyword.Null;
            el.style.backgroundColor = StyleKeyword.Null;

            // Reset border widths (in caso venissero settate inline da altri handler)
            el.style.borderTopWidth = StyleKeyword.Null;
            el.style.borderRightWidth = StyleKeyword.Null;
            el.style.borderBottomWidth = StyleKeyword.Null;
            el.style.borderLeftWidth = StyleKeyword.Null;
        }
        
        private void OnRoomButtonClicked(string roomId)
        {
            // Non permettere click su room locked
            if (_lockedRooms.Contains(roomId))
            {
                if (_enableDebugLogs)
                    SporiumLogger.LogWarning(LogCategory.UI, $"Room {roomId} is locked!");
                return;
            }
            
            // Aggiorna active room
            SetActiveRoom(roomId);
            
            // Trigger event
            OnRoomButtonClick?.Invoke(roomId);
            
            if (_enableDebugLogs)
                SporiumLogger.LogInfo(LogCategory.UI, $"Room button clicked: {roomId}");
        }
        
        private void OnRoomButtonHoverEnter(string roomId)
        {
            // Solo per room available (non locked, non active)
            if (_lockedRooms.Contains(roomId) || roomId == _activeRoom)
                return;
            
            // Cerca button nel mapping
            Button button = null;
            foreach (var kvp in _roomIds)
            {
                if (kvp.Value == roomId)
                {
                    button = _root.Q<Button>(kvp.Key);
                    break;
                }
            }
            
            if (button == null)
                return;
            
            if (button != null)
            {
                // Hover: border → #7FFF7A 70%, background → #7FFF7A 10%
                button.style.borderTopColor = new StyleColor(new Color(0.498f, 1f, 0.478f, 0.7f));
                button.style.borderRightColor = new StyleColor(new Color(0.498f, 1f, 0.478f, 0.7f));
                button.style.borderBottomColor = new StyleColor(new Color(0.498f, 1f, 0.478f, 0.7f));
                button.style.borderLeftColor = new StyleColor(new Color(0.498f, 1f, 0.478f, 0.7f));
                button.style.backgroundColor = new StyleColor(new Color(0.498f, 1f, 0.478f, 0.1f));
                
                // TODO: Scale 1.05 animation - UI Toolkit non supporta transform direttamente
                // Alternativa: usare width/height o implementare via shader/wrapper element
            }
        }
        
        private void OnRoomButtonHoverLeave(string roomId)
        {
            // Solo per room available
            if (_lockedRooms.Contains(roomId) || roomId == _activeRoom)
                return;
            
            // Cerca button nel mapping
            Button button = null;
            foreach (var kvp in _roomIds)
            {
                if (kvp.Value == roomId)
                {
                    button = _root.Q<Button>(kvp.Key);
                    break;
                }
            }
            
            if (button == null)
                return;
            
            if (button != null)
            {
                // Reset: border → #7FFF7A 40%, background → black 40%
                button.style.borderTopColor = new StyleColor(new Color(0.498f, 1f, 0.478f, 0.4f));
                button.style.borderRightColor = new StyleColor(new Color(0.498f, 1f, 0.478f, 0.4f));
                button.style.borderBottomColor = new StyleColor(new Color(0.498f, 1f, 0.478f, 0.4f));
                button.style.borderLeftColor = new StyleColor(new Color(0.498f, 1f, 0.478f, 0.4f));
                button.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.4f));
                
                // TODO: Reset scale - vedi commento sopra
            }
        }
    }
}

