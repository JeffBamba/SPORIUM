namespace _Project.Sporae.Core
{
    public class GoalChecker
    {
        public GoalConfig Config;

        public virtual bool Check() => false;
    }
}