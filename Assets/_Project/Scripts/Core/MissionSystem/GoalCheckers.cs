using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace _Project.Sporae.Core
{
    public class GoalCheckers
    {
        private readonly Dictionary<Type, Type> _checkers = new();

        public GoalCheckers()
        {
            LoadCheckers();
        }
        
        private void LoadCheckers()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    t.GetCustomAttribute<SpecificGoalCheckerAttribute>() != null &&
                    typeof(GoalChecker).IsAssignableFrom(t)
                );

            var checkerTypes = types as Type[] ?? types.ToArray();
            foreach (var checkerType in checkerTypes)
            {
                var goalType = checkerType.GetCustomAttribute<SpecificGoalCheckerAttribute>().GoalType;
                _checkers.Add(goalType, checkerType);
            }
        }
        
        public GoalChecker CreateNewCheckerForGoal(Type type, GoalConfig goalConfig)
        {
            if (typeof(GoalChecker).IsAssignableFrom(type))
                return null;
                
            if (!_checkers.TryGetValue(type, out var checker))
                return null;

            return (GoalChecker)Activator.CreateInstance(checker, new object[] { goalConfig });
        }
    }
}