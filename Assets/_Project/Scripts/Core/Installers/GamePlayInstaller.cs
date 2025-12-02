using _Project.Pot;
using UnityEngine;

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
        }

        private void Update()
        {
            if (_missionManager != null)
                _missionManager.Check();
        }
    }
}