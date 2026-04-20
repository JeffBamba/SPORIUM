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
        private readonly List<MissionChecker> _completedMissions = new();
        
        public ReadOnlyCollection<MissionChecker> CurrentMissions => _currentMissions.AsReadOnly();
        public ReadOnlyCollection<MissionChecker> CompletedMissions => _completedMissions.AsReadOnly();
        public event Action OnMissionsChanged;
        public event Action<MissionChecker> OnMissionComplete;
        public event Action<MissionChecker> OnMissionAdded;

        public void Append(MissionConfig config)
        {
            var checker = new MissionChecker(config);
            _currentMissions.Add(checker);
            OnMissionAdded?.Invoke(checker);
            OnMissionsChanged?.Invoke();
        }

        /// <summary>
        /// Aggiunge la missione solo se non esiste già in lista attiva.
        /// Utile per evitare duplicati quando più trigger possono appendere la stessa MissionConfig.
        /// </summary>
        public bool AppendIfMissing(MissionConfig config)
        {
            if (config == null)
                return false;

            bool alreadyPresent = _currentMissions.Any(m => m != null && m.Config == config);
            if (alreadyPresent)
                return false;

            Append(config);
            return true;
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
            _completedMissions.Clear();
            OnMissionsChanged?.Invoke();
        }

        /// <summary>Ripristina missioni da save (configName = MissionConfig.name, da Resources).</summary>
        public void RestoreFromSave(IEnumerable<(string configName, bool isCompleted)> entries)
        {
            if (entries == null) return;
            _currentMissions.Clear();
            _completedMissions.Clear();
            var configs = Resources.LoadAll<MissionConfig>("");
            foreach (var (configName, isCompleted) in entries)
            {
                if (string.IsNullOrEmpty(configName)) continue;
                var config = configs.FirstOrDefault(c => c != null && c.name == configName);
                if (config == null) continue;
                var checker = new MissionChecker(config);
                checker.IsCompleted = isCompleted;
                if (isCompleted)
                {
                    if (!_completedMissions.Any(m => m?.Config == config))
                        _completedMissions.Add(checker);
                }
                else
                {
                    _currentMissions.Add(checker);
                }
            }
            OnMissionsChanged?.Invoke();
        }
        
        public void Check()
        {
            var snapshot = _currentMissions.ToList();
            var anyRemoved = false;
            foreach (var mission in snapshot)
            {
                if (mission.IsCompleted)
                    continue;
                if (!mission.Check())
                    continue;
                mission.IsCompleted = true;
                OnMissionComplete?.Invoke(mission);
                _currentMissions.Remove(mission);
                if (!_completedMissions.Any(m => m?.Config == mission.Config))
                    _completedMissions.Add(mission);
                anyRemoved = true;
            }

            if (anyRemoved)
                OnMissionsChanged?.Invoke();
        }
    }
}
