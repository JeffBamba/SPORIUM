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
            ServiceContainer.Instance.Register(_uiNotification);
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