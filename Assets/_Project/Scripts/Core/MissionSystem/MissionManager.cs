using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

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

        /// <summary>Svuota la lista missioni (per restore da save).</summary>
        public void Clear()
        {
            _currentMissions.Clear();
            OnMissionsChanged?.Invoke();
        }

        /// <summary>Ripristina missioni da save (configName = MissionConfig.name, da Resources).</summary>
        public void RestoreFromSave(IEnumerable<(string configName, bool isCompleted)> entries)
        {
            if (entries == null) return;
            _currentMissions.Clear();
            var configs = Resources.LoadAll<MissionConfig>("");
            foreach (var (configName, isCompleted) in entries)
            {
                if (string.IsNullOrEmpty(configName)) continue;
                var config = configs.FirstOrDefault(c => c != null && c.name == configName);
                if (config == null) continue;
                var checker = new MissionChecker(config);
                checker.IsCompleted = isCompleted;
                _currentMissions.Add(checker);
            }
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