using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace _Project.Sporae.Core
{
    public class MissionManager 
    {
        private readonly List<MissionChecker> _currentMissions = new();
        
        public ReadOnlyCollection<MissionChecker> CurrentMissions => _currentMissions.AsReadOnly();
        public event Action OnMissionsChanged;
        public event Action<MissionChecker> OnMissionComplete;
        
        public void Append(MissionConfig config)
        {
            _currentMissions.Add(new MissionChecker(config));
            OnMissionsChanged?.Invoke();
        }

        public void Remove(MissionConfig config)
        {
            foreach (var mission in _currentMissions.ToList().Where(mission => mission.Config == config))
                _currentMissions.Remove(mission);
            OnMissionsChanged?.Invoke();
        }
        
        public void Check()
        {
            foreach (
                var mission in 
                    _currentMissions
                        .Where(mission => !mission.IsCompleted && mission.Check()))
            {
                mission.IsCompleted = true;
                OnMissionComplete?.Invoke(mission);
            }
        }
    }
}