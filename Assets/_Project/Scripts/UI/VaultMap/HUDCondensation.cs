using _Project.Sporae.Core;
using Sporae.Core;
using UnityEngine;
using UnityEngine.UI;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.NotificationsFoundation;

namespace _Project
{
    public class HUDCondensation : MonoBehaviour
    {
        [SerializeField] private ProgressBar _progressBar;
        [SerializeField] private Button _collectButton;

        private GameManager _gameManager;
        private Inventory _inventory;
        private UINotification _uiNotification;
        
        private void Awake()
        { 
            // Usa ServiceContainer invece di FindObjectOfType
            _gameManager = ServiceContainer.Instance?.Get<GameManager>();
            _uiNotification = ServiceContainer.Instance?.Get<UINotification>();
            
            if (_gameManager == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "GameManager non disponibile via ServiceContainer. Tentativo late binding...");
                if (ServiceContainer.Instance != null)
                {
                    ServiceContainer.Instance.OnServiceRegistered += OnServiceRegistered;
                }
            }
            
            if (_uiNotification == null)
                SporiumLogger.LogWarning(LogCategory.UI, "UINotification non disponibile via ServiceContainer");
        }
        
        /// <summary>
        /// Late binding per servizi quando vengono registrati
        /// </summary>
        private void OnServiceRegistered(object service)
        {
            if (service is GameManager gameManager && _gameManager == null)
            {
                _gameManager = gameManager;
                if (_gameManager != null)
                {
                    _gameManager.OnCondensationChanged += HandleChangeCondensation;
                    // FASE 7: Usa CurrentAccumulation (percentuale 0-100%)
                    HandleChangeCondensation(_gameManager.CondensationSystem?.CurrentAccumulation ?? 0f);
                }
                
                if (ServiceContainer.Instance != null)
                {
                    ServiceContainer.Instance.OnServiceRegistered -= OnServiceRegistered;
                }
            }
        }

        private void Start()
        {
            // Prova a ottenere GameManager se non ancora disponibile
            if (_gameManager == null)
            {
                _gameManager = ServiceContainer.Instance?.Get<GameManager>();
            }
            
            if (_gameManager != null)
            {
                _gameManager.OnCondensationChanged += HandleChangeCondensation;
                // FASE 7: Usa CurrentAccumulation (percentuale 0-100%)
                HandleChangeCondensation(_gameManager.CondensationSystem?.CurrentAccumulation ?? 0f);
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "GameManager non disponibile in Start(). Verrà sottoscritto quando disponibile.");
            }
            
            // DISABILITATO: Button Collect ora disponibile nel tooltip TopBar
            // Il button nella vecchia HUD è disabilitato per evitare doppia raccolta
            if (_collectButton != null)
            {
                _collectButton.interactable = false; // Disabilita button vecchia HUD
                // _collectButton.onClick.AddListener(HandleCollect); // Rimosso - usa tooltip TopBar
            }
        }

        private void OnDestroy()
        {
            if (_gameManager != null)
                _gameManager.OnCondensationChanged -= HandleChangeCondensation;
            
            // Cleanup ServiceContainer subscriptions
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.OnServiceRegistered -= OnServiceRegistered;
            }
        }

        /// <summary>
        /// FASE 7: Aggiorna progress bar con percentuale condensazione (0-100%).
        /// Il valore ricevuto è già una percentuale, quindi dividiamo per 100.
        /// </summary>
        private void HandleChangeCondensation(float value)
        {
            if (_gameManager == null || _progressBar == null)
                return;
                
            // FASE 7: Il valore è già percentuale (0-100%), dividiamo per 100 per normalizzare a 0-1
            _progressBar.Value = value / 100f;
        }

        private void HandleCollect()
        {
            // Prova a ottenere GameManager se non ancora disponibile
            if (_gameManager == null)
            {
                _gameManager = ServiceContainer.Instance?.Get<GameManager>();
            }
            
            if (_gameManager == null)
            {
                SporiumLogger.LogWarning(LogCategory.UI, "GameManager non disponibile per raccogliere condensa.");
                return;
            }
            
            int amountToCollect = _gameManager.CollectCondensation();
            if (amountToCollect != 0)
            {
                if (_uiNotification != null)
                {
                    // Usa nuovo sistema toast se disponibile
                    var foundation = FoundationNotificationServiceAccessor.Get(suppressWarning: true);
                    if (foundation != null && foundation.Enabled)
                    {
                        foundation.PostToast("WATER-001", new NotificationPayload().With("amount", amountToCollect.ToString()));
                    }
                    else
                    {
                        var toastManager = ServiceContainer.Instance?.Get<ToastNotificationManager>(suppressWarning: true);
                        if (toastManager != null)
                        {
                            toastManager.ShowToast(ToastNotificationType.ResourceGained, $"Acqua piovana raccolta: {amountToCollect}!", "WATER-001");
                        }
                        else if (_uiNotification != null)
                        {
                            _uiNotification.ShowNotification($"Acqua piovana raccolta: {amountToCollect}!", 3f, Color.green);
                        }
                    }
                }
                _gameManager.PlayerInventory.Add(Items.Water, amountToCollect);
            }
        }
    }
}