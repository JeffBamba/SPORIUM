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
            return _optionCheckers.All(CheckOptions);
        }
        
        public MissionChecker(MissionConfig config)
        {
            Config = config;
            
            _checkers = ServiceContainer.Instance.Get<GoalCheckers>();

            foreach (var goal in config.Goals)
                CreateOptionChecker(goal);
        }

        private void CreateOptionChecker(MissionConfig.GoalOptions goal)
        {
            var optionChecker = new OptionChecker() {
                Checkers = new()
            };

            foreach (var option in goal.Options)
            {
                var checker = _checkers.CreateNewCheckerForGoal(option.GetType(), option);
                
                optionChecker.Checkers.Add(checker);
            }

            _optionCheckers.Add(optionChecker);
        }
        
        private bool CheckOptions(OptionChecker optionChecker)
        {
            return optionChecker.Checkers
                .Select(checker => checker.Check())
                .Any(result => result);
        }
        
        private readonly List<OptionChecker> _optionCheckers = new();
        private readonly GoalCheckers _checkers;
    }
}