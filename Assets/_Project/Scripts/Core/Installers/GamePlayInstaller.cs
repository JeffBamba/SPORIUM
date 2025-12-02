using _Project.Pot;
using UnityEngine;
using _Project.Sporae.Core;
using Sporae.Core;

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
                Debug.LogError("[GamePlayInstaller] ServiceContainer.Instance è null! Impossibile registrare servizi.");
                return;
            }
            
            // Registra servizi solo se non null
            if (_uiNotification != null)
            {
                ServiceContainer.Instance.Register(_uiNotification);
            }
            else
            {
                Debug.LogWarning("[GamePlayInstaller] UINotification non assegnato! Alcune funzionalità potrebbero non funzionare.");
            }
            
            // DayCycleSystem può essere creato anche se _fadeToBlack è null
            ServiceContainer.Instance.Register(new DayCycleSystem(_fadeToBlack));
            
            ServiceContainer.Instance.Register(new GoalCheckers());
            ServiceContainer.Instance.Register(new DiaryStatistics());
            ServiceContainer.Instance.Register(new PotNotifications());

            _missionManager = new MissionManager();
            ServiceContainer.Instance.Register(_missionManager);
            
            // Registra AssetManager e precarica asset critici
            var assetManager = AssetManager.Instance;
            if (assetManager != null)
            {
                ServiceContainer.Instance.Register(assetManager);
                assetManager.PreloadCriticalAssets();
            }
            
            // Registra SaveManager e carica salvataggio se esiste
            var saveManager = SaveManager.Instance;
            if (saveManager != null)
            {
                ServiceContainer.Instance.Register(saveManager);
                
                // Carica salvataggio automatico se esiste
                if (saveManager.SaveExists("default"))
                {
                    bool loadSuccess = saveManager.LoadGame("default");
#if UNITY_EDITOR
                    if (loadSuccess)
                    {
                        Debug.Log("[GamePlayInstaller] ✅ Salvataggio caricato automaticamente");
                    }
                    else
                    {
                        Debug.LogWarning("[GamePlayInstaller] ⚠️ Errore durante il caricamento automatico del salvataggio");
                    }
#else
                    if (!loadSuccess)
                    {
                        Debug.LogWarning("[GamePlayInstaller] ⚠️ Errore durante il caricamento automatico del salvataggio");
                    }
#endif
                }
#if UNITY_EDITOR
                else
                {
                    Debug.Log("[GamePlayInstaller] Nessun salvataggio trovato, partita nuova");
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