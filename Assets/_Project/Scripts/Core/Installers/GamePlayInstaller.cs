using _Project.Sporae.Core;
using UnityEngine;

namespace _Project.Scripts.Core.Installers
{
    public class GamePlayInstaller : MonoBehaviour
    {
        public void Awake()
        {
            ServiceContainer.Instance.Register(new GoalCheckers());
            ServiceContainer.Instance.Register(new MissionManager());
        }
    }
}