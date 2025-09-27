using System.Collections.Generic;

namespace _Project.Sporae.Core
{
    public class MissionManager 
    {
        private readonly List<MissionChecker> _currentMissions = new();

        public void Append(MissionConfig config)
        {
            _currentMissions.Add(new MissionChecker(config));
        }          
    }
}