using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _Project; // Per UINotification
using _Project.Sporae.Core; // Per ServiceContainer
using Sporae.DevTools;

namespace Sporae.Dome.UI
{
    /// <summary>
    /// Dialog modale per conferma potatura con opzione Spray Antifungino (AZ-13)
    /// </summary>
    public class PruningDialog : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Toggle sprayToggle;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        
        public event Action<bool, bool> OnDialogResult; // confirmed, useSpray
        
        private bool _hasSprayAvailable = false; // Memorizza disponibilità STR-004
        
        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(HandleConfirm);
            
            if (cancelButton != null)
                cancelButton.onClick.AddListener(HandleCancel);
            
            // BUG FIX: Aggiungi listener per il toggle per debug e feedback
            if (sprayToggle != null)
            {
                sprayToggle.onValueChanged.AddListener(OnToggleValueChanged);
                
                // BUG FIX: Aggiungi listener per il click sul toggle per mostrare toast
                var toggleButton = sprayToggle.GetComponent<UnityEngine.UI.Button>();
                if (toggleButton == null)
                {
                    // Se non c'è un Button, aggiungiamo un EventTrigger per intercettare il click
                    var eventTrigger = sprayToggle.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                    if (eventTrigger == null)
                    {
                        eventTrigger = sprayToggle.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                    }
                    
                    var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
                    entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
                    entry.callback.AddListener((data) => {
                        if (sprayToggle.isOn && _hasSprayAvailable)
                        {
                            ShowSprayToggleToast("Spray Antifungino selezionato. Verrà utilizzato durante la potatura.");
                        }
                    });
                    eventTrigger.triggers.Add(entry);
                }
            }
            
            // BUG FIX: Non nascondere il dialog all'avvio se viene istanziato dinamicamente
            // Il dialog verrà mostrato/nascosto tramite Show()/Hide()
            // if (dialogPanel != null)
            //     dialogPanel.SetActive(false);
        }
        
        /// <summary>
        /// BUG FIX: Handler per il cambio valore del toggle (per debug)
        /// </summary>
        private void OnToggleValueChanged(bool isOn)
        {
            SporiumLogger.LogDebug(LogCategory.UI, $"Toggle value changed: {isOn} (hasSprayAvailable: {_hasSprayAvailable})");
            
            // BUG FIX: Se l'utente prova a selezionare il toggle ma non c'è STR-004, resetta
            if (isOn && !_hasSprayAvailable)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "Tentativo di selezionare toggle senza STR-004 disponibile. Reset.");
                if (sprayToggle != null)
                {
                    sprayToggle.isOn = false;
                }
            }
            else if (isOn && _hasSprayAvailable)
            {
                // BUG FIX: Mostra toast quando il toggle viene selezionato
                ShowSprayToggleToast("Spray Antifungino selezionato. Verrà utilizzato durante la potatura.");
            }
        }
        
        /// <summary>
        /// BUG FIX: Mostra toast message per feedback toggle
        /// </summary>
        private void ShowSprayToggleToast(string message)
        {
            var uiNotification = UnityEngine.Object.FindObjectOfType<UINotification>();
            if (uiNotification != null)
            {
                var toastManager = ServiceContainer.Instance?.Get<ToastNotificationManager>(suppressWarning: true);
                if (toastManager != null)
                {
                    toastManager.ShowInfo(message, "PRUNE-INFO-001");
                }
                else if (uiNotification != null)
                {
                    uiNotification.ShowNotification(message, 2f, new Color(0.2f, 0.8f, 1f)); // Colore azzurro per info
                }
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "UINotification non trovato! Toast non mostrato.");
            }
        }
        
        /// <summary>
        /// Mostra il dialog e verifica disponibilità STR-004
        /// </summary>
        /// <param name="hasSprayAvailable">Se true, STR-004 è disponibile in inventario</param>
        public void Show(bool hasSprayAvailable)
        {
            // Memorizza disponibilità STR-004
            _hasSprayAvailable = hasSprayAvailable;
            
            if (dialogPanel != null)
                dialogPanel.SetActive(true);
            
            if (titleText != null)
                titleText.text = "✂️ Potatura";
            
            if (bodyText != null)
                bodyText.text = "Eseguire la potatura su questa pianta?";
            
            // Configura toggle Spray
            if (sprayToggle != null)
            {
                // BUG FIX: Il toggle deve essere sempre interattivo per permettere il click
                // Se non c'è STR-004, il toggle sarà disabilitato visivamente ma comunque cliccabile per feedback
                sprayToggle.interactable = true; // Sempre interattivo per permettere il click
                sprayToggle.isOn = false; // Default: non selezionato
                
                // BUG FIX: Verifica che il toggle abbia un GraphicRaycaster nel Canvas
                Canvas canvas = sprayToggle.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                {
                    SporiumLogger.LogWarning(LogCategory.UI, "Canvas non ha GraphicRaycaster! Il toggle potrebbe non funzionare.");
                }
                
                // BUG FIX: Verifica che il toggle abbia un targetGraphic
                if (sprayToggle.targetGraphic == null)
                {
                    SporiumLogger.LogWarning(LogCategory.UI, "Toggle non ha targetGraphic! Il toggle potrebbe non funzionare.");
                }
                
                // Setup toggle
                
                // Aggiorna testo toggle
                var toggleLabel = sprayToggle.GetComponentInChildren<TextMeshProUGUI>();
                if (toggleLabel != null)
                {
                    if (hasSprayAvailable)
                        toggleLabel.text = "Aggiungi Spray Antifungino (consuma STR-004)";
                    else
                        toggleLabel.text = "Aggiungi Spray Antifungino (STR-004 non disponibile)";
                }
            }
            else
            {
                SporiumLogger.LogError(LogCategory.UI, "sprayToggle è NULL! Verifica che sia collegato nel prefab.");
            }
        }
        
        /// <summary>
        /// Nasconde il dialog
        /// </summary>
        public void Hide()
        {
            if (dialogPanel != null)
                dialogPanel.SetActive(false);
        }
        
        private void HandleConfirm()
        {
            bool useSpray = sprayToggle != null && sprayToggle.isOn;
            OnDialogResult?.Invoke(true, useSpray);
            Hide();
        }
        
        private void HandleCancel()
        {
            OnDialogResult?.Invoke(false, false);
            Hide();
        }
    }
}

