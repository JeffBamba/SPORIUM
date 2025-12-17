using _Project.Pot;
using UnityEngine;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.DevTools;

namespace _Project.Sporae.Core.Installers
{
    [DefaultExecutionOrder(-100)]
    public class GamePlayInstaller : MonoBehaviour
    {
        [SerializeField] private UINotification _uiNotification;
        [SerializeField] private FadeToBlackAnimation _fadeToBlack;
        
        private MissionManager _missionManager;
        
        public void Awake()
        {
            ServiceContainer.Init();
            
            // Verifica che ServiceContainer.Instance sia disponibile
            if (ServiceContainer.Instance == null)
            {
                SporiumLogger.LogError(LogCategory.Core, "ServiceContainer.Instance è null! Impossibile registrare servizi.");
                return;
            }
            
            // Registra servizi solo se non null
            if (_uiNotification != null)
            {
                ServiceContainer.Instance.Register(_uiNotification);
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.Core, "UINotification non assegnato! Alcune funzionalità potrebbero non funzionare.");
            }
            
            // DayCycleSystem può essere creato anche se _fadeToBlack è null
            ServiceContainer.Instance.Register(new DayCycleSystem(_fadeToBlack));
            
            ServiceContainer.Instance.Register(new GoalCheckers());
            ServiceContainer.Instance.Register(new DiaryStatistics());
            ServiceContainer.Instance.Register(new PotNotifications());

            // Registra ToastNotificationManager se presente nella scena
            var toastManager = FindObjectOfType<ToastNotificationManager>();
            if (toastManager != null)
            {
                ServiceContainer.Instance.Register(toastManager);
                SporiumLogger.LogInfo(LogCategory.Core, "ToastNotificationManager registrato nel ServiceContainer");
            }
            else
            {
                SporiumLogger.LogWarning(LogCategory.Core, "ToastNotificationManager non trovato nella scena. Toast notifications potrebbero non funzionare.");
            }

            _missionManager = new MissionManager();
            ServiceContainer.Instance.Register(_missionManager);
            
            // Registra AssetManager e precarica asset critici
            // DEBUG_SAFE_FIX: Verifica se è già registrato prima di registrarlo di nuovo
            var assetManager = AssetManager.Instance;
            if (assetManager != null)
            {
                // Registra solo se non è già registrato (evita doppia registrazione)
                if (!ServiceContainer.Instance.Contains(typeof(AssetManager)))
                {
                    ServiceContainer.Instance.Register(assetManager);
                }
                assetManager.PreloadCriticalAssets();
            }
            
            // Registra SaveManager e carica salvataggio se esiste
            // DEBUG_SAFE_FIX: Verifica se è già registrato prima di registrarlo di nuovo
            var saveManager = SaveManager.Instance;
            if (saveManager != null)
            {
                // Registra solo se non è già registrato (evita doppia registrazione)
                if (!ServiceContainer.Instance.Contains(typeof(SaveManager)))
                {
                    ServiceContainer.Instance.Register(saveManager);
                }
                
                // Carica salvataggio automatico se esiste
                if (saveManager.SaveExists("default"))
                {
                    bool loadSuccess = saveManager.LoadGame("default");
#if UNITY_EDITOR
                    if (loadSuccess)
                    {
                        SporiumLogger.LogInfo(LogCategory.Core, "✅ Salvataggio caricato automaticamente");
                    }
                    else
                    {
                        SporiumLogger.LogWarning(LogCategory.Core, "⚠️ Errore durante il caricamento automatico del salvataggio");
                    }
#else
                    if (!loadSuccess)
                    {
                        SporiumLogger.LogWarning(LogCategory.Core, "⚠️ Errore durante il caricamento automatico del salvataggio");
                    }
#endif
                }
#if UNITY_EDITOR
                else
                {
                    SporiumLogger.LogInfo(LogCategory.Core, "Nessun salvataggio trovato, partita nuova");
                }
#endif
            }
        }

        private void Update()
        {
            if (_missionManager != null)
                _missionManager.Check();
        }
    }
}