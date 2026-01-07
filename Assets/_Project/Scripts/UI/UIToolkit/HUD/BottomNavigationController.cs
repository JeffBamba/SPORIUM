using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using Sporae.DevTools;

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
        
        // UI Elements
        private VisualElement _root;
        private Dictionary<string, Button> _roomButtons;
        private Dictionary<string, string> _roomIds; // Button name -> Room ID mapping
        
        // Events
        public event Action<string> OnRoomButtonClick;
        
        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            
            // DEBUG_SAFE_FIX: Imposta sortingOrder per HUD base (sotto PlantCard, sopra background)
            if (_uiDocument != null)
                _uiDocument.sortingOrder = 50;
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

