using System.Collections;
using _Project;
using _Project.Pot;
using UnityEngine;
using _Project.Sporae.Core;
using Sporae.Core;
using Sporae.Dome;
using Sporae.DevTools;
using Sporae.UI.UIToolkit.NotificationsFoundation;
using _Project.UI.UIToolkit.VoOverlay;
using Sporae.UI.UIToolkit.HUD;

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
            
            // Dispose existing DayCycleSystem before creating a new one to prevent HandleFaded double-subscription
            if (ServiceContainer.Instance.Contains(typeof(DayCycleSystem)))
            {
                var existingDcs = ServiceContainer.Instance.Get<DayCycleSystem>(suppressWarning: true);
                existingDcs?.Dispose();
            }
            ServiceContainer.Instance.Register(new DayCycleSystem(_fadeToBlack));
            
            ServiceContainer.Instance.Register(new GoalCheckers());
            ServiceContainer.Instance.Register(new DiaryStatistics());
            ServiceContainer.Instance.Register(new DayActivityLog());
            ServiceContainer.Instance.Register(new WikiUnlockService());
            ServiceContainer.Instance.Register(new NightEventsGenerator());
            ServiceContainer.Instance.Register(new PotNotifications());
            ServiceContainer.Instance.Register(new DomePotRegistry());

            // PhSystem (pH globale Dome): core gameplay; TopBar, EoD, DayCycleController, PotActions, piante, ecc. lo usano via ServiceContainer.
            if (!ServiceContainer.Instance.Contains(typeof(PhSystem)))
            {
                ServiceContainer.Instance.Register(new PhSystem(0f));
            }

            if (!ServiceContainer.Instance.Contains(typeof(DomeMutationRuntimeService)))
            {
                ServiceContainer.Instance.Register(new DomeMutationRuntimeService());
            }

            // Registra ToastNotificationManager se presente nella scena (opzionale: il gioco usa Foundation/HUD 2.0)
            var toastManager = FindObjectOfType<ToastNotificationManager>();
            if (toastManager != null)
            {
                ServiceContainer.Instance.Register(toastManager);
                SporiumLogger.LogInfo(LogCategory.Core, "ToastNotificationManager registrato nel ServiceContainer");
            }
            else
            {
                SporiumLogger.LogDebug(LogCategory.Core, "ToastNotificationManager non in scena (ok se usi Notifications Foundation / HUD 2.0).");
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
                go.AddComponent<FoundationMutationImWatcher>();
                go.AddComponent<FoundationLoreSchedulerRunner>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                go.AddComponent<FoundationNotificationsDebugConsole>();
#endif
            }

            if (!ServiceContainer.Instance.Contains(typeof(MissionFlagTracker)))
                ServiceContainer.Instance.Register(new MissionFlagTracker());

            _missionManager = new MissionManager();
            ServiceContainer.Instance.Register(_missionManager);

            var voGo = new GameObject("VoOverlay");
            var voOverlay = voGo.AddComponent<VoOverlayController>();
            ServiceContainer.Instance.Register(voOverlay);

            var activeMissionsGo = new GameObject("ActiveMissions");
            activeMissionsGo.AddComponent<ActiveMissionsPanelController>();

            var wardrobeGo = new GameObject("WardrobePanel");
            wardrobeGo.AddComponent<WardrobePanelController>();

            var demoSession = new DemoSessionState();
            if (DemoSessionState.StartNextSessionAsDemo)
            {
                demoSession.IsDemo = true;
                DemoSessionState.StartNextSessionAsDemo = false;
            }
            ServiceContainer.Instance.Register(demoSession);
            if (demoSession.IsDemo)
            {
                var demoGo = new GameObject("DemoStoryDirector");
                demoGo.AddComponent<DemoStoryDirector>();
            }

            var playerStatToasts = new GameObject("PlayerStatToastBridge");
            playerStatToasts.AddComponent<PlayerStatToastBridge>();
            
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
        /// Dopo che GameManager è pronto: se si è entrati da "Carica" (SlotToLoadOnNextScene impostato)
        /// carica quello slot; altrimenti non fare auto-load (la scena di gioco si apre solo da Nuova Partita o Carica dal menu Bootstrap).
        /// </summary>
        private IEnumerator LoadSaveWhenGameManagerReady(SaveManager saveManager)
        {
            if (SkipAutoLoad)
            {
                SkipAutoLoad = false;
                yield break;
            }

            // Slot richiesto dal menu "Carica", altrimenti nessun auto-load (partita da zero solo con "Nuova Partita")
            string slotToLoad = SaveManager.SlotToLoadOnNextScene;
            SaveManager.SlotToLoadOnNextScene = null;
            if (string.IsNullOrEmpty(slotToLoad) || !saveManager.SaveExists(slotToLoad))
            {
#if UNITY_EDITOR
                if (string.IsNullOrEmpty(slotToLoad))
                    SporiumLogger.LogInfo(LogCategory.Core, "Ingresso in scena di gioco senza slot da caricare");
                else
                    SporiumLogger.LogWarning(LogCategory.Core, $"Slot richiesto '{slotToLoad}' non trovato o vuoto");
#endif
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

            bool loadSuccess = saveManager.LoadGame(slotToLoad);
#if UNITY_EDITOR
            if (loadSuccess)
                SporiumLogger.LogInfo(LogCategory.Core, $"✅ Salvataggio caricato: {slotToLoad}");
            else
                SporiumLogger.LogWarning(LogCategory.Core, $"⚠️ Errore durante il caricamento dello slot {slotToLoad}");
#else
            if (!loadSuccess)
                SporiumLogger.LogWarning(LogCategory.Core, $"⚠️ Errore durante il caricamento dello slot {slotToLoad}");
#endif
        }

        private void Update()
        {
            if (_missionManager != null)
                _missionManager.Check();
        }
    }
}