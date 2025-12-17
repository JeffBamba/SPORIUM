using _Project.Sporae.Core;
using Sporae.Core;
using UnityEngine;
using UnityEngine.UI;
using Sporae.DevTools;

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
                    HandleChangeCondensation(_gameManager.CondensationSystem?.CondensationAmount ?? 0f);
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
                HandleChangeCondensation(_gameManager.CondensationSystem?.CondensationAmount ?? 0f);
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.UI, "GameManager non disponibile in Start(). Verrà sottoscritto quando disponibile.");
            }
            
            if (_collectButton != null)
            {
                _collectButton.onClick.AddListener(HandleCollect);
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

        private void HandleChangeCondensation(float value)
        {
            if (_gameManager == null || _progressBar == null)
                return;
                
            _progressBar.Value = value / _gameManager.GetMaxCondensation();
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
            
            int amountToCollect = (int)_gameManager.CollectCondensation();
            if (amountToCollect != 0)
            {
                if (_uiNotification != null)
                {
                    // Usa nuovo sistema toast se disponibile
                    var toastManager = ServiceContainer.Instance?.Get<ToastNotificationManager>(suppressWarning: true);
                    if (toastManager != null)
                    {
                        toastManager.ShowToast(ToastNotificationType.ResourceGained, $"You collected Rainwater: {amountToCollect}!", "WATER-001");
                    }
                    else if (_uiNotification != null)
                    {
                        _uiNotification.ShowNotification($"You collected Rainwater: {amountToCollect}!", 3f, Color.green);
                    }
                }
                _gameManager.PlayerInventory.Add(Items.Water, amountToCollect);
            }
        }
    }
}