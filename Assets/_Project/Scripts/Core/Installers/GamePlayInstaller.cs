using System;
using System.Collections;
using _Project.Pot;
using UnityEngine;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.NotificationsFoundation;

namespace _Project.Sporae.Core.Installers
{
    [DefaultExecutionOrder(-100)]
    public class GamePlayInstaller : MonoBehaviour
    {
        [SerializeField] private UINotification _uiNotification;
        [SerializeField] private FadeToBlackAnimation _fadeToBlack;

        [Header("Notifications Foundation (ex novo)")]
        [SerializeField] private bool _enableFoundationNotifications = false;
        [SerializeField] private bool _enableFoundationRunners = true;
        
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

            // Notifications Foundation (ex novo): registra sempre il service, ma può restare disabilitato in coexistence
            if (!ServiceContainer.Instance.Contains(typeof(FoundationNotificationService)))
            {
                var foundationService = new FoundationNotificationService
                {
                    Enabled = _enableFoundationNotifications
                };
                ServiceContainer.Instance.Register(foundationService);
            }

            // Se abilitato, crea runners runtime (Tick service + watchers + lore). La UI viene aggiunta in scena manualmente.
            if (_enableFoundationNotifications && _enableFoundationRunners)
            {
                var go = new GameObject("FoundationNotificationsRuntime");
                go.AddComponent<FoundationNotificationsRunner>();
                go.AddComponent<FoundationNotificationsWatchersRunner>();
                go.AddComponent<FoundationLoreSchedulerRunner>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                go.AddComponent<FoundationNotificationsDebugConsole>();
#endif
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
            
            // Registra SaveManager; il caricamento viene posticipato dopo che GameManager è pronto
            // (LoadGame in Awake fallirebbe perché GameManager ha DefaultExecutionOrder -50 e non è ancora registrato)
            var saveManager = SaveManager.Instance;
            if (saveManager != null)
            {
                if (!ServiceContainer.Instance.Contains(typeof(SaveManager)))
                {
                    ServiceContainer.Instance.Register(saveManager);
                }
                StartCoroutine(LoadSaveWhenGameManagerReady(saveManager));
            }
        }

        /// <summary>
        /// Se true, la prossima volta che la scena di gioco viene caricata non verrà eseguito l'auto-load del save.
        /// Impostare a true da "New Game" nel menu prima di LoadScene.
        /// </summary>
        public static bool SkipAutoLoad { get; set; }

        /// <summary>
        /// Carica il salvataggio "default" solo dopo che GameManager è registrato e inizializzato,
        /// così ApplySaveData può ripristinare CRY, azioni, inventario e condensazione correttamente.
        /// </summary>
        private IEnumerator LoadSaveWhenGameManagerReady(SaveManager saveManager)
        {
            if (SkipAutoLoad)
            {
                SkipAutoLoad = false;
                yield break;
            }

            const int maxFrames = 30;
            int frame = 0;
            while (frame < maxFrames)
            {
                yield return null;
                frame++;
                var gameManager = ServiceContainer.Instance?.Get<GameManager>(suppressWarning: true);
                if (gameManager != null)
                    break;
            }

            if (!saveManager.SaveExists("default"))
            {
#if UNITY_EDITOR
                SporiumLogger.LogInfo(LogCategory.Core, "Nessun salvataggio trovato, partita nuova");
#endif
                yield break;
            }

            bool loadSuccess = saveManager.LoadGame("default");
#if UNITY_EDITOR
            if (loadSuccess)
                SporiumLogger.LogInfo(LogCategory.Core, "✅ Salvataggio caricato automaticamente");
            else
                SporiumLogger.LogWarning(LogCategory.Core, "⚠️ Errore durante il caricamento automatico del salvataggio");
#else
            if (!loadSuccess)
                SporiumLogger.LogWarning(LogCategory.Core, "⚠️ Errore durante il caricamento automatico del salvataggio");
#endif
        }

        private void Update()
        {
            if (_missionManager != null)
                _missionManager.Check();
        }
    }
}