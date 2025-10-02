using UnityEngine;

namespace _Project.Sporae.Core.Installers
{
    [DefaultExecutionOrder(-100)]
    public class GamePlayInstaller : MonoBehaviour
    {
        [SerializeField] private UINotification _uiNotification;
        
        private MissionManager _missionManager;
        
        public void Awake()
        {
            ServiceContainer.Init();
            ServiceContainer.Instance.Register(_uiNotification);
            ServiceContainer.Instance.Register(new DayCycleSystem());
            ServiceContainer.Instance.Register(new GoalCheckers());
            ServiceContainer.Instance.Register(new DiaryStatistics());

            _missionManager = new MissionManager();
            ServiceContainer.Instance.Register(_missionManager);
        }

        private void Update()
        {
            _missionManager.Check();
        }
    }
}