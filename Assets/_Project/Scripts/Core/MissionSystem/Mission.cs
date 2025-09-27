using System;
using System.Collections.Generic;
using System.Linq;

namespace _Project.Sporae.Core
{
    public class MissionChecker
    {
        public MissionConfig Config;
        
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
                optionChecker.Checkers.Add(_checkers.CreateNewCheckerForGoal(option.GetType()));
                
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