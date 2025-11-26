using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using _Project.Sporae.Core;

namespace _Project
{
    public class UIMission : MonoBehaviour
    {
        [SerializeField] private Button _callButton;
        [SerializeField] private Visitor _visitor;
        [SerializeField] private TextMeshProUGUI _missionLabel;
        
        private MissionManager _missionManager;

        private void HandleMissionsChanged()
        {
            string text = "";

            foreach (var mission in _missionManager.CurrentMissions)
            {
                text += $"{mission.Config.Title}:\n";
                text = mission.Config.Goals.Aggregate(text, (current, goal) => current + $"- {goal.Options[0].Title}\n");
            }
            
            _missionLabel.text = text;
        }
        
        private void Awake()
        {
            _missionManager = ServiceContainer.Instance.Get<MissionManager>();
        }

        private void Start()
        {
            if (_callButton != null)
                _callButton.onClick.AddListener(HandleCall);
            
            if (_missionManager != null)
            {
                _missionManager.OnMissionsChanged += HandleMissionsChanged;
                _missionManager.OnMissionComplete += HandleMissionComplete;
            }
        }
        
        private void OnDestroy()
        {
            if (_missionManager != null)
            {
                _missionManager.OnMissionComplete -= HandleMissionComplete;
                _missionManager.OnMissionsChanged -= HandleMissionsChanged;
            }
        }
        
        private void HandleCall()
        {
            _visitor.Appear(Visitor.VisitorState.WaitingForFinish);
            _callButton.interactable = false;
        }
        
        private void HandleMissionComplete(MissionChecker checker)
        {
            _callButton.interactable = true;
        }
    }
}