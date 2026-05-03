using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Project.Sporae.Core
{
    public class MissionChecker
    {
        public readonly MissionConfig Config;
        public bool IsCompleted;
        
        [Serializable]
        public struct OptionChecker
        {
            public List<GoalChecker> Checkers;
        }
        
        public bool Check()
        {
            // Enumerable.All su sequenza vuota è true: senza obiettivi la missione non deve auto-completarsi.
            if (_optionCheckers == null || _optionCheckers.Count == 0)
                return false;

            return _optionCheckers.All(CheckOptions);
        }
        
        public MissionChecker(MissionConfig config)
        {
            Config = config;
            
            _checkers = ServiceContainer.Instance.Get<GoalCheckers>();

            if (config.Goals == null)
                return;

            foreach (var goal in config.Goals)
                CreateOptionChecker(goal);
        }

        private void CreateOptionChecker(MissionConfig.GoalOptions goal)
        {
            var optionChecker = new OptionChecker() {
                Checkers = new()
            };

            if (goal.Options == null)
            {
                _optionCheckers.Add(optionChecker);
                return;
            }

            foreach (var option in goal.Options)
            {
                var checker = _checkers.CreateNewCheckerForGoal(option.GetType(), option);
                
                optionChecker.Checkers.Add(checker);
            }

            _optionCheckers.Add(optionChecker);
        }
        
        private bool CheckOptions(OptionChecker optionChecker)
        {
            if (optionChecker.Checkers == null || optionChecker.Checkers.Count == 0)
                return false;

            return optionChecker.Checkers
                .Where(checker => checker != null)
                .Select(checker => checker.Check())
                .Any(result => result);
        }
        
        private readonly List<OptionChecker> _optionCheckers = new();
        private readonly GoalCheckers _checkers;
    }
}