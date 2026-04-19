namespace _Project.Sporae.Core
{
    [SpecificGoalChecker(GoalType = typeof(MissionFlagGoal))]
    public sealed class MissionFlagGoalChecker : GoalChecker
    {
        private readonly MissionFlagGoal _goal;

        public MissionFlagGoalChecker(GoalConfig goalConfig)
        {
            _goal = (MissionFlagGoal)goalConfig;
        }

        public override bool Check()
        {
            var tracker = ServiceContainer.Instance?.Get<MissionFlagTracker>(suppressWarning: true);
            return tracker != null && tracker.HasFlag(_goal.FlagKey);
        }
    }
}
