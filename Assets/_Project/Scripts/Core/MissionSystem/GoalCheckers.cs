using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

            foreach (var casterType in types)
            {
                var spellType = casterType.GetCustomAttribute<SpecificGoalCheckerAttribute>().SpellType;
                _checkers.Add(spellType, casterType);
            }
        }
        
        public GoalChecker CreateNewCheckerForGoal(Type type)
        {
            if (!type.IsAssignableFrom(typeof(GoalChecker)))
                return null;
                
            if (!_checkers.TryGetValue(type, out var checker))
                return null;

            return (GoalChecker)Activator.CreateInstance(checker);
        }
    }
}